using Api.Tests.Testing;
using Application.TaskNotes.DTOs;
using Domain.Enums;
using FluentAssertions;
using System.Net;
using TestHelpers.Api.Auth;
using TestHelpers.Api.Http;
using TestHelpers.Api.Projects;
using TestHelpers.Api.TaskNotes;

namespace Api.Tests.Endpoints
{
    public sealed class TaskNotesEndpointsTests
    {
        [Fact]
        public async Task List_Create_GetById_ETag_Edit_Delete_Concurrency()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // Create full board (without note): project, lane, column and task
            var (project, lane, column, task, _) = await BoardSetupHelper.SetupBoard(client);

            // Get list (empty)
            var noteListResponse = await TaskNoteTestHelper.GetTaskNotesResponseAsync(
                client,
                project.Id,
                lane.Id,
                column.Id,
                task.Id);
            noteListResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var notes = await noteListResponse.ReadContentAsDtoAsync<List<TaskNoteReadDto>>();
            notes.Should().BeEmpty();

            // Create note
            var note = await TaskNoteTestHelper.PostNoteDtoAsync(
                client,
                project.Id,
                lane.Id,
                column.Id,
                task.Id);

            // Get note by id
            var getByIdResponse = await TaskNoteTestHelper.GetTaskNoteByIdResponseAsync(
                client,
                project.Id,
                lane.Id,
                column.Id,
                task.Id,
                note.Id);
            getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            getByIdResponse.Headers.ETag.Should().NotBeNull();

            // Valid note edition
            var editResponse = await TaskNoteTestHelper.EditNoteResponseAsync(
                client,
                project.Id,
                lane.Id,
                column.Id,
                task.Id,
                note.Id,
                note.RowVersion);
            editResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Get edited note
            var editedNote = await editResponse.ReadContentAsDtoAsync<TaskNoteReadDto>();

            // Edit with old row version: 412
            var differentEditDto = new TaskNoteEditDto() { NewContent = "different edition" };
            var staleResponse = await TaskNoteTestHelper.EditNoteResponseAsync(
                client,
                project.Id,
                lane.Id,
                column.Id,
                task.Id,
                note.Id,
                note.RowVersion,
                differentEditDto);
            staleResponse.StatusCode.Should().Be((HttpStatusCode)412);

            // Delete with missing ifmatch: 428
            client.DefaultRequestHeaders.IfMatch.Clear();
            var del428 = await client.DeleteAsync(
                $"/projects/{project.Id}/lanes/{lane.Id}/columns/{column.Id}/tasks/{task.Id}/notes/{note.Id}");
            del428.StatusCode.Should().Be((HttpStatusCode)428);

            // Correct delete: 204
            var deleteResponse = await TaskNoteTestHelper.DeleteNoteResponseAsync(
                client,
                project.Id,
                lane.Id,
                column.Id,
                task.Id,
                note.Id,
                editedNote.RowVersion);
            deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Me_200_And_ByUser_403_Then_200_As_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // Create full board: project, lane, column, task and note
            var (_, _, _, _, _, user) = await BoardSetupHelper.SetupBoardWithNote(client);

            // Get me: OK
            var getMeResponse = await client.GetAsync("/notes/me");
            getMeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            // Get other user notes without being admin: forbidden
            var getNotesByUserIdResponse = await TaskNoteTestHelper.GetTaskNotesByUserResponseAsync(client, user.UserId);
            getNotesByUserIdResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

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
            getNotesByUserIdResponse = await TaskNoteTestHelper.GetTaskNotesByUserResponseAsync(client, user.UserId);
            getNotesByUserIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }
}
