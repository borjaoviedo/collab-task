using Api.Auth.Authorization;
using Api.Extensions;
using Api.Filters;
using Application.TaskItems.Abstractions;
using Application.TaskItems.DTOs;
using Application.TaskItems.Mapping;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    public static class TaskItemEndpoints
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
            .WithName("Tasks_Get");

            // POST /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks
            group.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromBody] TaskItemCreateDto dto,
                [FromServices] ITaskItemWriteService svc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var (result, task) = await svc.CreateAsync(
                    projectId, laneId, columnId, dto.Title, dto.Description, dto.DueDate, dto.SortKey, ct);
                if (result != DomainMutation.Created) return result.ToHttp();

                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(task!.RowVersion)}\"";
                return Results.Created($"/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{task.Id}", task.ToReadDto());
            })
            .RequireAuthorization(Policies.ProjectMember)
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
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = (byte[])http.Items["rowVersion"]!;
                var result = await taskItemWriteSvc.EditAsync(taskId, dto.Title, dto.Description, dto.DueDate, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp();

                var edited = await taskItemReadSvc.GetAsync(taskId, ct);
                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(edited!.RowVersion)}\"";
                return Results.Ok(edited.ToReadDto());
            })
            .AddEndpointFilter<IfMatchRowVersionFilter>()
            .RequireAuthorization(Policies.ProjectMember)
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
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = (byte[])http.Items["rowVersion"]!;
                var result = await taskItemWriteSvc.MoveAsync(taskId, dto.TargetColumnId, dto.TargetLaneId, dto.TargetSortKey, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp();

                var moved = await taskItemReadSvc.GetAsync(taskId, ct);
                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(moved!.RowVersion)}\"";
                return Results.Ok(moved.ToReadDto());
            })
            .AddEndpointFilter<IfMatchRowVersionFilter>()
            .RequireAuthorization(Policies.ProjectMember)
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
                [FromServices] ITaskItemWriteService svc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = (byte[])http.Items["rowVersion"]!;
                var result = await svc.DeleteAsync(taskId, rowVersion, ct);
                return result.ToHttp();
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
            .WithSummary("Delete task")
            .WithDescription("Deletes a task.")
            .WithName("Tasks_Delete");

            return group;
        }
    }
}
