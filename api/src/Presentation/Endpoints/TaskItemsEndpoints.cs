using Api.Auth.Authorization;
using Api.Concurrency;
using Api.Filters;
using Application.TaskItems.Abstractions;
using Application.TaskItems.DTOs;
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
        /// Registers task item endpoints under:
        /// - /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks (create),
        /// - /columns/{columnId}/tasks (list by column),
        /// - /projects/{projectId}/tasks (task item operations).
        /// Wires handlers, validation, authorization, and OpenAPI metadata.
        /// </summary>
        /// <param name="app">Endpoint route builder.</param>
        /// <returns>The configured route group for project-scoped task item operations.</returns>
        public static RouteGroupBuilder MapTaskItems(this IEndpointRouteBuilder app)
        {
            // /columns/{columnId}/tasks
            var columnsTasksGroup = app
                .MapGroup("/columns/{columnId:guid}/tasks")
                .WithTags("Tasks")
                .RequireAuthorization(Policies.ProjectReader);

            // OpenAPI metadata across all endpoints: ensures generated clients and API docs
            // include consistent success/error shapes, auth requirements, and concurrency responses


            // GET /columns/{columnId}/tasks
            columnsTasksGroup.MapGet("/", async (
                [FromRoute] Guid columnId,
                [FromServices] ITaskItemReadService taskItemReadService,
                CancellationToken ct = default) =>
            {
                var taskItemReadDtoList = await taskItemReadService.ListByColumnIdAsync(columnId, ct);
                return Results.Ok(taskItemReadDtoList);
            })
            .Produces<IEnumerable<TaskItemReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List tasks")
            .WithDescription("Returns tasks for the column.")
            .WithName("Tasks_Get_All");


            // /projects/{projectId}/tasks
            var projectTasksGroup = app
                .MapGroup("/projects/{projectId:guid}/tasks")
                .WithTags("Tasks")
                .RequireAuthorization(Policies.ProjectReader);

            // GET /projects/{projectId}/tasks/{taskId}
            projectTasksGroup.MapGet("/{taskId:guid}", async (
                [FromRoute] Guid taskId,
                [FromServices] ITaskItemReadService taskItemReadService,
                CancellationToken ct = default) =>
            {
                var taskItemReadDto = await taskItemReadService.GetByIdAsync(taskId, ct);
                var etag = ETag.EncodeWeak(taskItemReadDto.RowVersion);

                return Results.Ok(taskItemReadDto).WithETag(etag);
            })
            .Produces<TaskItemReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get task")
            .WithDescription("Returns a task in the project. Sets ETag.")
            .WithName("Tasks_Get_ById");


            // /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks
            var projectColumnTasksGroup = app
                .MapGroup("/projects/{projectId:guid}/lanes/{laneId:guid}/columns/{columnId:guid}/tasks")
                .WithTags("Tasks")
                .RequireAuthorization(Policies.ProjectReader);

            // POST /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks
            projectColumnTasksGroup.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromBody] TaskItemCreateDto dto,
                [FromServices] ITaskItemWriteService taskItemWriteSvc,
                CancellationToken ct = default) =>
            {
                var taskItemReadDto = await taskItemWriteSvc.CreateAsync(
                    projectId,
                    laneId,
                    columnId,
                    dto,
                    ct);
                var etag = ETag.EncodeWeak(taskItemReadDto.RowVersion);

                var routeValues = new { projectId, taskId = taskItemReadDto.Id };
                return Results
                    .CreatedAtRoute("Tasks_Get_ById", routeValues, taskItemReadDto)
                    .WithETag(etag);
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

            // PATCH /projects/{projectId}/tasks/{taskId}/edit
            projectTasksGroup.MapPatch("/{taskId:guid}/edit", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid taskId,
                [FromBody] TaskItemEditDto dto,
                [FromServices] ITaskItemWriteService taskItemWriteSvc,
                CancellationToken ct = default) =>
            {
                var taskItemReadDto = await taskItemWriteSvc.EditAsync(
                    projectId,
                    taskId,
                    dto,
                    ct);
                var etag = ETag.EncodeWeak(taskItemReadDto.RowVersion);

                return Results.Ok(taskItemReadDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectMember) // ProjectMember-only
            .RequireValidation<TaskItemEditDto>()
            .RequireIfMatch() // Require If-Match to prevent overwriting concurrent edits
            .EnsureIfMatch<ITaskItemReadService, TaskItemReadDto>(routeValueKey: "taskId")
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

            // PUT /projects/{projectId}/tasks/{taskId}/move
            projectTasksGroup.MapPut("/{taskId:guid}/move", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid taskId,
                [FromBody] TaskItemMoveDto dto,
                [FromServices] ITaskItemWriteService taskItemWriteSvc,
                CancellationToken ct = default) =>
            {
                var taskItemReadDto = await taskItemWriteSvc.MoveAsync(
                    projectId,
                    taskId,
                    dto,
                    ct);
                var etag = ETag.EncodeWeak(taskItemReadDto.RowVersion);

                return Results.Ok(taskItemReadDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectMember) // ProjectMember-only
            .RequireValidation<TaskItemMoveDto>()
            .RequireIfMatch() // Require If-Match to avoid applying moves on stale state
            .EnsureIfMatch<ITaskItemReadService, TaskItemReadDto>(routeValueKey: "taskId")
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

            // DELETE /projects/{projectId}/tasks/{taskId}
            projectTasksGroup.MapDelete("/{taskId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid taskId,
                [FromServices] ITaskItemWriteService taskItemWriteSvc,
                CancellationToken ct = default) =>
            {
                await taskItemWriteSvc.DeleteByIdAsync(projectId, taskId, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Policies.ProjectMember) // ProjectMember-only
            .RequireIfMatch() // Member-only delete and requires If-Match to prevent removing a concurrently edited task
            .EnsureIfMatch<ITaskItemReadService, TaskItemReadDto>(routeValueKey: "taskId")
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

            return projectTasksGroup;
        }
    }
}
