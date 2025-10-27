using Api.Tests.Testing;
using Application.TaskAssignments.DTOs;
using Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TestHelpers.Api;

namespace Api.Tests.Endpoints
{
    public sealed class TaskAssignmentsEndpointsTests
    {
        [Fact]
        public async Task Create_Get_Upsert_Delete_With_Concurrency()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var (project, lane, column, task, owner) = await EndpointsTestHelper.SetupBoard(client);
            var assignee = await EndpointsTestHelper.RegisterAndLoginAsync(client, name: "assignee", email: "ass@x.com");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);
            client.DefaultRequestHeaders.IfMatch.Clear();

            // create CoOwner
            var add = await client.PostAsJsonAsync(
                $"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/assignments",
                new TaskAssignmentCreateDto { UserId = assignee.UserId, Role = TaskRole.CoOwner });
            add.StatusCode.Should().Be(HttpStatusCode.Created);
            var created = (await add.Content.ReadFromJsonAsync<TaskAssignmentReadDto>(EndpointsTestHelper.Json))!;

            // get by id
            var get = await client.GetAsync($"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/assignments/{assignee.UserId}");
            get.StatusCode.Should().Be(HttpStatusCode.OK);
            get.Headers.ETag.Should().NotBeNull();

            // upsert -> 200
            client.DefaultRequestHeaders.IfMatch.Clear();
            var upd = await client.PostAsJsonAsync(
                $"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/assignments",
                new TaskAssignmentCreateDto { UserId = assignee.UserId, Role = TaskRole.CoOwner });
            upd.StatusCode.Should().Be(HttpStatusCode.OK);

            // delete: 428 then 204
            client.DefaultRequestHeaders.IfMatch.Clear();
            var del428 = await client.DeleteAsync($"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/assignments/{assignee.UserId}");
            del428.StatusCode.Should().Be((HttpStatusCode)428);

            EndpointsTestHelper.SetIfMatchFromRowVersion(client, created.RowVersion);
            var del = await client.DeleteAsync($"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/assignments/{assignee.UserId}");
            del.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }


        [Fact]
        public async Task Me_200_And_ByUser_403_Then_200_As_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var (project, lane, column, task, owner) = await EndpointsTestHelper.SetupBoard(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", owner.AccessToken);

            client.DefaultRequestHeaders.IfMatch.Clear();
            (await client.PostAsJsonAsync(
                $"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/assignments",
                new TaskAssignmentCreateDto { UserId = owner.UserId, Role = TaskRole.CoOwner }))
                .EnsureSuccessStatusCode();

            var me = await client.GetAsync("/assignments/me");
            me.StatusCode.Should().Be(HttpStatusCode.OK);

            var byUserForbidden = await client.GetAsync($"/assignments/users/{owner.UserId}");
            byUserForbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var admin = await EndpointsTestHelper.RegisterAndLoginAsync(client, name: "sys", email: "sys@x.com");
            var adminBearer = await UsersEndpointsTests.MintToken(app, admin.UserId, admin.Email, admin.Name, UserRole.Admin);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);

            var byUser = await client.GetAsync($"/assignments/users/{owner.UserId}");
            byUser.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
