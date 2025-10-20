using Api.Auth.Authorization;
using Api.Extensions;
using Api.Helpers;
using Application.Common.Abstractions.Auth;
using Application.TaskNotes.Abstractions;
using Application.TaskNotes.DTOs;
using Application.TaskNotes.Mapping;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    public static class TaskNotesEndpoints
    {
        public static RouteGroupBuilder MapTaskNotes(this IEndpointRouteBuilder app)
        {
            // nested: single note
            var nested = app
                        .MapGroup("/projects/{projectId:guid}/lanes/{laneId:guid}/columns/{columnId:guid}/tasks/{taskId:guid}/notes")
                        .WithTags("Task Notes")
                        .RequireAuthorization(Policies.ProjectReader);

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

                var notes = await taskNoteReadSvc.ListByTaskAsync(taskId, ct);
                var responseDto = notes.Select(t => t.ToReadDto()).ToList();

                log.LogInformation("Task notes listed projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} count={Count}",
                                    projectId, laneId, columnId, taskId, responseDto.Count);
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

                var note = await taskNoteReadSvc.GetAsync(noteId, ct);
                if (note is null)
                {
                    log.LogInformation("Task note not found projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} noteId={NoteId}",
                                        projectId, laneId, columnId, taskId, noteId);
                    return Results.NotFound();
                }

                var responseDto = note.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                log.LogInformation("Task note fetched projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} noteId={NoteId} etag={ETag}",
                                    projectId, laneId, columnId, taskId, noteId, context.Response.Headers.ETag.ToString());
                return Results.Ok(responseDto);
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

                var userId = (Guid)currentUserSvc.UserId!;
                var (result, note) = await taskNoteWriteSvc.CreateAsync(projectId, taskId, userId, dto.Content, ct);
                if (result != DomainMutation.Created || note is null)
                {
                    log.LogInformation("Task note create rejected projectId={ProjectId} taskId={TaskId} userId={UserId} mutation={Mutation}",
                                        projectId, taskId, userId, result);
                    return result.ToHttp(context);
                }

                var responseDto = note.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                log.LogInformation("Task note created projectId={ProjectId} taskId={TaskId} noteId={NoteId} userId={UserId} etag={ETag}",
                                    projectId, taskId, note.Id, userId, context.Response.Headers.ETag.ToString());
                return Results.CreatedAtRoute("TaskNotes_Get_ById", new { projectId, laneId, columnId, taskId, noteId = note.Id }, responseDto);
            })
            .RequireAuthorization(Policies.ProjectMember)
            .RequireValidation<TaskNoteCreateDto>()
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

                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => taskNoteReadSvc.GetAsync(noteId, ct), n => n.RowVersion);

                var userId = (Guid)currentUserSvc.UserId!;
                var result = await taskNoteWriteSvc.EditAsync(projectId, taskId, noteId, userId, dto.NewContent, rowVersion, ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation("Task note edit rejected projectId={ProjectId} taskId={TaskId} noteId={NoteId} userId={UserId} mutation={Mutation}",
                                        projectId, taskId, noteId, userId, result);
                    return result.ToHttp(context);
                }

                var edited = await taskNoteReadSvc.GetAsync(noteId, ct);
                if (edited is null)
                {
                    log.LogInformation("Task note edit readback missing projectId={ProjectId} taskId={TaskId} noteId={NoteId}",
                                        projectId, taskId, noteId);
                    return Results.NotFound();
                }

                var responseDto = edited.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                log.LogInformation("Task note edited projectId={ProjectId} taskId={TaskId} noteId={NoteId} userId={UserId} etag={ETag}",
                                    projectId, taskId, noteId, userId, context.Response.Headers.ETag.ToString());
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.ProjectMember)
            .RequireValidation<TaskNoteEditDto>()
            .RequireIfMatch()
            .Produces<TaskNoteReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
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

                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => taskNoteReadSvc.GetAsync(noteId, ct), n => n.RowVersion);
                var userId = (Guid)currentUserSvc.UserId!;

                var result = await taskNoteWriteSvc.DeleteAsync(projectId, noteId, userId, rowVersion, ct);

                log.LogInformation("Task note deleted projectId={ProjectId} taskId={TaskId} noteId={NoteId} userId={UserId} mutation={Mutation}",
                                    projectId, taskId, noteId, userId, result);
                return result.ToHttp(context);
            })
            .RequireAuthorization(Policies.ProjectMember)
            .RequireIfMatch()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Delete task note")
            .WithDescription("Member-only. Deletes a note using optimistic concurrency (If-Match).")
            .WithName("Notes_Delete");

            // top-level: list by author
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

                var userId = (Guid)currentUserSvc.UserId!;
                var notes = await taskNoteReadSvc.ListByAuthorAsync(userId, ct);
                var responseDto = notes.Select(n => n.ToReadDto()).ToList();

                log.LogInformation("Task notes listed for current user userId={UserId} count={Count}",
                                    userId, responseDto.Count);
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

                var notes = await taskNoteReadSvc.ListByAuthorAsync(userId, ct);
                var responseDto = notes.Select(n => n.ToReadDto()).ToList();

                log.LogInformation("Task notes listed for userId={UserId} count={Count}",
                                    userId, responseDto.Count);
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.SystemAdmin)
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
