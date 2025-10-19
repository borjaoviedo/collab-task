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
                [FromServices] ITaskItemReadService svc,
                CancellationToken ct = default) =>
            {
                var tasks = await svc.ListByColumnAsync(columnId, ct);
                var dto = tasks.Select(t => t.ToReadDto()).ToList();
                return Results.Ok(dto);
            })
            .Produces<IEnumerable<TaskItemReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get all tasks")
            .WithDescription("Returns tasks belonging to the specified column.")
            .WithName("Tasks_Get_All");

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}
            group.MapGet("/{taskId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromServices] ITaskItemReadService taskReadSvc,
                CancellationToken ct = default) =>
            {
                var task = await taskReadSvc.GetAsync(taskId, ct);
                return task is null ? Results.NotFound() : Results.Ok(task.ToReadDto());
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
                [FromServices] ITaskItemWriteService svc,
                [FromServices] ICurrentUserService currentUserSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var userId = (Guid)currentUserSvc.UserId!;

                var (result, task) = await svc.CreateAsync(
                    projectId, laneId, columnId, userId,
                    dto.Title, dto.Description, dto.DueDate, dto.SortKey, ct);
                if (result != DomainMutation.Created || task is null) return result.ToHttp();

                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(task.RowVersion)}\"";

                return Results.CreatedAtRoute(
                    "Tasks_Get_ById",
                    new {projectId, laneId, columnId, taskId = task.Id },
                    task.ToReadDto());
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
                [FromServices] ITaskItemWriteService taskItemWriteSvc,
                [FromServices] ITaskItemReadService taskItemReadSvc,
                [FromServices] ICurrentUserService currentUserSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    http, () => taskItemReadSvc.GetAsync(taskId, ct), t => t.RowVersion);

                var userId = (Guid)currentUserSvc.UserId!;
                var result = await taskItemWriteSvc.EditAsync(projectId, taskId, userId, dto.NewTitle, dto.NewDescription, dto.NewDueDate, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(http);

                var edited = await taskItemReadSvc.GetAsync(taskId, ct);
                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(edited!.RowVersion)}\"";
                return Results.Ok(edited.ToReadDto());
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
                [FromServices] ITaskItemWriteService taskItemWriteSvc,
                [FromServices] ITaskItemReadService taskItemReadSvc,
                [FromServices] ICurrentUserService currentUserSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    http, () => taskItemReadSvc.GetAsync(taskId, ct), t => t.RowVersion);

                var userId = (Guid)currentUserSvc.UserId!;
                var result = await taskItemWriteSvc.MoveAsync(projectId, taskId, dto.TargetColumnId, dto.TargetLaneId, userId,
                    dto.TargetSortKey, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(http);

                var moved = await taskItemReadSvc.GetAsync(taskId, ct);
                if (moved is null) return Results.NotFound();

                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(moved.RowVersion)}\"";

                return Results.Ok(moved.ToReadDto());
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
                [FromServices] ITaskItemWriteService taskItemWriteSvc,
                [FromServices] ITaskItemReadService taskItemReadSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    http, () => taskItemReadSvc.GetAsync(taskId, ct), t => t.RowVersion);

                var result = await taskItemWriteSvc.DeleteAsync(projectId, taskId, rowVersion, ct);
                return result.ToHttp(http);
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
