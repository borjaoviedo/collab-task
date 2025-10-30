using Api.Auth.Authorization;
using Api.Extensions;
using Api.Helpers;
using Application.Common.Abstractions.Auth;
using Application.TaskNotes.Abstractions;
using Application.TaskNotes.DTOs;
using Application.TaskNotes.Mapping;
using Domain.Enums;
using Domain.ValueObjects;
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
        /// Registers task note endpoints under project/board hierarchy and global author routes.
        /// Wires handlers, validation, authorization, and OpenAPI metadata.
        /// </summary>
        /// <param name="app">Endpoint route builder.</param>
        /// <returns>The configured route group.</returns>
        public static RouteGroupBuilder MapTaskNotes(this IEndpointRouteBuilder app)
        {
            // Group task-scoped note endpoints; requires ProjectReader by default
            var nested = app
                .MapGroup("/projects/{projectId:guid}/lanes/{laneId:guid}/columns/{columnId:guid}/tasks/{taskId:guid}/notes")
                .WithTags("Task Notes")
                .RequireAuthorization(Policies.ProjectReader);

            // OpenAPI metadata across all endpoints: ensures generated clients and API docs
            // include consistent success/error shapes, auth requirements, and concurrency responses

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes
            nested.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ITaskNoteReadService taskNoteReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskNotes.Get_All");

                // Read-side list of notes for a task; returns lightweight DTOs for UI rendering
                var notes = await taskNoteReadSvc.ListByTaskAsync(taskId, ct);
                var responseDto = notes.Select(t => t.ToReadDto()).ToList();

                log.LogInformation(
                    "Task notes listed projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} count={Count}",
                    projectId,
                    laneId,
                    columnId,
                    taskId,
                    responseDto.Count);
                return Results.Ok(responseDto);
            })
            .Produces<IEnumerable<TaskNoteReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List task notes")
            .WithDescription("Returns notes for the task.")
            .WithName("Notes_Get_All");

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}
            nested.MapGet("/{noteId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromRoute] Guid noteId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ITaskNoteReadService taskNoteReadSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskNotes.Get_ById");

                // Fetch a single note by id within the task scope. Return 404 if not found
                var note = await taskNoteReadSvc.GetAsync(noteId, ct);
                if (note is null)
                {
                    log.LogInformation(
                        "Task note not found projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} noteId={NoteId}",
                        projectId,
                        laneId,
                        columnId,
                        taskId,
                        noteId);
                    return Results.NotFound();
                }

                // Attach weak ETag from RowVersion so clients can use conditional requests
                var responseDto = note.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Task note fetched projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} noteId={NoteId} etag={ETag}",
                    projectId,
                    laneId,
                    columnId,
                    taskId,
                    noteId,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
            })
            .Produces<TaskNoteReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get task note")
            .WithDescription("Returns a task note. Sets ETag.")
            .WithName("TaskNotes_Get_ById");

            // POST /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes
            nested.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromBody] TaskNoteCreateDto dto,
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] ITaskNoteWriteService taskNoteWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskNotes.Create");

                // Create a new note on the task. Content is validated via domain VO; actor is current user
                var userId = (Guid)currentUserSvc.UserId!;
                var noteContent = NoteContent.Create(dto.Content);

                var (result, note) = await taskNoteWriteSvc.CreateAsync(
                    projectId,
                    taskId,
                    userId,
                    noteContent,
                    ct);
                if (result != DomainMutation.Created || note is null)
                {
                    log.LogInformation(
                        "Task note create rejected projectId={ProjectId} taskId={TaskId} userId={UserId} mutation={Mutation}",
                        projectId,
                        taskId,
                        userId,
                        result);
                    return result.ToHttp(context);
                }

                // Return canonical representation with ETag and Location
                var responseDto = note.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Task note created projectId={ProjectId} taskId={TaskId} noteId={NoteId} userId={UserId} etag={ETag}",
                    projectId,
                    taskId,
                    note.Id,
                    userId,
                    etag);

                var routeValues = new { projectId, laneId, columnId, taskId, noteId = note.Id };
                return Results.CreatedAtRoute("TaskNotes_Get_ById", routeValues, responseDto).WithETag(etag);
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

            // PATCH /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}/edit
            nested.MapPatch("/{noteId:guid}/edit", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromRoute] Guid noteId,
                [FromBody] TaskNoteEditDto dto,
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] ITaskNoteReadService taskNoteReadSvc,
                [FromServices] ITaskNoteWriteService taskNoteWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskNotes.Edit");

                // Resolve current RowVersion from If-Match or storage to guard against lost updates
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => taskNoteReadSvc.GetAsync(noteId, ct),
                    n => n.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation(
                        "Task note not found when resolving row version projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} noteId={NoteId}",
                        projectId,
                        laneId,
                        columnId,
                        taskId,
                        noteId);
                    return Results.NotFound();
                }

                // Edit under optimistic concurrency; content normalized via domain VO
                var userId = (Guid)currentUserSvc.UserId!;
                var noteContent = NoteContent.Create(dto.NewContent);

                var result = await taskNoteWriteSvc.EditAsync(
                    projectId,
                    taskId,
                    noteId,
                    userId,
                    noteContent,
                    rowVersion,
                    ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation(
                        "Task note edit rejected projectId={ProjectId} taskId={TaskId} noteId={NoteId} userId={UserId} mutation={Mutation}",
                        projectId,
                        taskId,
                        noteId,
                        userId,
                        result);
                    return result.ToHttp(context);
                }

                // Return fresh state with a new ETag
                var edited = await taskNoteReadSvc.GetAsync(noteId, ct);
                if (edited is null)
                {
                    log.LogInformation(
                        "Task note edit readback missing projectId={ProjectId} taskId={TaskId} noteId={NoteId}",
                        projectId,
                        taskId,
                        noteId);
                    return Results.NotFound();
                }

                var responseDto = edited.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Task note edited projectId={ProjectId} taskId={TaskId} noteId={NoteId} userId={UserId} etag={ETag}",
                    projectId,
                    taskId,
                    noteId,
                    userId,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectMember) // ProjectMember-only
            .RequireValidation<TaskNoteEditDto>()
            .RequireIfMatch() // Require If-Match to prevent overwriting concurrent edits
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

            // DELETE /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/notes/{noteId}
            nested.MapDelete("/{noteId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromRoute] Guid noteId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] ITaskNoteReadService taskNoteReadSvc,
                [FromServices] ITaskNoteWriteService taskNoteWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskNotes.Delete");

                // Conditional delete of a note; includes actor for audit log
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => taskNoteReadSvc.GetAsync(noteId, ct),
                    n => n.RowVersion,
                    ct);
                var userId = (Guid)currentUserSvc.UserId!;

                if (rowVersion is null)
                {
                    log.LogInformation(
                        "Task note not found when resolving row version projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} noteId={NoteId}",
                        projectId,
                        laneId,
                        columnId,
                        taskId,
                        noteId);
                    return Results.NotFound();
                }

                var result = await taskNoteWriteSvc.DeleteAsync(projectId, noteId, userId, rowVersion, ct);

                log.LogInformation(
                    "Task note deleted projectId={ProjectId} taskId={TaskId} noteId={NoteId} userId={UserId} mutation={Mutation}",
                    projectId,
                    taskId,
                    noteId,
                    userId,
                    result);

                // Map DomainMutation to HTTP (204, 404, 409, 412)
                return result.ToHttp(context);
            })
            .RequireAuthorization(Policies.ProjectMember) // ProjectMember-only
            .RequireIfMatch() // Member-only and requires If-Match to avoid deleting over stale state
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


            // Global author-centric queries for task notes (current user and admin)
            var top = app.MapGroup("/notes")
                .WithTags("Task Notes")
                .RequireAuthorization();

            // GET /notes/me
            top.MapGet("/me", async (
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] ITaskNoteReadService taskNoteReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskNotes.Get_Mine");

                // Lists notes authored by the current user across accessible projects
                var userId = (Guid)currentUserSvc.UserId!;
                var notes = await taskNoteReadSvc.ListByUserAsync(userId, ct);
                var responseDto = notes.Select(n => n.ToReadDto()).ToList();

                log.LogInformation(
                    "Task notes listed for current user userId={UserId} count={Count}",
                    userId,
                    responseDto.Count);
                return Results.Ok(responseDto);
            })
            .Produces<IEnumerable<TaskNoteReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("List my notes")
            .WithDescription("Returns notes authored by the authenticated user.")
            .WithName("TaskNotes_Get_Mine");

            // GET /notes/users/{userId}
            top.MapGet("/users/{userId:guid}", async (
                [FromRoute] Guid userId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ITaskNoteReadService taskNoteReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskNotes.Get_ByUser");

                // Admin-only listing of notes authored by a specific user
                var notes = await taskNoteReadSvc.ListByUserAsync(userId, ct);
                var responseDto = notes.Select(n => n.ToReadDto()).ToList();

                log.LogInformation(
                    "Task notes listed for userId={UserId} count={Count}",
                    userId,
                    responseDto.Count);
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.SystemAdmin) // SystemAdmin-only
            .Produces<IEnumerable<TaskNoteReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List notes by user")
            .WithDescription("Admin-only. Returns notes authored by the specified user.")
            .WithName("TaskNotes_Get_ByUser");

            return top;
        }
    }
}
