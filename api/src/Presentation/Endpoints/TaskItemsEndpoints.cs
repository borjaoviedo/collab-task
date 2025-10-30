using Api.Auth.Authorization;
using Api.Extensions;
using Api.Helpers;
using Application.Common.Abstractions.Auth;
using Application.TaskItems.Abstractions;
using Application.TaskItems.DTOs;
using Application.TaskItems.Mapping;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    /// <summary>
    /// Task item endpoints: list, read, create, edit, move, and delete.
    /// Uses per-endpoint auth and optimistic concurrency via ETag/If-Match.
    /// </summary>
    public static class TaskItemsEndpoints
    {
        /// <summary>
        /// Registers task item endpoints under the project/board hierarchy and wires handlers,
        /// validation, authorization, and OpenAPI metadata.
        /// </summary>
        /// <param name="app">Endpoint route builder.</param>
        /// <returns>The configured route group.</returns>
        public static RouteGroupBuilder MapTaskItems(this IEndpointRouteBuilder app)
        {
            // Group task endpoints under lane/column scope; requires ProjectReader by default
            var group = app
                .MapGroup("/projects/{projectId:guid}/lanes/{laneId:guid}/columns/{columnId:guid}/tasks")
                .WithTags("Tasks")
                .RequireAuthorization(Policies.ProjectReader);

            // OpenAPI metadata across all endpoints: ensures generated clients and API docs
            // include consistent success/error shapes, auth requirements, and concurrency responses

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks
            group.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ITaskItemReadService taskItemReadService,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskItems.Get_All");

                // Read-side list for the column; returns lightweight DTOs for client rendering
                var tasks = await taskItemReadService.ListByColumnAsync(columnId, ct);
                var responseDto = tasks.Select(t => t.ToReadDto()).ToList();

                log.LogInformation(
                    "Tasks listed projectId={ProjectId} laneId={LaneId} columnId={ColumnId} count={Count}",
                    projectId,
                    laneId,
                    columnId,
                    responseDto.Count);
                return Results.Ok(responseDto);
            })
            .Produces<IEnumerable<TaskItemReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List tasks")
            .WithDescription("Returns tasks for the column.")
            .WithName("Tasks_Get_All");

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}
            group.MapGet("/{taskId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ITaskItemReadService taskItemReadService,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskItems.Get_ById");

                // Fetch a single task. Return 404 if not found within the specified scope
                var task = await taskItemReadService.GetAsync(taskId, ct);
                if (task is null)
                {
                    log.LogInformation(
                        "Task not found projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId}",
                        projectId,
                        laneId,
                        columnId,
                        taskId);
                    return Results.NotFound();
                }

                // Attach weak ETag from RowVersion to enable conditional requests
                var responseDto = task.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Task fetched projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} etag={ETag}",
                    projectId,
                    laneId,
                    columnId,
                    taskId,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
            })
            .Produces<TaskItemReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get task")
            .WithDescription("Returns a task in the column. Sets ETag.")
            .WithName("Tasks_Get_ById");

            // POST /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks
            group.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromBody] TaskItemCreateDto dto,
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] ITaskItemWriteService taskItemWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskItems.Create");

                // Create a task. Title/Description are domain value objects; actor is the current user.
                // DomainMutation drives HTTP mapping; on success return canonical representation with ETag
                var userId = (Guid)currentUserSvc.UserId!;
                var taskTitle = TaskTitle.Create(dto.Title);
                var taskDescription = TaskDescription.Create(dto.Description);

                var (result, task) = await taskItemWriteSvc.CreateAsync(
                    projectId,
                    laneId,
                    columnId,
                    userId,
                    taskTitle,
                    taskDescription,
                    dto.DueDate,
                    dto.SortKey,
                    ct);

                if (result != DomainMutation.Created || task is null)
                {
                    log.LogInformation(
                        "Task create rejected projectId={ProjectId} laneId={LaneId} columnId={ColumnId} userId={UserId} mutation={Mutation}",
                        projectId,
                        laneId,
                        columnId,
                        userId,
                        result);
                    return result.ToHttp(context);
                }

                var responseDto = task.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Task created projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} userId={UserId} title={Title} etag={ETag}",
                    projectId,
                    laneId,
                    columnId,
                    task.Id,
                    userId,
                    dto.Title,
                    etag);

                var routevalues = new { projectId, laneId, columnId, taskId = task.Id };
                return Results.CreatedAtRoute("Tasks_Get_ById", routevalues, responseDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectMember) // ProjectMember-only
            .RequireValidation<TaskItemCreateDto>()
            .RejectIfMatch() // Reject If-Match on create: preconditions do not apply to new resources
            .Produces<TaskItemReadDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Create task")
            .WithDescription("Member-only. Creates a task in the column. Returns the resource with ETag.")
            .WithName("Tasks_Create");

            // PATCH /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/edit
            group.MapPatch("/{taskId:guid}/edit", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromBody] TaskItemEditDto dto,
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] ITaskItemReadService taskItemReadSvc,
                [FromServices] ITaskItemWriteService taskItemWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskItems.Edit");

                // Resolve current RowVersion from If-Match or storage to guard against lost updates
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => taskItemReadSvc.GetAsync(taskId, ct),
                    t => t.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation(
                        "Task item not found when resolving row version projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId}",
                        projectId,
                        laneId,
                        columnId,
                        taskId);
                    return Results.NotFound();
                }

                // Edit under optimistic concurrency. Title/Description VOs normalize inputs
                var userId = (Guid)currentUserSvc.UserId!;
                var taskTitle = TaskTitle.Create(dto.NewTitle!);
                var taskDescription = TaskDescription.Create(dto.NewDescription!);

                var result = await taskItemWriteSvc.EditAsync(
                    projectId,
                    taskId,
                    userId,
                    taskTitle,
                    taskDescription,
                    dto.NewDueDate,
                    rowVersion,
                    ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation(
                        "Task edit rejected projectId={ProjectId} taskId={TaskId} userId={UserId} mutation={Mutation}",
                        projectId,
                        taskId,
                        userId,
                        result);
                    return result.ToHttp(context);
                }

                // Read back to return fresh state and a new ETag
                var edited = await taskItemReadSvc.GetAsync(taskId, ct);
                if (edited is null)
                {
                    log.LogInformation(
                        "Task edit readback missing projectId={ProjectId} taskId={TaskId}",
                        projectId,
                        taskId);
                    return Results.NotFound();
                }

                var responseDto = edited.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Task edited projectId={ProjectId} taskId={TaskId} userId={UserId} newTitle={NewTitle} newDueDate={NewDueDate} etag={ETag}",
                    projectId,
                    taskId,
                    userId,
                    dto.NewTitle,
                    dto.NewDueDate,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectMember) // ProjectMember-only
            .RequireValidation<TaskItemEditDto>()
            .RequireIfMatch() // Require If-Match to prevent overwriting concurrent edits
            .Produces<TaskItemReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Edit task")
            .WithDescription("Member-only. Updates task fields using optimistic concurrency (If-Match). Returns the updated resource and ETag.")
            .WithName("Tasks_Edit");

            // PUT /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/move
            group.MapPut("/{taskId:guid}/move", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromBody] TaskItemMoveDto dto,
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] ITaskItemReadService taskItemReadSvc,
                [FromServices] ITaskItemWriteService taskItemWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskItems.Move");

                // Move task across lane/column with optimistic concurrency; server recomputes sort key if needed
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => taskItemReadSvc.GetAsync(taskId, ct),
                    t => t.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation(
                        "Task item not found when resolving row version projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId}",
                        projectId,
                        laneId,
                        columnId,
                        taskId);
                    return Results.NotFound();
                }

                var userId = (Guid)currentUserSvc.UserId!;
                var result = await taskItemWriteSvc.MoveAsync(
                    projectId,
                    taskId,
                    dto.NewColumnId,
                    dto.NewLaneId,
                    userId,
                    dto.NewSortKey,
                    rowVersion,
                    ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation(
                        "Task move rejected projectId={ProjectId} taskId={TaskId} userId={UserId} newLaneId={NewLaneId} newColumnId={NewColumnId} newSortKey={NewSortKey} mutation={Mutation}",
                        projectId,
                        taskId,
                        userId,
                        dto.NewLaneId,
                        dto.NewColumnId,
                        dto.NewSortKey,
                        result);
                    return result.ToHttp(context);
                }

                // Return updated representation and refreshed ETag
                var moved = await taskItemReadSvc.GetAsync(taskId, ct);
                if (moved is null)
                {
                    log.LogInformation(
                        "Task move readback missing projectId={ProjectId} taskId={TaskId}",
                        projectId,
                        taskId);
                    return Results.NotFound();
                }

                var responseDto = moved.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Task moved projectId={ProjectId} taskId={TaskId} userId={UserId} newLaneId={NewLaneId} newColumnId={NewColumnId} newSortKey={NewSortKey} etag={ETag}",
                    projectId,
                    taskId,
                    userId,
                    dto.NewLaneId,
                    dto.NewColumnId,
                    dto.NewSortKey,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectMember) // ProjectMember-only
            .RequireValidation<TaskItemMoveDto>()
            .RequireIfMatch() // Require If-Match to avoid applying moves on stale state
            .Produces<TaskItemReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Move task")
            .WithDescription("Member-only. Moves the task to another lane/column using optimistic concurrency (If-Match). Returns the updated resource and ETag.")
            .WithName("Tasks_Move");

            // DELETE /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}
            group.MapDelete("/{taskId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ITaskItemReadService taskItemReadSvc,
                [FromServices] ITaskItemWriteService taskItemWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskItems.Delete");

                // Conditional delete. Resolve RowVersion and map DomainMutation to HTTP
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => taskItemReadSvc.GetAsync(taskId, ct),
                    t => t.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation(
                        "Task item not found when resolving row version projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId}",
                        projectId,
                        laneId,
                        columnId,
                        taskId);
                    return Results.NotFound();
                }

                var result = await taskItemWriteSvc.DeleteAsync(projectId, taskId, rowVersion, ct);

                log.LogInformation(
                    "Task delete result projectId={ProjectId} taskId={TaskId} mutation={Mutation}",
                    projectId,
                    taskId,
                    result);

                // Map DomainMutation to HTTP (204, 404, 409, 412)
                return result.ToHttp(context);
            })
            .RequireAuthorization(Policies.ProjectMember) // ProjectMember-only
            .RequireIfMatch() // Member-only delete and requires If-Match to prevent removing a concurrently edited task
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Delete task")
            .WithDescription("Member-only. Deletes a task using optimistic concurrency (If-Match).")
            .WithName("Tasks_Delete");

            return group;
        }
    }
}
