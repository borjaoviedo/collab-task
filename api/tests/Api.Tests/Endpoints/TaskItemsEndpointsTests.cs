using Api.Tests.Testing;
using Application.TaskItems.DTOs;
using Application.Users.DTOs;
using FluentAssertions;
using System.Net;
using TestHelpers.Api.Auth;
using TestHelpers.Api.Http;
using TestHelpers.Api.Projects;
using TestHelpers.Api.TaskItems;

namespace Api.Tests.Endpoints
{
    public sealed class TaskItemsEndpointsTests
    {

        [Fact]
        public async Task List_Empty_Create_GetById_Sends_ETag()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);

            // Create project, lane and column but no tasks
            var (project, lane, column) = await BoardSetupHelper.CreateProjectLaneAndColumn(client);
            var getTasksResponse = await TaskItemTestHelper.GetTaskItemsResponseAsync(
                client,
                project.Id,
                lane.Id,
                column.Id);

            getTasksResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Task list is empty
            var taskList = await getTasksResponse.ReadContentAsDtoAsync<List<TaskItemReadDto>>();
            taskList.Should().BeEmpty();

            // Create task
            var task = await TaskItemTestHelper.PostTaskItemDtoAsync(client, project.Id, lane.Id, column.Id);

            // Get task by id works
            var get = await client.GetAsync($"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}");
            get.StatusCode.Should().Be(HttpStatusCode.OK);
            get.Headers.ETag.Should().NotBeNull();
        }

        [Fact]
        public async Task Unauthorized_401_And_Forbidden_403()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // Get tasks without being authenticated
            var getTasksResponse = await TaskItemTestHelper.GetTaskItemsResponseAsync(
                client,
                projectId: Guid.NewGuid(),
                laneId: Guid.NewGuid(),
                columnId: Guid.NewGuid());
            getTasksResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // Default user register, login and create project, lane and column
            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);
            var (project, lane, column) = await BoardSetupHelper.CreateProjectLaneAndColumn(client);

            // Different user register and login
            var userRegisterDto = new UserRegisterDto()
            {
                Email = "random@e.com",
                Name = "random",
                Password = "Passw0rd!"
            };
            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client, userRegisterDto);

            // Get tasks from project where different user is not a member: forbidden 
            var forbiddenResponse = await TaskItemTestHelper.GetTaskItemsResponseAsync(
                client,
                project.Id,
                lane.Id,
                column.Id);
            forbiddenResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}
