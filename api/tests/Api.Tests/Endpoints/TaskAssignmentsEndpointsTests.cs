using Api.Tests.Testing;
using Application.TaskAssignments.DTOs;
using Domain.Enums;
using FluentAssertions;
using System.Net;
using TestHelpers.Api.Common;
using TestHelpers.Api.Common.Http;
using TestHelpers.Api.Endpoints.Projects;
using TestHelpers.Api.Endpoints.TaskAssignments;
using TestHelpers.Common.Testing;

namespace Api.Tests.Endpoints
{
    [IntegrationTest]
    public sealed class TaskAssignmentsEndpointsTests
    {
        [Fact]
        public async Task Create_Get_Delete_Concurrency_Preconditions()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // Create full board (project, lane, column, task, owner)
            var (project, _, _, task, owner) = await BoardSetupHelper.SetupBoard(client);

            // Use owner as assignment user (project admin and project member)
            client.SetAuthorization(owner.AccessToken);

            var assignmentCreateDto = new TaskAssignmentCreateDto
            {
                Role = TaskRole.Owner,
                UserId = owner.UserId
            };

            // Ensure there is an assignment for this task/user
            var createResponse = await TaskAssignmentTestHelper.PostAssignmentResponseAsync(
                client,
                project.Id,
                task.Id,
                assignmentCreateDto);

            // Depending on your domain, this may be 201 (first owner) or 409 (already exists)
            createResponse.StatusCode.Should().BeOneOf(
                HttpStatusCode.Created,
                HttpStatusCode.Conflict);

            // Load current assignment state
            var getAssignmentResponse = await TaskAssignmentTestHelper.GetAssignmentByIdResponseAsync(
                client,
                project.Id,
                task.Id,
                owner.UserId);
            getAssignmentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            getAssignmentResponse.Headers.ETag.Should().NotBeNull();

            var currentAssignment =
                await getAssignmentResponse.ReadContentAsDtoAsync<TaskAssignmentReadDto>();

            // 428: delete without If-Match
            client.DefaultRequestHeaders.IfMatch.Clear();
            var deleteMissingIfMatch = await client.DeleteAsync(
                $"/projects/{project.Id}/tasks/{task.Id}/assignments/{owner.UserId}");
            deleteMissingIfMatch.StatusCode.Should().Be((HttpStatusCode)428);

            // 412: delete with clearly stale RowVersion
            var staleRowVersion = CommonApiTestHelpers.GenerateStaleRowVersion(currentAssignment.RowVersion);

            var deleteStale = await TaskAssignmentTestHelper.DeleteAssignmentResponseAsync(
                client,
                project.Id,
                task.Id,
                owner.UserId,
                staleRowVersion);
            deleteStale.StatusCode.Should().Be((HttpStatusCode)412);
        }

        [Fact]
        public async Task Me_200_And_ByUser_403_Then_200_As_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // Create full board (without note): project, lane, column and task
            var (_, _, _, _, user) = await BoardSetupHelper.SetupBoard(client);

            // Get me: OK
            var me = await client.GetAsync("/assignments/me");
            me.StatusCode.Should().Be(HttpStatusCode.OK);

            // Get user assignments without being admin: forbidden
            var getByUserIdResponse = await TaskAssignmentTestHelper.GetAssignmentByUserResponseAsync(
                client,
                user.UserId);
            getByUserIdResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // Promote to admin
            var adminBearer = await UsersEndpointsTests.MintTokenAsync(
                app,
                user.UserId,
                user.Email,
                user.Name,
                UserRole.Admin);
            client.SetAuthorization(adminBearer);

            // Get user assignments being admin: OK
            getByUserIdResponse = await TaskAssignmentTestHelper.GetAssignmentByUserResponseAsync(
                client,
                user.UserId);
            getByUserIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
