using Api.Tests.Testing;
using Application.Lanes.DTOs;
using Application.Projects.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TestHelpers;

namespace Api.Tests.Endpoints
{
    public sealed class LanesEndpointsTests
    {
        [Fact]
        public async Task List_ByProject_Returns200_And_Empty_Initially()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var createPrj = await client.PostAsJsonAsync("/projects", new ProjectCreateDto { Name = "My Project" });
            createPrj.StatusCode.Should().Be(HttpStatusCode.Created);
            var project = await createPrj.Content.ReadFromJsonAsync<ProjectReadDto>(EndpointsTestHelper.Json);

            var list = await client.GetAsync($"/projects/{project!.Id}/lanes");
            list.StatusCode.Should().Be(HttpStatusCode.OK);
            var items = await list.Content.ReadFromJsonAsync<List<LaneReadDto>>(EndpointsTestHelper.Json);
            items.Should().NotBeNull();
            items!.Should().BeEmpty();
        }

        [Fact]
        public async Task Create_Rename_Reorder_Delete_Works_With_Concurrency()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            // create project and lane
            var (prj, lane) = await EndpointsTestHelper.CreateProjectAndLane(client, laneName: "Backlog");

            // rename (If-Match)
            var etag1 = $"W/\"{Convert.ToBase64String(lane.RowVersion)}\"";
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag1);

            var rename = await client.PutAsJsonAsync($"/projects/{prj.Id}/lanes/{lane.Id}/rename", new LaneRenameDto { NewName = "Todo" });
            rename.StatusCode.Should().Be(HttpStatusCode.OK);
            var lane2 = await rename.Content.ReadFromJsonAsync<LaneReadDto>(EndpointsTestHelper.Json);
            lane2!.Name.Should().Be("Todo");
            lane2.RowVersion.Should().NotBeEquivalentTo(lane.RowVersion);

            client.DefaultRequestHeaders.IfMatch.Clear();

            // create another lane so index 1 exists
            await EndpointsTestHelper.CreateLane(client, prj.Id, "Review", 1);

            // reorder (If-Match)
            var etag2 = $"W/\"{Convert.ToBase64String(lane2.RowVersion)}\"";
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag2);

            var reorder = await client.PutAsJsonAsync($"/projects/{prj.Id}/lanes/{lane.Id}/reorder", new LaneReorderDto { NewOrder = 1 });
            reorder.StatusCode.Should().Be(HttpStatusCode.OK);
            var lane3 = await reorder.Content.ReadFromJsonAsync<LaneReadDto>(EndpointsTestHelper.Json);
            lane3!.Order.Should().Be(1);

            // delete (If-Match)
            var etag3 = $"W/\"{Convert.ToBase64String(lane3.RowVersion)}\"";
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag3);

            var del = await client.DeleteAsync($"/projects/{prj.Id}/lanes/{lane.Id}");
            del.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Rename_Returns412_On_Wrong_RowVersion()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var (prj, lane) = await EndpointsTestHelper.CreateProjectAndLane(client);

            // wrong ETag
            var wrongEtag = $"W/\"{Convert.ToBase64String(new byte[] { 1, 2, 3 })}\"";
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", wrongEtag);

            var rename = await client.PutAsJsonAsync($"/projects/{prj.Id}/lanes/{lane.Id}/rename", new LaneRenameDto { NewName = "Lane X" });
            rename.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        }

        [Fact]
        public async Task List_Empty_Create_GetById_Sends_ETag()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();
            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var prj = await EndpointsTestHelper.CreateProject(client);

            var list0 = await client.GetAsync($"/projects/{prj.Id}/lanes");
            list0.StatusCode.Should().Be(HttpStatusCode.OK);
            (await list0.Content.ReadFromJsonAsync<LaneReadDto[]>(EndpointsTestHelper.Json))!.Should().BeEmpty();

            var lane = await EndpointsTestHelper.CreateLane(client, prj.Id, "Backlog", 0);

            var get = await client.GetAsync($"/projects/{prj.Id}/lanes/{lane.Id}");
            get.StatusCode.Should().Be(HttpStatusCode.OK);
            get.Headers.ETag.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_With_IfMatch_Header_Returns_400()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();
            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var prj = await EndpointsTestHelper.CreateProject(client);

            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "\"abc\"");

            var create = await client.PostAsJsonAsync($"/projects/{prj.Id}/lanes", new LaneCreateDto() { Name = "L1", Order = 0});
            create.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Rename_Missing_IfMatch_Returns_428()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();
            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var (prj, lane) = await EndpointsTestHelper.CreateProjectAndLane(client);

            client.DefaultRequestHeaders.IfMatch.Clear();
            var resp = await client.PutAsJsonAsync($"/projects/{prj.Id}/lanes/{lane.Id}/rename", new LaneRenameDto() { NewName = "L2"});
            resp.StatusCode.Should().Be((HttpStatusCode)428);
        }

        [Fact]
        public async Task Rename_Valid_Then_Stale_412()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();
            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var (prj, lane) = await EndpointsTestHelper.CreateProjectAndLane(client);

            EndpointsTestHelper.SetIfMatchFromRowVersion(client, lane.RowVersion);
            var ok = await client.PutAsJsonAsync($"/projects/{prj.Id}/lanes/{lane.Id}/rename", new LaneRenameDto() { NewName = "L2"});
            ok.StatusCode.Should().Be(HttpStatusCode.OK);
            await ok.Content.ReadFromJsonAsync<LaneReadDto>(EndpointsTestHelper.Json);

            EndpointsTestHelper.SetIfMatchFromRowVersion(client, lane.RowVersion); // stale
            var stale = await client.PutAsJsonAsync($"/projects/{prj.Id}/lanes/{lane.Id}/rename", new LaneRenameDto() { NewName = "L3" });
            stale.StatusCode.Should().Be((HttpStatusCode)412);
        }

        [Fact]
        public async Task Reorder_Then_Delete_Missing_IfMatch_Returns_428()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();
            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var (prj, a) = await EndpointsTestHelper.CreateProjectAndLane(client, laneName: "Lane A", laneOrder: 0);
            await EndpointsTestHelper.CreateLane(client, prj.Id, "Lane B", 1);

            EndpointsTestHelper.SetIfMatchFromRowVersion(client, a.RowVersion);
            var reorder = await client.PutAsJsonAsync($"/projects/{prj.Id}/lanes/{a.Id}/reorder", new LaneReorderDto() { NewOrder = 1 });
            reorder.StatusCode.Should().Be(HttpStatusCode.OK);

            client.DefaultRequestHeaders.IfMatch.Clear();
            var del428 = await client.DeleteAsync($"/projects/{prj.Id}/lanes/{a.Id}");
            del428.StatusCode.Should().Be((HttpStatusCode)428);
        }

        [Fact]
        public async Task GetById_404_Vs_403()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth1 = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth1.AccessToken);

            var (prj, lane) = await EndpointsTestHelper.CreateProjectAndLane(client);

            var notFound = await client.GetAsync($"/projects/{prj.Id}/lanes/{Guid.NewGuid()}");
            notFound.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var auth2 = await EndpointsTestHelper.RegisterAndLoginAsync(client, name: "Other", email: "b@b.com");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth2.AccessToken);
            var forbidden = await client.GetAsync($"/projects/{prj.Id}/lanes/{lane.Id}");
            forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}
