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
    public sealed class ColumnsEndpointsTests
    {
        private static async Task<(ProjectReadDto prj, LaneReadDto lane)> CreateProjectAndLane(HttpClient client)
        {
            var prj = (await (await client.PostAsJsonAsync("/projects", new ProjectCreateDto { Name = "P1" }))
                .Content.ReadFromJsonAsync<ProjectReadDto>(AuthTestHelper.Json))!;
            var lane = (await (await client.PostAsJsonAsync($"/projects/{prj.Id}/lanes", new LaneCreateDto { Name = "L1", Order = 0 }))
                .Content.ReadFromJsonAsync<LaneReadDto>(AuthTestHelper.Json))!;
            return (prj, lane);
        }

        [Fact]
        public async Task List_ByLane_Then_Create_Rename_Reorder_Delete_Works()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var auth = await AuthTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var (prj, lane) = await CreateProjectAndLane(client);

            // list columns empty
            var list0 = await client.GetAsync($"/projects/{prj.Id}/lanes/{lane.Id}/columns");
            list0.StatusCode.Should().Be(HttpStatusCode.OK);
            var empty = await list0.Content.ReadFromJsonAsync<List<ColumnReadDto>>(AuthTestHelper.Json);
            empty!.Should().BeEmpty();

            // create
            var create = await client.PostAsJsonAsync($"/projects/{prj.Id}/lanes/{lane.Id}/columns", new ColumnCreateDto { Name = "Todo", Order = 0 });
            create.StatusCode.Should().Be(HttpStatusCode.Created);
            var col = await create.Content.ReadFromJsonAsync<ColumnReadDto>(AuthTestHelper.Json);

            // rename (If-Match)
            var etag1 = $"W/\"{Convert.ToBase64String(col!.RowVersion)}\"";
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag1);

            var rename = await client.PutAsJsonAsync($"/projects/{prj.Id}/lanes/{lane.Id}/columns/{col.Id}/rename", new ColumnRenameDto { NewName = "In Progress" });
            rename.StatusCode.Should().Be(HttpStatusCode.OK);
            var col2 = await rename.Content.ReadFromJsonAsync<ColumnReadDto>(AuthTestHelper.Json);
            col2!.Name.Should().Be("In Progress");

            // create another column so index 1 exists
            var create2 = await client.PostAsJsonAsync(
                $"/projects/{prj.Id}/lanes/{lane.Id}/columns",
                new ColumnCreateDto { Name = "Review", Order = 1 });
            create2.StatusCode.Should().Be(HttpStatusCode.Created);
            await create2.Content.ReadFromJsonAsync<ColumnReadDto>(AuthTestHelper.Json);

            // reorder (If-Match)
            var etag2 = $"W/\"{Convert.ToBase64String(col2.RowVersion)}\"";
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag2);

            var reorder = await client.PutAsJsonAsync($"/projects/{prj.Id}/lanes/{lane.Id}/columns/{col.Id}/reorder", new ColumnReorderDto { NewOrder = 1 });
            reorder.StatusCode.Should().Be(HttpStatusCode.OK);
            var col3 = await reorder.Content.ReadFromJsonAsync<ColumnReadDto>(AuthTestHelper.Json);
            col3!.Order.Should().Be(1);

            // delete (If-Match)
            var etag3 = $"W/\"{Convert.ToBase64String(col3.RowVersion)}\"";
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag3);

            var del = await client.DeleteAsync($"/projects/{prj.Id}/lanes/{lane.Id}/columns/{col.Id}");
            del.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }
}
