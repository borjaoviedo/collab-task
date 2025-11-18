using Api.Auth.Authorization;
using Api.Concurrency;
using Api.Filters;
using Application.TaskNotes.Abstractions;
using Application.TaskNotes.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    /// <summary>
    /// Task note endpoints: list, read, create, edit, and delete.
    /// Uses per-endpoint authorization and optimistic concurrency via ETag/If-Match.
    /// </summary>
    public static class TaskNotesEndpoints
    {
        /// <summary>
        /// Registers task note endpoints under:
        /// - /projects/{projectId}/tasks/{taskId}/notes (create/edit/delete),
        /// - /tasks/{taskId}/notes (list by task),
        /// - /notes/{noteId} (get by id),
        /// - /notes (author-centric global queries).
        /// Wires handlers, validation, authorization, and OpenAPI metadata.
        /// </summary>
        /// <param name="app">Endpoint route builder.</param>
        /// <returns>The configured route group for global note endpoints.</returns>
        public static RouteGroupBuilder MapTaskNotes(this IEndpointRouteBuilder app)
        {
            // /projects/{projectId}/tasks/{taskId}/notes
            var projectTaskNotesGroup = app
                .MapGroup("/projects/{projectId:guid}/tasks/{taskId:guid}/notes")
                .WithTags("Task Notes")
                .RequireAuthorization(Policies.ProjectReader);

            // OpenAPI metadata across all endpoints: ensures generated clients and API docs
            // include consistent success/error shapes, auth requirements, and concurrency responses


            // GET /projects/{projectId}/tasks/{taskId}/notes
            projectTaskNotesGroup.MapGet("/", async (
                [FromRoute] Guid taskId,
                [FromServices] ITaskNoteReadService taskNoteReadSvc,
                CancellationToken ct = default) =>
            {
                var taskNoteReadDtoList = await taskNoteReadSvc.ListByTaskIdAsync(taskId, ct);
                return Results.Ok(taskNoteReadDtoList);
            })
            .Produces<IEnumerable<TaskNoteReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List task notes")
            .WithDescription("Returns notes for the task.")
            .WithName("Notes_Get_All");

            // POST /projects/{projectId}/tasks/{taskId}/notes
            projectTaskNotesGroup.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid taskId,
                [FromBody] TaskNoteCreateDto dto,
                [FromServices] ITaskNoteWriteService taskNoteWriteSvc,
                CancellationToken ct = default) =>
            {
                var taskNoteReadDto = await taskNoteWriteSvc.CreateAsync(
                    projectId,
                    taskId,
                    dto,
                    ct);
                var etag = ETag.EncodeWeak(taskNoteReadDto.RowVersion);

                var routeValues = new { noteId = taskNoteReadDto.Id };
                return Results
                    .CreatedAtRoute("TaskNotes_Get_ById", routeValues, taskNoteReadDto)
                    .WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectMember) // ProjectMember-only
            .RequireValidation<TaskNoteCreateDto>()
            .RejectIfMatch() // Reject If-Match on create: preconditions do not apply to new resources
            .Produces<TaskNoteReadDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Create task note")
            .WithDescription("Member-only. Creates a note on the task. Returns the resource with ETag.")
            .WithName("Notes_Create");

            // PATCH /projects/{projectId}/tasks/{taskId}/notes/{noteId}/edit
            projectTaskNotesGroup.MapPatch("/{noteId:guid}/edit", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid taskId,
                [FromRoute] Guid noteId,
                [FromBody] TaskNoteEditDto dto,
                [FromServices] ITaskNoteWriteService taskNoteWriteSvc,
                CancellationToken ct = default) =>
            {
                var taskNoteReadDto = await taskNoteWriteSvc.EditAsync(
                    projectId,
                    taskId,
                    noteId,
                    dto,
                    ct);
                var etag = ETag.EncodeWeak(taskNoteReadDto.RowVersion);

                return Results.Ok(taskNoteReadDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectMember) // ProjectMember-only
            .RequireValidation<TaskNoteEditDto>()
            .RequireIfMatch() // Require If-Match to prevent overwriting concurrent edits
            .EnsureIfMatch<ITaskNoteReadService, TaskNoteReadDto>(routeValueKey: "noteId")
            .Produces<TaskNoteReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Edit task note")
            .WithDescription("Member-only. Updates a note using optimistic concurrency (If-Match). Returns the updated resource and ETag.")
            .WithName("Notes_Edit");

            // DELETE /projects/{projectId}/tasks/{taskId}/notes/{noteId}
            projectTaskNotesGroup.MapDelete("/{noteId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid taskId,
                [FromRoute] Guid noteId,
                [FromServices] ITaskNoteWriteService taskNoteWriteSvc,
                CancellationToken ct = default) =>
            {
                await taskNoteWriteSvc.DeleteAsync(projectId, taskId, noteId, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Policies.ProjectMember) // ProjectMember-only
            .RequireIfMatch() // Member-only and requires If-Match to avoid deleting over stale state
            .EnsureIfMatch<ITaskNoteReadService, TaskNoteReadDto>(routeValueKey: "noteId")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Delete task note")
            .WithDescription("Member-only. Deletes a note using optimistic concurrency (If-Match).")
            .WithName("Notes_Delete");


            // /projects/{projectId}/notes/{noteId}
            var noteGroup = app
                .MapGroup("/projects/{projectId:guid}/notes/{noteId:guid}")
                .WithTags("Task Notes")
                .RequireAuthorization(Policies.ProjectReader);

            // GET /projects/{projectId}/notes/{noteId}
            noteGroup.MapGet("/", async (
                [FromRoute] Guid noteId,
                [FromServices] ITaskNoteReadService taskNoteReadSvc,
                CancellationToken ct = default) =>
            {
                var taskNoteReadDto = await taskNoteReadSvc.GetByIdAsync(noteId, ct);
                var etag = ETag.EncodeWeak(taskNoteReadDto.RowVersion);

                return Results.Ok(taskNoteReadDto).WithETag(etag);
            })
            .Produces<TaskNoteReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get task note")
            .WithDescription("Returns a task note. Sets ETag.")
            .WithName("TaskNotes_Get_ById");


            // Global author-centric queries for task notes (current user and admin)
            var notesGroup = app.MapGroup("/notes")
                .WithTags("Task Notes")
                .RequireAuthorization();

            // GET /notes/me
            notesGroup.MapGet("/me", async (
                [FromServices] ITaskNoteReadService taskNoteReadSvc,
                CancellationToken ct = default) =>
            {
                var taskNoteReadDtoList = await taskNoteReadSvc.ListSelfAsync(ct);
                return Results.Ok(taskNoteReadDtoList);
            })
            .Produces<IEnumerable<TaskNoteReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("List my notes")
            .WithDescription("Returns notes authored by the authenticated user.")
            .WithName("TaskNotes_Get_Mine");

            // GET /notes/users/{userId}
            notesGroup.MapGet("/users/{userId:guid}", async (
                [FromRoute] Guid userId,
                [FromServices] ITaskNoteReadService taskNoteReadSvc,
                CancellationToken ct = default) =>
            {
                var taskNoteReadDtoList = await taskNoteReadSvc.ListByUserIdAsync(userId, ct);
                return Results.Ok(taskNoteReadDtoList);
            })
            .RequireAuthorization(Policies.SystemAdmin) // SystemAdmin-only
            .Produces<IEnumerable<TaskNoteReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List notes by user")
            .WithDescription("Admin-only. Returns notes authored by the specified user.")
            .WithName("TaskNotes_Get_ByUser");

            return notesGroup;
        }
    }
}
