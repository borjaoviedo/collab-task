using Api.Tests.Testing;
using Application.TaskActivities.DTOs;
using Application.Users.DTOs;
using Domain.Enums;
using FluentAssertions;
using System.Net;
using TestHelpers.Api.Common.Http;
using TestHelpers.Api.Endpoints.Auth;
using TestHelpers.Api.Endpoints.Defaults;
using TestHelpers.Api.Endpoints.Projects;
using TestHelpers.Api.Endpoints.TaskActivities;
using TestHelpers.Common.Testing;

namespace Api.Tests.Endpoints
{
    [IntegrationTest]
    public sealed class TaskActivitiesEndpointsTests
    {
        [Fact]
        public async Task List_ByTask_Then_Optional_ByType_And_GetById_And_TopLevel_Me_And_ByUser_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // Create full board: project, lane, column, task and note
            var (project, _, _, task, _, user) = await BoardSetupHelper.SetupBoardWithNote(client);

            // List activities for the task
            var getActivitiesResponse = await TaskActivityTestHelper.GetTaskActivitiesResponseAsync(
                client,
                project.Id,
                task.Id);

            getActivitiesResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var list = await getActivitiesResponse.ReadContentAsDtoAsync<List<TaskActivityReadDto>>();
            list.Should().NotBeNull();

            if (list.Count > 0)
            {
                // Filter by type for an existing activity
                var type = list[0].Type;

                var listByTypeResponse = await client.GetAsync(
                    $"/projects/{project.Id}/tasks/{task.Id}/activities?activityType={type}");

                listByTypeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

                var listByType = await listByTypeResponse.ReadContentAsDtoAsync<List<TaskActivityReadDto>>();
                listByType.Should().NotBeNull();
                listByType.Should().OnlyContain(a => a.Type == type);

                // Get by id using the activity endpoint
                var activityId = list[0].Id;

                var getByIdResponse = await client.GetAsync(
                    $"/projects/{project.Id}/activities/{activityId}");

                getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);

                var single = await getByIdResponse.ReadContentAsDtoAsync<TaskActivityReadDto>();
                single.Id.Should().Be(activityId);
            }

            // Top-level "me" endpoint: activities for current user
            var getMeResponse = await client.GetAsync("/activities/me");
            getMeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Access /activities/users/{userId} as non-admin: should be forbidden
            var getByUserResponse = await TaskActivityTestHelper.GetTaskActivitiesByUserResponseAsync(client, user.UserId);
            getByUserResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // Create system admin user
            var admin = await AuthTestHelper.PostRegisterAndLoginAsync(
                client,
                name: "sys",
                email: "sys@x.com");

            var adminBearer = await UsersEndpointsTests.MintTokenAsync(
                app,
                admin.UserId,
                admin.Email,
                admin.Name,
                UserRole.Admin);

            client.SetAuthorization(adminBearer);

            // As admin, listing activities by user should be allowed
            getByUserResponse = await TaskActivityTestHelper.GetTaskActivitiesByUserResponseAsync(client, user.UserId);
            getByUserResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var byUserList = await getByUserResponse.ReadContentAsDtoAsync<List<TaskActivityReadDto>>();
            byUserList.Should().NotBeNull();
        }

        [Fact]
        public async Task Unauthorized_401_And_NotFound_For_NonMember()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // 401: request without authentication
            var getActivitiesResponse = await TaskActivityTestHelper.GetTaskActivitiesResponseAsync(
                client,
                projectId: Guid.NewGuid(),
                taskId: Guid.NewGuid());
            getActivitiesResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // Create full board as first authenticated user
            var (project, _, _, task, _) = await BoardSetupHelper.SetupBoard(client);

            // Authenticate as a different user who is not a member of that project
            var userRegisterDto = new UserRegisterDto
            {
                Email = "user@e.com",
                Name = "user",
                Password = UserDefaults.DefaultPassword
            };
            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client, userRegisterDto);

            // Service currently returns 404 when the user is authenticated but not a member
            getActivitiesResponse = await TaskActivityTestHelper.GetTaskActivitiesResponseAsync(
                client,
                project.Id,
                task.Id);

            getActivitiesResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
