using Api.Tests.Testing;
using Application.TaskItems.DTOs;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TestHelpers;

namespace Api.Tests.Endpoints
{
    public sealed class TaskItemsEndpointsTests
    {
        [Fact]
        public async Task List_Empty_Create_GetById_Sends_ETag()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();
            var auth = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

            var (project, lane, column) = await EndpointsTestHelper.CreateProjectLaneAndColumn(client);
            var list = await client.GetAsync($"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks");
            list.StatusCode.Should().Be(HttpStatusCode.OK);
            (await list.Content.ReadFromJsonAsync<TaskItemReadDto[]>(EndpointsTestHelper.Json))!.Should().BeEmpty();

            var task = await EndpointsTestHelper.CreateTask(client, project.Id, lane.Id, column.Id);

            var get = await client.GetAsync($"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}");
            get.StatusCode.Should().Be(HttpStatusCode.OK);
            get.Headers.ETag.Should().NotBeNull();
        }

        [Fact]
        public async Task Unauthorized_401_And_Forbidden_403()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var list401 = await client.GetAsync($"/projects/{Guid.NewGuid()}/lanes/{Guid.NewGuid()}/columns/{Guid.NewGuid()}/tasks");
            list401.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            var a = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", a.AccessToken);
            var (prj, lane, col) = await EndpointsTestHelper.CreateProjectLaneAndColumn(client);

            var b = await EndpointsTestHelper.RegisterAndLoginAsync(client, name: "other", email: "b@x.com");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", b.AccessToken);
            var forbidden = await client.GetAsync($"/projects/{prj.Id}/lanes/{lane.Id}/columns/{col.Id}/tasks");
            forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }
    }
}
