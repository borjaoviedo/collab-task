using Api.Tests.Testing;
using Application.Columns.DTOs;
using Application.Lanes.DTOs;
using Application.Projects.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TestHelpers.Api;

namespace Api.Tests.Endpoints
{
    public sealed class ColumnsEndpointsTests
    {
        [Fact]
        public async Task List_ByLane_Then_Create_Rename_Reorder_Delete_Works()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var (prj, lane) = await EndpointsTestHelper.CreateProjectAndLane(client);

            // list columns empty
            var list0 = await client.GetAsync($"/projects/{prj.Id}/lanes/{lane.Id}/columns");
            list0.StatusCode.Should().Be(HttpStatusCode.OK);
            var empty = await list0.Content.ReadFromJsonAsync<List<ColumnReadDto>>(EndpointsTestHelper.Json);
            empty!.Should().BeEmpty();

            // create
            var create = await client.PostAsJsonAsync($"/projects/{prj.Id}/lanes/{lane.Id}/columns", new ColumnCreateDto { Name = "Todo", Order = 0 });
            create.StatusCode.Should().Be(HttpStatusCode.Created);
            var col = await create.Content.ReadFromJsonAsync<ColumnReadDto>(EndpointsTestHelper.Json);

            // rename (If-Match)
            EndpointsTestHelper.SetIfMatchFromRowVersion(client, col!.RowVersion);
            var rename = await client.PutAsJsonAsync($"/projects/{prj.Id}/lanes/{lane.Id}/columns/{col.Id}/rename", new ColumnRenameDto { NewName = "In Progress" });
            rename.StatusCode.Should().Be(HttpStatusCode.OK);
            var col2 = await rename.Content.ReadFromJsonAsync<ColumnReadDto>(EndpointsTestHelper.Json);
            col2!.Name.Should().Be("In Progress");

            client.DefaultRequestHeaders.IfMatch.Clear();

            // create another column so index 1 exists
            var create2 = await client.PostAsJsonAsync(
                $"/projects/{prj.Id}/lanes/{lane.Id}/columns",
                new ColumnCreateDto { Name = "Review", Order = 1 });
            create2.StatusCode.Should().Be(HttpStatusCode.Created);
            await create2.Content.ReadFromJsonAsync<ColumnReadDto>(EndpointsTestHelper.Json);

            // reorder (If-Match)
            EndpointsTestHelper.SetIfMatchFromRowVersion(client, col2.RowVersion);
            var reorder = await client.PutAsJsonAsync($"/projects/{prj.Id}/lanes/{lane.Id}/columns/{col.Id}/reorder", new ColumnReorderDto { NewOrder = 1 });
            reorder.StatusCode.Should().Be(HttpStatusCode.OK);
            var col3 = await reorder.Content.ReadFromJsonAsync<ColumnReadDto>(EndpointsTestHelper.Json);
            col3!.Order.Should().Be(1);

            // delete (If-Match)
            EndpointsTestHelper.SetIfMatchFromRowVersion(client, col3.RowVersion);
            var del = await client.DeleteAsync($"/projects/{prj.Id}/lanes/{lane.Id}/columns/{col.Id}");
            del.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task List_Empty_Create_GetById_Sends_ETag()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();
            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var (prj, lane) = await EndpointsTestHelper.CreateProjectAndLane(client);

            var list0 = await client.GetAsync($"/projects/{prj.Id}/lanes/{lane.Id}/columns");
            list0.StatusCode.Should().Be(HttpStatusCode.OK);
            (await list0.Content.ReadFromJsonAsync<ColumnReadDto[]>(EndpointsTestHelper.Json))!.Should().BeEmpty();

            var create = await EndpointsTestHelper.PostWithoutIfMatchAsync(client, $"/projects/{prj.Id}/lanes/{lane.Id}/columns",
                new ColumnCreateDto { Name = "Todo", Order = 0 });
            create.StatusCode.Should().Be(HttpStatusCode.Created);
            var col = await create.Content.ReadFromJsonAsync<ColumnReadDto>(EndpointsTestHelper.Json);

            var get = await client.GetAsync($"/projects/{prj.Id}/lanes/{lane.Id}/columns/{col!.Id}");
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

            var (prj, lane) = await EndpointsTestHelper.CreateProjectAndLane(client);

            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "\"abc\"");

            var create = await client.PostAsJsonAsync($"/projects/{prj.Id}/lanes/{lane.Id}/columns",
                new ColumnCreateDto { Name = "X", Order = 0 });
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
            var create = await EndpointsTestHelper.PostWithoutIfMatchAsync(client, $"/projects/{prj.Id}/lanes/{lane.Id}/columns",
                new ColumnCreateDto { Name = "Todo", Order = 0 });
            var col = await create.Content.ReadFromJsonAsync<ColumnReadDto>(EndpointsTestHelper.Json);

            client.DefaultRequestHeaders.IfMatch.Clear(); // missing
            var rename = await client.PutAsJsonAsync(
                $"/projects/{prj.Id}/lanes/{lane.Id}/columns/{col!.Id}/rename",
                new ColumnRenameDto { NewName = "In Progress" });

            rename.StatusCode.Should().Be((HttpStatusCode)428);
        }

        [Fact]
        public async Task Rename_With_Valid_Then_Stale_Returns_200_Then_412()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();
            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var (prj, lane) = await EndpointsTestHelper.CreateProjectAndLane(client);
            var create = await EndpointsTestHelper.PostWithoutIfMatchAsync(client, $"/projects/{prj.Id}/lanes/{lane.Id}/columns",
                new ColumnCreateDto { Name = "Todo", Order = 0 });
            var col = await create.Content.ReadFromJsonAsync<ColumnReadDto>(EndpointsTestHelper.Json);

            // valid
            EndpointsTestHelper.SetIfMatchFromRowVersion(client, col!.RowVersion);
            var ok = await client.PutAsJsonAsync(
                $"/projects/{prj.Id}/lanes/{lane.Id}/columns/{col.Id}/rename",
                new ColumnRenameDto { NewName = "In Progress" });
            ok.StatusCode.Should().Be(HttpStatusCode.OK);
            await ok.Content.ReadFromJsonAsync<ColumnReadDto>(EndpointsTestHelper.Json);

            // stale
            EndpointsTestHelper.SetIfMatchFromRowVersion(client, col.RowVersion); // old
            var stale = await client.PutAsJsonAsync(
                $"/projects/{prj.Id}/lanes/{lane.Id}/columns/{col.Id}/rename",
                new ColumnRenameDto { NewName = "Done" });
            stale.StatusCode.Should().Be((HttpStatusCode)412);
        }

        [Fact]
        public async Task Reorder_Then_Delete_Missing_IfMatch_Returns_428()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();
            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var (prj, lane) = await EndpointsTestHelper.CreateProjectAndLane(client);
            var c1 = await EndpointsTestHelper.PostWithoutIfMatchAsync(client, $"/projects/{prj.Id}/lanes/{lane.Id}/columns",
                new ColumnCreateDto { Name = "Column A", Order = 0 });
            var col = await c1.Content.ReadFromJsonAsync<ColumnReadDto>(EndpointsTestHelper.Json);
            await EndpointsTestHelper.PostWithoutIfMatchAsync(client, $"/projects/{prj.Id}/lanes/{lane.Id}/columns",
                new ColumnCreateDto { Name = "Column B", Order = 1 });

            // reorder ok
            EndpointsTestHelper.SetIfMatchFromRowVersion(client, col!.RowVersion);
            var reorder = await client.PutAsJsonAsync(
                $"/projects/{prj.Id}/lanes/{lane.Id}/columns/{col.Id}/reorder",
                new ColumnReorderDto { NewOrder = 1 });
            reorder.StatusCode.Should().Be(HttpStatusCode.OK);

            // delete missing If-Match -> 428
            client.DefaultRequestHeaders.IfMatch.Clear();
            var del = await client.DeleteAsync($"/projects/{prj.Id}/lanes/{lane.Id}/columns/{col.Id}");
            del.StatusCode.Should().Be((HttpStatusCode)428);
        }

        [Fact]
        public async Task GetById_NotFound_And_Unauthorized_List()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // 401 sin auth
            var list401 = await client.GetAsync($"/projects/{Guid.NewGuid()}/lanes/{Guid.NewGuid()}/columns");
            list401.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // auth + proyecto/lane propios
            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var prj = await (await client.PostAsJsonAsync("/projects", new ProjectCreateDto { Name = "P1" }))
                .Content.ReadFromJsonAsync<ProjectReadDto>(EndpointsTestHelper.Json);
            var lane = await (await client.PostAsJsonAsync($"/projects/{prj!.Id}/lanes",
                new LaneCreateDto { Name = "L1", Order = 0 }))
                .Content.ReadFromJsonAsync<LaneReadDto>(EndpointsTestHelper.Json);

            // 404: columnId inexistente dentro de un proyecto/lane accesibles
            var notFound = await client.GetAsync($"/projects/{prj.Id}/lanes/{lane!.Id}/columns/{Guid.NewGuid()}");
            notFound.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
