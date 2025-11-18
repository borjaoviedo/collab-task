using Api.Auth.Authorization;
using Api.Concurrency;
using Api.Filters;
using Application.TaskAssignments.Abstractions;
using Application.TaskAssignments.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    /// <summary>
    /// Task assignment endpoints: list, read, create-or-update, change role, and remove.
    /// Uses Clean Architecture services and optimistic concurrency via ETag/If-Match.
    /// </summary>
    public static class TaskAssignmentsEndpoints
    {
        /// <summary>
        /// Registers endpoints for task assignments under:
        /// - /projects/{projectId}/tasks/{taskId}/assignments (task-scoped management),
        /// - /assignments (global assignment queries).
        /// Enforces per-endpoint authorization and documents success/error shapes.
        /// </summary>
        /// <param name="app">Endpoint route builder.</param>
        /// <returns>The configured route group.</returns>
        public static RouteGroupBuilder MapTaskAssignments(this IEndpointRouteBuilder app)
        {
            // OpenAPI metadata across all endpoints: ensures generated clients and API docs
            // include consistent success/error shapes and auth requirements


            // Group task-scoped assignment endpoints; requires ProjectReader by default
            var taskAssignmentsGroup = app
                .MapGroup("/projects/{projectId:guid}/tasks/{taskId:guid}/assignments")
                .WithTags("Task Assignments")
                .RequireAuthorization(Policies.ProjectReader);


            // ===================================================================================
            // GET /projects/{projectId}/tasks/{taskId}/assignments
            // ===================================================================================
            taskAssignmentsGroup.MapGet("/", async (
                [FromRoute] Guid taskId,
                [FromServices] ITaskAssignmentReadService taskAssignmentReadSvc,
                CancellationToken ct = default) =>
            {
                var taskAssignmentReadDtoList = await taskAssignmentReadSvc.ListByTaskIdAsync(taskId, ct);
                return Results.Ok(taskAssignmentReadDtoList);
            })
            .Produces<IEnumerable<TaskAssignmentReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("List task assignments")
            .WithDescription("Returns all assignments for the task.")
            .WithName("TaskAssignments_Get_All");

            // ===================================================================================
            // GET /projects/{projectId}/tasks/{taskId}/assignments/{userId}
            // ===================================================================================
            taskAssignmentsGroup.MapGet("/{userId:guid}", async (
                [FromRoute] Guid taskId,
                [FromRoute] Guid userId,
                [FromServices] ITaskAssignmentReadService taskAssignmentReadSvc,
                CancellationToken ct = default) =>
            {
                var taskAssignmentReadDto = await taskAssignmentReadSvc.GetByTaskAndUserIdAsync(taskId, userId, ct);
                var etag = ETag.EncodeWeak(taskAssignmentReadDto.RowVersion);

                return Results.Ok(taskAssignmentReadDto).WithETag(etag);
            })
            .Produces<TaskAssignmentReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get task assignment")
            .WithDescription("Returns a user’s assignment for the task. Sets ETag.")
            .WithName("TaskAssignments_Get_ById");

            // ===================================================================================
            // POST /projects/{projectId}/tasks/{taskId}/assignments
            // ===================================================================================
            taskAssignmentsGroup.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid taskId,
                [FromBody] TaskAssignmentCreateDto dto,
                [FromServices] ITaskAssignmentWriteService taskAssignmentWriteSvc,
                CancellationToken ct = default) =>
            {
                var taskAssignmentReadDto = await taskAssignmentWriteSvc.CreateAsync(
                    projectId,
                    taskId,
                    dto,
                    ct);
                var etag = ETag.EncodeWeak(taskAssignmentReadDto.RowVersion);

                var routeValues = new { projectId, taskId, userId = dto.UserId };
                return Results
                    .CreatedAtRoute("TaskAssignments_Get_ById", routeValues, taskAssignmentReadDto)
                    .WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin)  // ProjectAdmin-only
            .RequireValidation<TaskAssignmentCreateDto>()
            .RejectIfMatch() // Reject If-Match on create-or-update entry point; the write model defines idempotency
            .Produces<TaskAssignmentReadDto>(StatusCodes.Status201Created)
            .Produces<TaskAssignmentReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Create or update assignment")
            .WithDescription("Admin-only. Creates or updates a task assignment. Returns the resource with ETag (201 if created, 200 if updated).")
            .WithName("Assignments_Create");

            // ===================================================================================
            // PATCH /projects/{projectId}/tasks/{taskId}/assignments/{userId}/role
            // ===================================================================================
            taskAssignmentsGroup.MapPatch("/{userId:guid}/role", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid taskId,
                [FromRoute] Guid userId,
                [FromBody] TaskAssignmentChangeRoleDto dto,
                [FromServices] ITaskAssignmentWriteService taskAssignmentWriteSvc,
                CancellationToken ct = default) =>
            {
                var taskAssignmentReadDto = await taskAssignmentWriteSvc.ChangeRoleAsync(
                    projectId,
                    taskId,
                    userId,
                    dto,
                    ct);
                var etag = ETag.EncodeWeak(taskAssignmentReadDto.RowVersion);

                return Results.Ok(taskAssignmentReadDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireValidation<TaskAssignmentChangeRoleDto>()
            .RequireIfMatch() // Require If-Match to prevent overwriting concurrent role changes
            .EnsureIfMatch<ITaskAssignmentReadService, TaskAssignmentReadDto>(routeValueKey: "userId")
            .Produces<TaskAssignmentReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Change assignment role")
            .WithDescription("Admin-only. Changes the role using optimistic concurrency (If-Match). Returns the updated resource and ETag.")
            .WithName("Assignments_ChangeRole");

            // ===================================================================================
            // DELETE /projects/{projectId}/tasks/{taskId}/assignments/{userId}
            // ===================================================================================
            taskAssignmentsGroup.MapDelete("/{userId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid taskId,
                [FromRoute] Guid userId,
                [FromServices] ITaskAssignmentWriteService taskAssignmentWriteSvc,
                CancellationToken ct = default) =>
            {
                await taskAssignmentWriteSvc.DeleteAsync(projectId, taskId, userId, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireIfMatch() // Requires If-Match to avoid deleting over stale state
            .EnsureIfMatch<ITaskAssignmentReadService, TaskAssignmentReadDto>(routeValueKey: "userId")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Remove assignment")
            .WithDescription("Admin-only. Removes a task assignment using optimistic concurrency (If-Match).")
            .WithName("Assignments_Remove");


            // Global utilities for assignment queries by current user or admin context
            var assignmentsGroup = app.MapGroup("/assignments")
                .WithTags("Task Assignments")
                .RequireAuthorization();


            // ===================================================================================
            // GET /assignments/me
            // ===================================================================================
            assignmentsGroup.MapGet("/me", async (
                [FromServices] ITaskAssignmentReadService taskAssignmentReadSvc,
                CancellationToken ct = default) =>
            {
                var taskAssignmentReadDtoList = await taskAssignmentReadSvc.ListSelfAsync(ct);
                return Results.Ok(taskAssignmentReadDtoList);
            })
            .Produces<IEnumerable<TaskAssignmentReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("List my assignments")
            .WithDescription("Returns task assignments of the authenticated user across accessible projects.")
            .WithName("TaskAssignments_Get_Mine");

            // ===================================================================================
            // GET /assignments/users/{userId}
            // ===================================================================================
            assignmentsGroup.MapGet("/users/{userId:guid}", async (
                [FromRoute] Guid userId,
                [FromServices] ITaskAssignmentReadService taskAssignmentReadSvc,
                CancellationToken ct = default) =>
            {
                var taskAssignmentReadDtoList = await taskAssignmentReadSvc.ListByUserIdAsync(userId, ct);
                return Results.Ok(taskAssignmentReadDtoList);
            })
            .RequireAuthorization(Policies.SystemAdmin) // SystemAdmin-only
            .Produces<IEnumerable<TaskAssignmentReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("List assignments by user")
            .WithDescription("Admin-only. Returns task assignments of the specified user across accessible projects.")
            .WithName("TaskAssignments_Get_ByUser");

            return assignmentsGroup;
        }
    }
}
