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
            var group = app.MapGroup("/projects/{projectId:guid}/lanes/{laneId:guid}/columns/{columnId:guid}/tasks")
                .WithTags("Tasks")
                .RequireAuthorization(Policies.ProjectReader);

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks
            group.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromServices] ITaskItemReadService taskItemReadService,
                CancellationToken ct = default) =>
            {
                var tasks = await taskItemReadService.ListByColumnAsync(columnId, ct);
                var responseDto = tasks.Select(t => t.ToReadDto()).ToList();

                return Results.Ok(responseDto);
            })
            .Produces<IEnumerable<TaskItemReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("Get all tasks")
            .WithDescription("Returns tasks belonging to the specified column.")
            .WithName("Tasks_Get_All");

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}
            group.MapGet("/{taskId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromServices] ITaskItemReadService taskItemReadService,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var task = await taskItemReadService.GetAsync(taskId, ct);
                if (task is null) return Results.NotFound();

                var responseDto = task.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                return Results.Ok(responseDto);
            })
            .Produces<TaskItemReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get task")
            .WithDescription("Returns a task by id within a column.")
            .WithName("Tasks_Get_ById");

            // POST /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks
            group.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromBody] TaskItemCreateDto dto,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] ITaskItemWriteService taskItemWriteService,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var userId = (Guid)currentUserSvc.UserId!;
                var (result, task) = await taskItemWriteService.CreateAsync(
                    projectId, laneId, columnId, userId, dto.Title, dto.Description, dto.DueDate, dto.SortKey, ct);
                if (result != DomainMutation.Created || task is null) return result.ToHttp(context);

                var responseDto = task.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                return Results.CreatedAtRoute(
                    "Tasks_Get_ById",
                    new {projectId, laneId, columnId, taskId = task.Id },
                    responseDto);
            })
            .RequireAuthorization(Policies.ProjectMember)
            .RequireValidation<TaskItemCreateDto>()
            .Produces<TaskItemReadDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Create task")
            .WithDescription("Creates a task in the column and returns it.")
            .WithName("Tasks_Create");

            // PATCH /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/edit
            group.MapPatch("/{taskId:guid}/edit", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromBody] TaskItemEditDto dto,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] ITaskItemReadService taskItemReadSvc,
                [FromServices] ITaskItemWriteService taskItemWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => taskItemReadSvc.GetAsync(taskId, ct), t => t.RowVersion);

                var userId = (Guid)currentUserSvc.UserId!;
                var result = await taskItemWriteSvc.EditAsync(projectId, taskId, userId, dto.NewTitle, dto.NewDescription, dto.NewDueDate, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(context);

                var edited = await taskItemReadSvc.GetAsync(taskId, ct);
                if (edited is null) return Results.NotFound();

                var responseDto = edited.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

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
            .WithDescription("Edits a task and returns the updated task.")
            .WithName("Tasks_Edit");

            // PUT /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/move
            group.MapPut("/{taskId:guid}/move", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromBody] TaskItemMoveDto dto,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] ITaskItemReadService taskItemReadSvc,
                [FromServices] ITaskItemWriteService taskItemWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => taskItemReadSvc.GetAsync(taskId, ct), t => t.RowVersion);

                var userId = (Guid)currentUserSvc.UserId!;
                var result = await taskItemWriteSvc.MoveAsync(
                    projectId, taskId, dto.TargetColumnId, dto.TargetLaneId, userId, dto.TargetSortKey, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(context);

                var moved = await taskItemReadSvc.GetAsync(taskId, ct);
                if (moved is null) return Results.NotFound();

                var responseDto = moved.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

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
            .WithSummary("Move a task")
            .WithDescription("Moves a task to another column/lane in the same project.")
            .WithName("Tasks_Move");

            // DELETE /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}
            group.MapDelete("/{taskId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromServices] ITaskItemReadService taskItemReadSvc,
                [FromServices] ITaskItemWriteService taskItemWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => taskItemReadSvc.GetAsync(taskId, ct), t => t.RowVersion);

                var result = await taskItemWriteSvc.DeleteAsync(projectId, taskId, rowVersion, ct);

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
            .WithDescription("Deletes a task.")
            .WithName("Tasks_Delete");

            return group;
        }
    }
}
