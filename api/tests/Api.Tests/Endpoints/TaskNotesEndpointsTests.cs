using Api.Tests.Testing;
using Application.TaskNotes.DTOs;
using Domain.Enums;
using FluentAssertions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TestHelpers;

namespace Api.Tests.Endpoints
{
    public sealed class TaskNotesEndpointsTests
    {
        [Fact]
        public async Task List_Create_GetById_ETag_Edit_Delete_Concurrency()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var (project, lane, column, task, _) = await EndpointsTestHelper.SetupBoard(client);

            var list0 = await client.GetAsync($"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/notes");
            list0.StatusCode.Should().Be(HttpStatusCode.OK);
            (await list0.Content.ReadFromJsonAsync<TaskNoteReadDto[]>(EndpointsTestHelper.Json))!.Should().BeEmpty();

            client.DefaultRequestHeaders.IfMatch.Clear();
            var create = await client.PostAsJsonAsync(
                $"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/notes",
                new TaskNoteCreateDto { Content = "c1" });
            create.StatusCode.Should().Be(HttpStatusCode.Created);
            var note = (await create.Content.ReadFromJsonAsync<TaskNoteReadDto>(EndpointsTestHelper.Json))!;

            var get = await client.GetAsync($"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/notes/{note.Id}");
            get.StatusCode.Should().Be(HttpStatusCode.OK);
            get.Headers.ETag.Should().NotBeNull();

            EndpointsTestHelper.SetIfMatchFromRowVersion(client, note.RowVersion);
            var edit = await client.PatchAsJsonAsync(
                $"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/notes/{note.Id}/edit",
                new TaskNoteEditDto { NewContent = "c2" });
            edit.StatusCode.Should().Be(HttpStatusCode.OK);
            var edited = await edit.Content.ReadFromJsonAsync<TaskNoteReadDto>(EndpointsTestHelper.Json);

            EndpointsTestHelper.SetIfMatchFromRowVersion(client, note.RowVersion);
            var stale = await client.PatchAsJsonAsync(
                $"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/notes/{note.Id}/edit",
                new TaskNoteEditDto { NewContent = "c3" });
            stale.StatusCode.Should().Be((HttpStatusCode)412);

            client.DefaultRequestHeaders.IfMatch.Clear();
            var del428 = await client.DeleteAsync($"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/notes/{note.Id}");
            del428.StatusCode.Should().Be((HttpStatusCode)428);

            EndpointsTestHelper.SetIfMatchFromRowVersion(client, edited!.RowVersion);
            var del = await client.DeleteAsync($"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/notes/{note.Id}");
            del.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Me_200_And_ByUser_403_Then_200_As_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var (project, lane, column, task, user) = await EndpointsTestHelper.SetupBoard(client);
            client.DefaultRequestHeaders.IfMatch.Clear();

            (await client.PostAsJsonAsync(
                $"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/notes",
                new TaskNoteCreateDto { Content = "mine" })).EnsureSuccessStatusCode();

            var me = await client.GetAsync("/notes/me");
            me.StatusCode.Should().Be(HttpStatusCode.OK);

            var byUserForbidden = await client.GetAsync($"/notes/users/{user.UserId}");
            byUserForbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var admin = await EndpointsTestHelper.RegisterAndLoginAsync(client, name: "sys", email: "sys@x.com");
            var adminBearer = await UsersEndpointsTests.MintToken(app, admin.UserId, admin.Email, admin.Name, UserRole.Admin);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);

            var byUser = await client.GetAsync($"/notes/users/{user.UserId}");
            byUser.StatusCode.Should().Be(HttpStatusCode.OK);
        }

    }
}
