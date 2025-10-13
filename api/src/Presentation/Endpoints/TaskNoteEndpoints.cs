using Api.Auth.Authorization;
using Api.Extensions;
using Api.Filters;
using Application.Common.Abstractions.Auth;
using Application.TaskNotes.Abstractions;
using Application.TaskNotes.DTOs;
using Application.TaskNotes.Mapping;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    public static class TaskNoteEndpoints
    {
        public static RouteGroupBuilder MapTaskNotes(this IEndpointRouteBuilder app)
        {
            // nested: single note
            var nested = app.MapGroup("/projects/{projectId:guid}/lanes/{laneId:guid}/columns/{columnId:guid}/tasks/{taskId:guid}/notes")
                .WithTags("Task Notes")
                .RequireAuthorization(Policies.ProjectReader);

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes
            nested.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromServices] ITaskNoteReadService svc,
                CancellationToken ct = default) =>
            {
                var notes = await svc.ListByTaskAsync(taskId, ct);
                var dto = notes.Select(t => t.ToReadDto()).ToList();
                return Results.Ok(dto);
            })
            .Produces<IEnumerable<TaskNoteReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get all notes")
            .WithDescription("Returns notes belonging to the specified task.")
            .WithName("Notes_Get_All");

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}
            nested.MapGet("/{noteId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromRoute] Guid noteId,
                [FromServices] ITaskNoteReadService noteReadSvc,
                CancellationToken ct = default) =>
            {
                var note = await noteReadSvc.GetAsync(noteId, ct);
                return note is null ? Results.NotFound() : Results.Ok(note.ToReadDto());
            })
            .Produces<TaskNoteReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithDescription("Returns a task note by id.")
            .WithSummary("Get task note")
            .WithName("TaskNotes_Get");

            // POST /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes
            nested.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromBody] TaskNoteCreateDto dto,
                [FromServices] ITaskNoteWriteService taskNoteWriteSvc,
                [FromServices] ICurrentUserService currentUserSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var authorId = (Guid)currentUserSvc.UserId!;
                var (result, note) = await taskNoteWriteSvc.CreateAsync(taskId, authorId, dto.Content, ct);
                if (result != DomainMutation.Created) return result.ToHttp();

                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(note!.RowVersion)}\"";
                return Results.Created($"/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{note.Id}", note.ToReadDto());
            })
            .RequireAuthorization(Policies.ProjectMember)
            .Produces<TaskNoteReadDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Create note")
            .WithDescription("Creates a note in the task and returns it.")
            .WithName("Notes_Create");

            // PATCH /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}/edit
            nested.MapPatch("/{noteId:guid}/edit", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromRoute] Guid noteId,
                [FromBody] TaskNoteEditDto dto,
                [FromServices] ITaskNoteWriteService taskNoteWriteSvc,
                [FromServices] ITaskNoteReadService taskNoteReadSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = (byte[])http.Items["rowVersion"]!;
                var result = await taskNoteWriteSvc.EditAsync(noteId, dto.NewContent, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(http);

                var edited = await taskNoteReadSvc.GetAsync(noteId, ct);
                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(edited!.RowVersion)}\"";
                return Results.Ok(edited.ToReadDto());
            })
            .AddEndpointFilter<IfMatchRowVersionFilter>()
            .RequireAuthorization(Policies.ProjectMember)
            .Produces<TaskNoteReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Edit note")
            .WithDescription("Edits a note and returns the updated note.")
            .WithName("Notes_Edit");

            // DELETE /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}
            nested.MapDelete("/{noteId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromRoute] Guid noteId,
                [FromServices] ITaskNoteWriteService svc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = (byte[])http.Items["rowVersion"]!;
                var result = await svc.DeleteAsync(noteId, rowVersion, ct);
                return result.ToHttp(http);
            })
            .AddEndpointFilter<IfMatchRowVersionFilter>()
            .RequireAuthorization(Policies.ProjectMember)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Delete note")
            .WithDescription("Deletes a note.")
            .WithName("Notes_Delete");

            // top-level: list by author
            var top = app.MapGroup("/notes")
                .WithTags("Task Notes")
                .RequireAuthorization();

            // GET /notes/me
            top.MapGet("/me", async (
                HttpContext http,
                [FromServices] ITaskNoteReadService noteReadSvc,
                [FromServices] ICurrentUserService currentUserSvc,
                CancellationToken ct = default) =>
            {
                var userId = (Guid)currentUserSvc.UserId!;
                var notes = await noteReadSvc.ListByAuthorAsync(userId, ct);

                var dto = notes.Select(n => n.ToReadDto()).ToList();
                return Results.Ok(dto);
            })
            .Produces<IEnumerable<TaskNoteReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("List my notes")
            .WithDescription("Lists notes authored by the authenticated user across accessible projects.")
            .WithName("TaskNotes_ListMine");

            // GET /notes/users/{userId}
            top.MapGet("/users/{userId:guid}", async (
                [FromRoute] Guid userId,
                [FromServices] ITaskNoteReadService noteReadSvc,
                CancellationToken ct = default) =>
            {
                var items = await noteReadSvc.ListByAuthorAsync(userId, ct);
                var dto = items.Select(n => n.ToReadDto()).ToList();
                return Results.Ok(dto);
            })
            .Produces<IEnumerable<TaskNoteReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List notes by author")
            .WithDescription("Lists notes authored by the specified user across accessible projects.")
            .WithName("TaskNotes_ListByUser");

            return top;
        }
    }
}
