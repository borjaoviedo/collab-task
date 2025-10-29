using Api.Tests.Testing;
using Application.TaskAssignments.DTOs;
using Domain.Enums;
using FluentAssertions;
using System.Net;
using TestHelpers.Api.Auth;
using TestHelpers.Api.Http;
using TestHelpers.Api.Projects;
using TestHelpers.Api.TaskAssignments;

namespace Api.Tests.Endpoints
{
    public sealed class TaskAssignmentsEndpointsTests
    {
        [Fact]
        public async Task Create_Get_Upsert_Delete_With_Concurrency()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // Create full board (without note): project, lane, column and task
            var (project, lane, column, task, owner) = await BoardSetupHelper.SetupBoard(client);

            // Create different user
            var assignee = await AuthTestHelper.PostRegisterAndLoginAsync(
                client,
                email: "assignee@e.com",
                name: "assignee");

            // Back to owner authorization
            client.SetAuthorization(owner.AccessToken);

            // Create CoOwner assignment
            var assignmentCreateDto = new TaskAssignmentCreateDto
            {
                Role = TaskRole.CoOwner,
                UserId = assignee.UserId
            };
            var createResponse = await TaskAssignmentTestHelper.PostAssignmentResponseAsync(
                client,
                project.Id,
                lane.Id,
                column.Id,
                task.Id,
                assignmentCreateDto);

            createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdAssignment = await createResponse.ReadContentAsDtoAsync<TaskAssignmentReadDto>();

            // Get assignment by id
            var getAssignmentResponse = await TaskAssignmentTestHelper.GetAssignmentByIdResponseAsync(
                client,
                project.Id,
                lane.Id,
                column.Id,
                task.Id,
                assignee.UserId);
            getAssignmentResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            getAssignmentResponse.Headers.ETag.Should().NotBeNull();

            // Upsert -> 200
            var upsertResponse = await TaskAssignmentTestHelper.PostAssignmentResponseAsync(
                client,
                project.Id,
                lane.Id,
                column.Id,
                task.Id,
                assignmentCreateDto);
            upsertResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Delete without ifMatch: 428
            client.DefaultRequestHeaders.IfMatch.Clear();
            var deleteResponse = await client.DeleteAsync(
                $"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/assignments/{assignee.UserId}");
            deleteResponse.StatusCode.Should().Be((HttpStatusCode)428);

            // Delete (with ifMatch): 204
            IfMatchExtensions.SetIfMatchFromRowVersion(client, createdAssignment.RowVersion);
            deleteResponse = await TaskAssignmentTestHelper.DeleteAssignmentResponseAsync(
                client,
                project.Id,
                lane.Id,
                column.Id,
                task.Id,
                assignee.UserId,
                createdAssignment.RowVersion);
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
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
