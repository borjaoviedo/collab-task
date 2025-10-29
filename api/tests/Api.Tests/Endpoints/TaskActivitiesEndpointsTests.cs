using Api.Tests.Testing;
using Application.TaskActivities.DTOs;
using Application.Users.DTOs;
using Domain.Enums;
using FluentAssertions;
using System.Net;
using TestHelpers.Api.Auth;
using TestHelpers.Api.Defaults;
using TestHelpers.Api.Http;
using TestHelpers.Api.Projects;
using TestHelpers.Api.TaskActivities;

namespace Api.Tests.Endpoints
{
    public sealed class TaskActivitiesEndpointsTests
    {
        [Fact]
        public async Task List_ByTask_And_ByType_Then_GetById_And_TopLevel_Me_And_ByUser_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // Create full board: project, lane, column, task and note
            var (project, lane, column, task, _, user) = await BoardSetupHelper.SetupBoardWithNote(client);

            // Get all activities
            var getActivitiesResponse = await TaskActivityTestHelper.GetTaskActivitiesResponseAsync(
                client,
                project.Id,
                lane.Id,
                column.Id,
                task.Id);
            var list = await getActivitiesResponse.ReadContentAsDtoAsync<List<TaskActivityReadDto>>();
            list.Should().NotBeNull().And.NotBeEmpty();

            // Filter by type
            var type = list[0].Type;
            var listByType = await client.GetAsync(
                $"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/activities?activityType={type}");
            listByType.StatusCode.Should().Be(HttpStatusCode.OK);

            // Get me
            var getMeResponse = await client.GetAsync("/activities/me");
            getMeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Get other user activities without being admin: forbidden
            var getByUserResponse = await TaskActivityTestHelper.GetTaskActivitiesByUserResponseAsync(client, user.UserId);
            getByUserResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // Create admin
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

            // Get other user notes being admin: OK
            getByUserResponse = await TaskActivityTestHelper.GetTaskActivitiesByUserResponseAsync(client, user.UserId);
            getByUserResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Unauthorized_401_And_Forbidden_403()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // Get activities without being authenticated: unauthorized
            var getActivitiesResponse = await TaskActivityTestHelper.GetTaskActivitiesResponseAsync(
                client,
                projectId: Guid.NewGuid(),
                laneId: Guid.NewGuid(),
                columnId: Guid.NewGuid(),
                taskId: Guid.NewGuid());
            getActivitiesResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // Create full board (without note): project, lane, column and task
            var (project, lane, column, task, _) = await BoardSetupHelper.SetupBoard(client);

            // Create different user
            var userRegisterDto = new UserRegisterDto()
            {
                Email = "user@e.com",
                Name = "user",
                Password = UserDefaults.DefaultPassword
            };
            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client, userRegisterDto);

            // Get activities without being a project member: forbidden
            getActivitiesResponse = await TaskActivityTestHelper.GetTaskActivitiesResponseAsync(
                client,
                project.Id,
                lane.Id,
                column.Id,
                task.Id);
            getActivitiesResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}
