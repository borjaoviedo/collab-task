using Api.Tests.Testing;
using Application.Columns.DTOs;
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

            var auth = await AuthTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var createPrj = await client.PostAsJsonAsync("/projects", new ProjectCreateDto { Name = "My Project" });
            createPrj.StatusCode.Should().Be(HttpStatusCode.Created);
            var project = await createPrj.Content.ReadFromJsonAsync<ProjectReadDto>(AuthTestHelper.Json);

            var list = await client.GetAsync($"/projects/{project!.Id}/lanes");
            list.StatusCode.Should().Be(HttpStatusCode.OK);
            var items = await list.Content.ReadFromJsonAsync<List<LaneReadDto>>(AuthTestHelper.Json);
            items.Should().NotBeNull();
            items!.Should().BeEmpty();
        }

        [Fact]
        public async Task Create_Rename_Reorder_Delete_Works_With_Concurrency()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth = await AuthTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            // project
            var createPrj = await client.PostAsJsonAsync("/projects", new ProjectCreateDto { Name = "P1" });
            createPrj.StatusCode.Should().Be(HttpStatusCode.Created);
            var prj = await createPrj.Content.ReadFromJsonAsync<ProjectReadDto>(AuthTestHelper.Json);

            // create lane
            var createLane = await client.PostAsJsonAsync($"/projects/{prj!.Id}/lanes", new LaneCreateDto { Name = "Backlog", Order = 0 });
            createLane.StatusCode.Should().Be(HttpStatusCode.Created);
            var lane = await createLane.Content.ReadFromJsonAsync<LaneReadDto>(AuthTestHelper.Json);
            lane!.Name.Should().Be("Backlog");

            // rename (If-Match)
            var etag1 = $"W/\"{Convert.ToBase64String(lane.RowVersion)}\"";
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag1);

            var rename = await client.PutAsJsonAsync($"/projects/{prj.Id}/lanes/{lane.Id}/rename", new LaneRenameDto { NewName = "Todo" });
            rename.StatusCode.Should().Be(HttpStatusCode.OK);
            var lane2 = await rename.Content.ReadFromJsonAsync<LaneReadDto>(AuthTestHelper.Json);
            lane2!.Name.Should().Be("Todo");
            lane2.RowVersion.Should().NotBeEquivalentTo(lane.RowVersion);

            // create another lane so index 1 exists
            var create2 = await client.PostAsJsonAsync($"/projects/{prj.Id}/lanes", new LaneCreateDto { Name = "Review", Order = 1 });
            create2.StatusCode.Should().Be(HttpStatusCode.Created);
            await create2.Content.ReadFromJsonAsync<LaneReadDto>(AuthTestHelper.Json);

            // reorder (If-Match)
            var etag2 = $"W/\"{Convert.ToBase64String(lane2.RowVersion)}\"";
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag2);

            var reorder = await client.PutAsJsonAsync($"/projects/{prj.Id}/lanes/{lane.Id}/reorder", new LaneReorderDto { NewOrder = 1 });
            reorder.StatusCode.Should().Be(HttpStatusCode.OK);
            var lane3 = await reorder.Content.ReadFromJsonAsync<LaneReadDto>(AuthTestHelper.Json);
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

            var auth = await AuthTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var prj = (await (await client.PostAsJsonAsync("/projects", new ProjectCreateDto { Name = "P1" }))
                .Content.ReadFromJsonAsync<ProjectReadDto>(AuthTestHelper.Json))!;

            var lane = (await (await client.PostAsJsonAsync($"/projects/{prj.Id}/lanes", new LaneCreateDto { Name = "Lane", Order = 0 }))
                .Content.ReadFromJsonAsync<LaneReadDto>(AuthTestHelper.Json))!;

            // wrong ETag
            var wrongEtag = $"W/\"{Convert.ToBase64String(new byte[] { 1, 2, 3 })}\"";
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", wrongEtag);

            var rename = await client.PutAsJsonAsync($"/projects/{prj.Id}/lanes/{lane.Id}/rename", new LaneRenameDto { NewName = "Lane X" });
            rename.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        }
    }
}
