using Api.Tests.Testing;
using Application.Lanes.DTOs;
using Application.Users.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using TestHelpers.Api.Auth;
using TestHelpers.Api.Defaults;
using TestHelpers.Api.Http;
using TestHelpers.Api.Lanes;
using TestHelpers.Api.Projects;

namespace Api.Tests.Endpoints
{
    public sealed class LanesEndpointsTests
    {
        [Fact]
        public async Task List_ByProject_Returns200_And_Empty_Initially()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            var project = await ProjectTestHelper.PostProjectDtoAsync(client);
            var list = await client.GetAsync($"/projects/{project.Id}/lanes");

            list.StatusCode.Should().Be(HttpStatusCode.OK);

            var items = await list.ReadContentAsDtoAsync<List<LaneReadDto>>();

            items.Should().NotBeNull();
            items.Should().BeEmpty();
        }

        [Fact]
        public async Task Create_Rename_Reorder_Delete_Works_With_Concurrency()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            // create project and lane
            var (project, lane) = await BoardSetupHelper.CreateProjectAndLane(client);

            // rename
            var renamedLane = await LaneTestHelper.RenameLaneDtoAsync(
                client,
                project.Id,
                lane.Id,
                lane.RowVersion);

            renamedLane.Name.Should().Be(LaneDefaults.DefaultLaneRename);
            renamedLane.RowVersion.Should().NotBeEquivalentTo(lane.RowVersion);

            // create another lane so index 1 exists
            var laneCreateDto = new LaneCreateDto() { Name = "different lane", Order = 1 };
            await LaneTestHelper.PostLaneDtoAsync(client, project.Id, laneCreateDto);

            // reorder
            var reorderedLane = await LaneTestHelper.ReorderLaneDtoAsync(
                client,
                project.Id,
                lane.Id,
                renamedLane.RowVersion);
            reorderedLane.Order.Should().Be(1);

            // delete
            var deleleResponse = await LaneTestHelper.DeleteLaneResponseAsync(
                client,
                project.Id,
                lane.Id,
                reorderedLane.RowVersion);
            deleleResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Rename_Returns412_On_Wrong_RowVersion()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            var (project, lane) = await BoardSetupHelper.CreateProjectAndLane(client);

            // wrong ETag
            var wrongEtag = $"W/\"{Convert.ToBase64String(new byte[] { 1, 2, 3 })}\"";
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", wrongEtag);

            var renameResponse = await client.PutAsJsonAsync(
                $"/projects/{project.Id}/lanes/{lane.Id}/rename",
                LaneDefaults.DefaultLaneRenameDto);
            renameResponse.StatusCode.Should().Be(HttpStatusCode.PreconditionFailed);
        }

        [Fact]
        public async Task List_Empty_Create_GetById_Sends_ETag()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            var project = await ProjectTestHelper.PostProjectDtoAsync(client);

            // Get lanes response from empty project
            var getLanesResponse = await LaneTestHelper.GetLanesResponseAsync(client, project.Id);
            getLanesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var lanesList = await getLanesResponse.ReadContentAsDtoAsync<List<LaneReadDto>>();
            lanesList.Should().BeEmpty();

            // Create lane
            var lane = await LaneTestHelper.PostLaneDtoAsync(client, project.Id);

            getLanesResponse = await LaneTestHelper.GetLaneResponseAsync(client, project.Id, lane.Id);
            getLanesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            getLanesResponse.Headers.ETag.Should().NotBeNull();
        }

        [Fact]
        public async Task Create_With_IfMatch_Header_Returns_400()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);
            var project = await ProjectTestHelper.PostProjectDtoAsync(client);

            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", "\"abc\"");

            var createResponse = await client.PostAsJsonAsync($"/projects/{project.Id}/lanes", LaneDefaults.DefaultLaneCreateDto);
            createResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Rename_Missing_IfMatch_Returns_428()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            var (project, lane) = await BoardSetupHelper.CreateProjectAndLane(client);

            client.DefaultRequestHeaders.IfMatch.Clear();

            var renameResponse = await client.PutAsJsonAsync(
                $"/projects/{project.Id}/lanes/{lane.Id}/rename",
                LaneDefaults.DefaultLaneRename);
            renameResponse.StatusCode.Should().Be((HttpStatusCode)428);
        }

        [Fact]
        public async Task Rename_Valid_Then_Stale_412()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            var (project, lane) = await BoardSetupHelper.CreateProjectAndLane(client);

            // Valid rename
            var okRenameResponse = await LaneTestHelper.RenameLaneResponseAsync(
                client,
                project.Id,
                lane.Id,
                lane.RowVersion);
            okRenameResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Invalid rename
            var newRenameDto = new LaneRenameDto() { NewName = "L3" };
            var staleRenameResponse = await LaneTestHelper.RenameLaneResponseAsync(
                client,
                project.Id,
                lane.Id,
                lane.RowVersion,
                newRenameDto);

            staleRenameResponse.StatusCode.Should().Be((HttpStatusCode)412);
        }

        [Fact]
        public async Task Reorder_Then_Delete_Missing_IfMatch_Returns_428()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            var (project, lane) = await BoardSetupHelper.CreateProjectAndLane(client);

            // Create another lane so index 1 exists
            var laneCreateDto = new LaneCreateDto() { Name = "different lane", Order = 1 };
            await LaneTestHelper.PostLaneDtoAsync(client, project.Id, laneCreateDto);

            // Reorder
            var reorderResponse = await LaneTestHelper.ReorderLaneResponseAsync(
                client,
                project.Id,
                lane.Id,
                lane.RowVersion);
            reorderResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Delete without ifMatch
            client.DefaultRequestHeaders.IfMatch.Clear();
            var deleteResponse = await client.DeleteAsync($"/projects/{project.Id}/lanes/{lane.Id}");
            deleteResponse.StatusCode.Should().Be((HttpStatusCode)428);
        }

        [Fact]
        public async Task GetById_404_Vs_403()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            var (project, lane) = await BoardSetupHelper.CreateProjectAndLane(client);

            // Get non existing lane: not found
            var getLaneResponse = await LaneTestHelper.GetLaneResponseAsync(
                client,
                project.Id,
                laneId: Guid.NewGuid());
            getLaneResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var userRegisterDto = new UserRegisterDto()
            {
                Email = "random@e.com",
                Name = "random",
                Password = "Rand0m123!"
            };
            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client, userRegisterDto);

            // Get lane from project where current user is not a member: forbidden
            getLaneResponse = await LaneTestHelper.GetLaneResponseAsync(client, project.Id, lane.Id);
            getLaneResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}
