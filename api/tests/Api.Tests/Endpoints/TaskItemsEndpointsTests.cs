using Api.Tests.Testing;
using Application.TaskItems.DTOs;
using Application.Users.DTOs;
using FluentAssertions;
using System.Net;
using TestHelpers.Api.Common.Http;
using TestHelpers.Api.Endpoints.Auth;
using TestHelpers.Api.Endpoints.Projects;
using TestHelpers.Api.Endpoints.TaskItems;

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

            // List tasks for that column (should be empty)
            var getTasksResponse = await TaskItemTestHelper.GetTaskItemsByColumnResponseAsync(
                client,
                project.Id,
                column.Id);

            getTasksResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var taskList = await getTasksResponse.ReadContentAsDtoAsync<List<TaskItemReadDto>>();
            taskList.Should().BeEmpty();

            // Create task
            var task = await TaskItemTestHelper.PostTaskItemDtoAsync(client, project.Id, lane.Id, column.Id);

            // Get task by id works and sends ETag
            var get = await client.GetAsync($"/projects/{project.Id}/tasks/{task.Id}");
            get.StatusCode.Should().Be(HttpStatusCode.OK);
            get.Headers.ETag.Should().NotBeNull();
        }

        [Fact]
        public async Task Unauthorized_401_And_NotFound_For_Other_Project()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // 401: Get tasks without being authenticated
            var getTasksResponse = await TaskItemTestHelper.GetTaskItemsByColumnResponseAsync(
                client,
                projectId: Guid.NewGuid(),
                columnId: Guid.NewGuid());
            getTasksResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // Default user register, login and create project, lane and column
            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client);
            var (project, _, column) = await BoardSetupHelper.CreateProjectLaneAndColumn(client);

            // Different user register and login
            var userRegisterDto = new UserRegisterDto
            {
                Email = "random@e.com",
                Name = "random",
                Password = "Passw0rd!"
            };
            await AuthTestHelper.RegisterLoginAndAuthorizeAsync(client, userRegisterDto);

            // NotFound: Get tasks from project/column where this user is not a member
            var forbiddenResponse = await TaskItemTestHelper.GetTaskItemsByColumnResponseAsync(
                client,
                project.Id,
                column.Id);
            forbiddenResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
