using Api.Tests.Testing;
using Application.Columns.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using TestHelpers.Api.Common.Http;
using TestHelpers.Api.Endpoints.Auth;
using TestHelpers.Api.Endpoints.Columns;
using TestHelpers.Api.Endpoints.Defaults;
using TestHelpers.Api.Endpoints.Projects;

namespace Api.Tests.Endpoints
{
    public sealed class ColumnsEndpointsTests
    {
        [Fact]
        public async Task List_ByLane_Then_Create_Rename_Reorder_Delete_Works()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            var (project, lane) = await BoardSetupHelper.CreateProjectAndLane(client);

            // List columns empty
            var getResponse = await ColumnTestHelper.GetColumnsResponseAsync(client, project.Id, lane.Id);
            getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var columnsList = await getResponse.ReadContentAsDtoAsync<List<ColumnReadDto>>();
            columnsList.Should().BeEmpty();

            // Create
            var column = await ColumnTestHelper.PostColumnDtoAsync(client, project.Id, lane.Id);

            // Rename
            var renameResponse = await ColumnTestHelper.RenameColumnResponseAsync(
                client,
                project.Id,
                lane.Id,
                column.Id,
                column.RowVersion);
            renameResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var renamedColumn = await renameResponse.ReadContentAsDtoAsync<ColumnReadDto>();
            renamedColumn.Name.Should().Be(ColumnDefaults.DefaultColumnRename);

            // Create another column so index 1 exists
            client.DefaultRequestHeaders.IfMatch.Clear();

            var secondColumnCreateDto = new ColumnCreateDto { Name = "Review", Order = 1 };
            await ColumnTestHelper.PostColumnResponseAsync(client, project.Id, lane.Id, secondColumnCreateDto);

            // Reorder
            var reorderResponse = await ColumnTestHelper.ReorderColumnResponseAsync(
                client,
                project.Id,
                lane.Id,
                column.Id,
                renamedColumn.RowVersion);
            reorderResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var reorderedColumn = await reorderResponse.ReadContentAsDtoAsync<ColumnReadDto>();
            reorderedColumn.Order.Should().Be(1);

            // Delete
            var deleteResponse = await ColumnTestHelper.DeleteColumnResponseAsync(
                client,
                project.Id,
                lane.Id,
                column.Id,
                reorderedColumn.RowVersion);
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task List_Empty_Create_GetById_Sends_ETag()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            var (project, lane) = await BoardSetupHelper.CreateProjectAndLane(client);

            // No columns created -> empty list
            var columnsList = await ColumnTestHelper.GetColumnsDtoAsync(client, project.Id, lane.Id);
            columnsList.Should().BeEmpty();

            // Create column
            var column = await ColumnTestHelper.PostColumnDtoAsync(client, project.Id, lane.Id);

            // Get created column
            var getColumnResponse = await ColumnTestHelper.GetColumnResponseAsync(client, project.Id, lane.Id, column.Id);

            // Response includes ETag
            getColumnResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            getColumnResponse.Headers.ETag.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_With_IfMatch_Header_Returns_400()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            var (project, lane) = await BoardSetupHelper.CreateProjectAndLane(client);

            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "\"abc\"");

            var create = await client.PostAsJsonAsync(
                $"/projects/{project.Id}/lanes/{lane.Id}/columns",
                ColumnDefaults.DefaultColumnCreateDto);
            create.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Rename_Missing_IfMatch_Returns_428()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            var (project, lane, column) = await BoardSetupHelper.CreateProjectLaneAndColumn(client);

            client.DefaultRequestHeaders.IfMatch.Clear(); // missing

            var renameResponse = await client.PutAsJsonAsync(
                $"/projects/{project.Id}/lanes/{lane.Id}/columns/{column!.Id}/rename",
                ColumnDefaults.DefaultColumnRenameDto);
            renameResponse.StatusCode.Should().Be((HttpStatusCode)428);
        }

        [Fact]
        public async Task Reorder_Then_Delete_Missing_IfMatch_Returns_428()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            var (project, lane, column) = await BoardSetupHelper.CreateProjectLaneAndColumn(client);

            // create second column
            var secondColumnCreateDto = new ColumnCreateDto() { Name = "Second column", Order = 1 };
            await ColumnTestHelper.PostColumnResponseAsync(
                client,
                project.Id,
                lane.Id,
                secondColumnCreateDto);

            // reorder ok
            var reorderResponse = await ColumnTestHelper.ReorderColumnResponseAsync(
                client,
                project.Id,
                lane.Id,
                column.Id,
                column.RowVersion);
            reorderResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // delete missing If-Match -> 428
            client.DefaultRequestHeaders.IfMatch.Clear();
            var deleteResponse = await client.DeleteAsync($"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}");
            deleteResponse.StatusCode.Should().Be((HttpStatusCode)428);
        }

        [Fact]
        public async Task GetById_NotFound_And_Unauthorized_List()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // 401 no auth
            var list401 = await ColumnTestHelper.GetColumnsResponseAsync(
                client,
                projectId: Guid.NewGuid(),
                laneId: Guid.NewGuid());
            list401.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            var (project, lane) = await BoardSetupHelper.CreateProjectAndLane(client);

            // 404 not found column
            var notFoundResponse = await ColumnTestHelper.GetColumnResponseAsync(
                client,
                project.Id,
                lane.Id,
                columnId: Guid.NewGuid());
            notFoundResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
