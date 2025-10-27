using Api.Tests.Testing;
using Application.TaskActivities.DTOs;
using Application.TaskNotes.DTOs;
using Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TestHelpers.Api;

namespace Api.Tests.Endpoints
{
    public sealed class TaskActivitiesEndpointsTests
    {
        [Fact]
        public async Task List_ByTask_And_ByType_Then_GetById_And_TopLevel_Me_And_ByUser_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var (project, lane, column, task, user) = await EndpointsTestHelper.SetupBoard(client);

            client.DefaultRequestHeaders.IfMatch.Clear();
            (await client.PostAsJsonAsync(
                $"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/notes",
                new TaskNoteCreateDto { Content = "note" })).EnsureSuccessStatusCode();

            // list all
            var listAll = await client.GetAsync($"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/activities");
            listAll.StatusCode.Should().Be(HttpStatusCode.OK);
            var all = (await listAll.Content.ReadFromJsonAsync<TaskActivityReadDto[]>(EndpointsTestHelper.Json))!;
            all.Should().NotBeNull().And.NotBeEmpty();

            // filter by type
            var type = all[0].Type;
            var listByType = await client.GetAsync($"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/activities?activityType={type}");
            listByType.StatusCode.Should().Be(HttpStatusCode.OK);

            // top-level me
            var me = await client.GetAsync("/activities/me");
            me.StatusCode.Should().Be(HttpStatusCode.OK);

            // by user → 403 luego 200 como admin
            var byUserForbidden = await client.GetAsync($"/activities/users/{user.UserId}");
            byUserForbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var admin = await EndpointsTestHelper.RegisterAndLoginAsync(client, name: "sys", email: "sys@x.com");
            var adminBearer = await UsersEndpointsTests.MintToken(app, admin.UserId, admin.Email, admin.Name, UserRole.Admin);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);

            var byUser = await client.GetAsync($"/activities/users/{user.UserId}");
            byUser.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Unauthorized_401_And_Forbidden_403()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();
            var anon = await client.GetAsync($"/projects/{Guid.NewGuid()}/lanes/{Guid.NewGuid()}/columns/{Guid.NewGuid()}/tasks/{Guid.NewGuid()}/activities");
            anon.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var (project, lane, column, task, _) = await EndpointsTestHelper.SetupBoard(client);

            var b = await EndpointsTestHelper.RegisterAndLoginAsync(client, name: "other", email: "b@x.com");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", b.AccessToken);
            var forbidden = await client.GetAsync($"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/activities");
            forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}
