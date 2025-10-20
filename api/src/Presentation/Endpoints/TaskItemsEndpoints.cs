using Api.Auth.Authorization;
using Api.Extensions;
using Api.Helpers;
using Application.Common.Abstractions.Auth;
using Application.TaskItems.Abstractions;
using Application.TaskItems.DTOs;
using Application.TaskItems.Mapping;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    public static class TaskItemsEndpoints
    {
        public static RouteGroupBuilder MapTaskItems(this IEndpointRouteBuilder app)
        {
            var group = app
                        .MapGroup("/projects/{projectId:guid}/lanes/{laneId:guid}/columns/{columnId:guid}/tasks")
                        .WithTags("Tasks")
                        .RequireAuthorization(Policies.ProjectReader);

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

                var tasks = await taskItemReadService.ListByColumnAsync(columnId, ct);
                var responseDto = tasks.Select(t => t.ToReadDto()).ToList();

                log.LogInformation("Tasks listed projectId={ProjectId} laneId={LaneId} columnId={ColumnId} count={Count}",
                                    projectId, laneId, columnId, responseDto.Count);
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

                var task = await taskItemReadService.GetAsync(taskId, ct);
                if (task is null)
                {
                    log.LogInformation("Task not found projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId}",
                                        projectId, laneId, columnId, taskId);
                    return Results.NotFound();
                }

                var responseDto = task.ToReadDto();
                context.Response.SetETag(responseDto.RowVersion);

                log.LogInformation("Task fetched projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} etag={ETag}",
                                    projectId, laneId, columnId, taskId, context.Response.Headers.ETag.ToString());
                return Results.Ok(responseDto);
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
                [FromServices] ITaskItemWriteService taskItemWriteService,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskItems.Create");

                var userId = (Guid)currentUserSvc.UserId!;
                var (result, task) = await taskItemWriteService.CreateAsync(
                    projectId, laneId, columnId, userId, dto.Title, dto.Description, dto.DueDate, dto.SortKey, ct);
                if (result != DomainMutation.Created || task is null)
                {
                    log.LogInformation("Task create rejected projectId={ProjectId} laneId={LaneId} columnId={ColumnId} userId={UserId} mutation={Mutation}",
                                        projectId, laneId, columnId, userId, result);
                    return result.ToHttp(context);
                }

                var responseDto = task.ToReadDto();
                context.Response.SetETag(responseDto.RowVersion);

                log.LogInformation("Task created projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} userId={UserId} title={Title} etag={ETag}",
                                    projectId, laneId, columnId, task.Id, userId, dto.Title, context.Response.Headers.ETag.ToString());
                return Results.CreatedAtRoute("Tasks_Get_ById", new { projectId, laneId, columnId, taskId = task.Id }, responseDto);
            })
            .RequireAuthorization(Policies.ProjectMember)
            .RequireValidation<TaskItemCreateDto>()
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

                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => taskItemReadSvc.GetAsync(taskId, ct), t => t.RowVersion);

                var userId = (Guid)currentUserSvc.UserId!;
                var result = await taskItemWriteSvc.EditAsync(projectId, taskId, userId, dto.NewTitle, dto.NewDescription, dto.NewDueDate, rowVersion, ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation("Task edit rejected projectId={ProjectId} taskId={TaskId} userId={UserId} mutation={Mutation}",
                                        projectId, taskId, userId, result);
                    return result.ToHttp(context);
                }

                var edited = await taskItemReadSvc.GetAsync(taskId, ct);
                if (edited is null)
                {
                    log.LogInformation("Task edit readback missing projectId={ProjectId} taskId={TaskId}",
                                        projectId, taskId);
                    return Results.NotFound();
                }

                var responseDto = edited.ToReadDto();
                context.Response.SetETag(responseDto.RowVersion);

                log.LogInformation("Task edited projectId={ProjectId} taskId={TaskId} userId={UserId} newTitle={NewTitle} newDueDate={NewDueDate} etag={ETag}",
                                    projectId, taskId, userId, dto.NewTitle, dto.NewDueDate, context.Response.Headers.ETag.ToString());
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.ProjectMember)
            .RequireValidation<TaskItemEditDto>()
            .RequireIfMatch()
            .Produces<TaskItemReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
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

                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => taskItemReadSvc.GetAsync(taskId, ct), t => t.RowVersion);

                var userId = (Guid)currentUserSvc.UserId!;
                var result = await taskItemWriteSvc.MoveAsync(
                    projectId, taskId, dto.TargetColumnId, dto.TargetLaneId, userId, dto.TargetSortKey, rowVersion, ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation("Task move rejected projectId={ProjectId} taskId={TaskId} userId={UserId} targetLaneId={TargetLaneId} targetColumnId={TargetColumnId} mutation={Mutation}",
                                        projectId, taskId, userId, dto.TargetLaneId, dto.TargetColumnId, result);
                    return result.ToHttp(context);
                }

                var moved = await taskItemReadSvc.GetAsync(taskId, ct);
                if (moved is null)
                {
                    log.LogInformation("Task move readback missing projectId={ProjectId} taskId={TaskId}",
                                        projectId, taskId);
                    return Results.NotFound();
                }

                var responseDto = moved.ToReadDto();
                context.Response.SetETag(responseDto.RowVersion);

                log.LogInformation("Task moved projectId={ProjectId} taskId={TaskId} userId={UserId} targetLaneId={TargetLaneId} targetColumnId={TargetColumnId} targetSortKey={TargetSortKey} etag={ETag}",
                                    projectId, taskId, userId, dto.TargetLaneId, dto.TargetColumnId, dto.TargetSortKey, context.Response.Headers.ETag.ToString());
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.ProjectMember)
            .RequireValidation<TaskItemMoveDto>()
            .RequireIfMatch()
            .Produces<TaskItemReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
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

                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => taskItemReadSvc.GetAsync(taskId, ct), t => t.RowVersion);

                var result = await taskItemWriteSvc.DeleteAsync(projectId, taskId, rowVersion, ct);

                log.LogInformation("Task delete result projectId={ProjectId} taskId={TaskId} mutation={Mutation}",
                                    projectId, taskId, result);
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
            .WithSummary("Delete task")
            .WithDescription("Member-only. Deletes a task using optimistic concurrency (If-Match).")
            .WithName("Tasks_Delete");

            return group;
        }
    }
}
