using Api.Auth.Authorization;
using Api.Extensions;
using Api.Helpers;
using Application.Common.Abstractions.Auth;
using Application.TaskAssignments.Abstractions;
using Application.TaskAssignments.DTOs;
using Application.TaskAssignments.Mapping;
using Domain.Enums;
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
        /// Registers endpoints for task assignments under project-scoped and global routes.
        /// Enforces per-endpoint authorization and documents success/error shapes.
        /// </summary>
        /// <param name="app">Endpoint route builder.</param>
        /// <returns>The configured route group.</returns>
        public static RouteGroupBuilder MapTaskAssignments(this IEndpointRouteBuilder app)
        {
            // Group task-scoped assignment endpoints; requires ProjectReader by default
            var nested = app
                .MapGroup("/projects/{projectId:guid}/lanes/{laneId:guid}/columns/{columnId:guid}/tasks/{taskId:guid}/assignments")
                .WithTags("Task Assignments")
                .RequireAuthorization(Policies.ProjectReader);

            // OpenAPI metadata across all endpoints: ensures generated clients and API docs
            // include consistent success/error shapes, auth requirements, and concurrency responses

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments
            nested.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ITaskAssignmentReadService taskAssignmentReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskAssignments.Get_All");

                // List all assignments for a task from the read side
                var assignments = await taskAssignmentReadSvc.ListByTaskAsync(taskId, ct);
                var responseDto = assignments.Select(a => a.ToReadDto()).ToList();

                log.LogInformation(
                    "Task assignments listed projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} count={Count}",
                    projectId,
                    laneId,
                    columnId,
                    taskId,
                    responseDto.Count);
                return Results.Ok(responseDto);
            })
            .Produces<IEnumerable<TaskAssignmentReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List task assignments")
            .WithDescription("Returns all assignments for the task.")
            .WithName("TaskAssignments_Get_All");

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}
            nested.MapGet("/{userId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromRoute] Guid userId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ITaskAssignmentReadService taskAssignmentReadSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskAssignments.Get_ById");

                // Fetch a single assignment for a user in the given task. Return 404 if missing
                var assignment = await taskAssignmentReadSvc.GetAsync(taskId, userId, ct);
                if (assignment is null)
                {
                    log.LogInformation(
                        "Task assignment not found projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} userId={UserId}",
                        projectId,
                        laneId,
                        columnId,
                        taskId,
                        userId);
                    return Results.NotFound();
                }

                // Attach weak ETag from RowVersion so clients can perform conditional operations later
                var responseDto = assignment.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Task assignment fetched projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} userId={UserId} etag={ETag}",
                    projectId,
                    laneId,
                    columnId,
                    taskId,
                    userId,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
            })
            .Produces<TaskAssignmentReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get task assignment")
            .WithDescription("Returns a user’s assignment for the task. Sets ETag.")
            .WithName("TaskAssignments_Get_ById");

            // POST /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments
            nested.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromBody] TaskAssignmentCreateDto dto,
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] ITaskAssignmentReadService taskAssignmentReadSvc,
                [FromServices] ITaskAssignmentWriteService taskAssignmentWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskAssignments.Create");

                // Create or upgrade an assignment. The command includes the executor for audit purposes
                // Service returns Conflict for illegal transitions; map DomainMutation to HTTP
                var executedBy = (Guid)currentUserSvc.UserId!;
                var (result, assignment) = await taskAssignmentWriteSvc.CreateAsync(
                    projectId,
                    taskId,
                    targetUserId: dto.UserId,
                    dto.Role,
                    executedBy,
                    ct);
                if (result == DomainMutation.Conflict || assignment is null)
                {
                    log.LogInformation(
                        "Task assignment create conflict projectId={ProjectId} taskId={TaskId} userId={UserId}",
                        projectId,
                        taskId,
                        dto.UserId);
                    return result.ToHttp(context);
                }

                // Read back canonical state and return 201 on create, 200 on update
                // Always attach a fresh ETag so clients can proceed with If-Match semantics
                var created = await taskAssignmentReadSvc.GetAsync(taskId, dto.UserId, ct);
                if (created is null)
                {
                    log.LogInformation(
                        "Task assignment create readback missing projectId={ProjectId} taskId={TaskId} userId={UserId}",
                        projectId,
                        taskId,
                        dto.UserId);
                    return Results.NotFound();
                }

                var responseDto = created.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                if (result != DomainMutation.Created)
                {
                    log.LogInformation(
                        "Task assignment updated projectId={ProjectId} taskId={TaskId} userId={UserId} role={Role} etag={ETag}",
                        projectId,
                        taskId,
                        dto.UserId,
                        created.Role,
                        etag);
                    return Results.Ok(responseDto).WithETag(etag);  // upsert-style update
                }

                log.LogInformation(
                    "Task assignment created projectId={ProjectId} taskId={TaskId} userId={UserId} role={Role} etag={ETag}",
                    projectId,
                    taskId,
                    dto.UserId,
                    created.Role,
                    etag);

                var routeValues = new { projectId, laneId, columnId, taskId, userId = dto.UserId };
                return Results.CreatedAtRoute("TaskAssignments_Get_ById", routeValues, responseDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin)  // ProjectAdmin-only
            .RequireValidation<TaskAssignmentCreateDto>()
            .RejectIfMatch() // Reject If-Match on create-or-update entry point; the write model defines idempotency
            .Produces<TaskAssignmentReadDto>(StatusCodes.Status201Created)
            .Produces<TaskAssignmentReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Create or update assignment")
            .WithDescription("Admin-only. Creates or updates a task assignment. Returns the resource with ETag (201 if created, 200 if updated).")
            .WithName("Assignments_Create");

            // PATCH /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}/role
            nested.MapPatch("/{userId:guid}/role", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromRoute] Guid userId,
                [FromBody] TaskAssignmentChangeRoleDto dto,
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] ITaskAssignmentReadService taskAssignmentReadSvc,
                [FromServices] ITaskAssignmentWriteService taskAssignmentWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskAssignments.ChangeRole");

                // Resolve current RowVersion from If-Match or storage fallback to guard against lost updates
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => taskAssignmentReadSvc.GetAsync(taskId, userId, ct),
                    a => a.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation(
                        "Task assignment not found when resolving row version projectId={ProjectId} taskId={TaskId} userId={UserId}",
                        projectId,
                        taskId,
                        userId);
                    return Results.NotFound();
                }

                // Change role under optimistic concurrency. 412 on stale, 404 if not found
                var executedBy = (Guid)currentUserSvc.UserId!;
                var result = await taskAssignmentWriteSvc.ChangeRoleAsync(
                    projectId,
                    taskId,
                    targetUserId: userId,
                    dto.NewRole,
                    executedBy,
                    rowVersion,
                    ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation(
                        "Task assignment role change rejected projectId={ProjectId} taskId={TaskId} userId={UserId} mutation={Mutation}",
                        projectId,
                        taskId,
                        userId,
                        result);
                    return result.ToHttp(context);
                }

                // Return updated representation with refreshed ETag
                var updated = await taskAssignmentReadSvc.GetAsync(taskId, userId, ct);
                if (updated is null)
                {
                    log.LogInformation(
                        "Task assignment role change readback missing projectId={ProjectId} taskId={TaskId} userId={UserId}",
                        projectId,
                        taskId,
                        userId);
                    return Results.NotFound();
                }

                var responseDto = updated.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Task assignment role changed projectId={ProjectId} taskId={TaskId} userId={UserId} newRole={NewRole} etag={ETag}",
                    projectId,
                    taskId,
                    userId,
                    dto.NewRole,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireValidation<TaskAssignmentChangeRoleDto>()
            .RequireIfMatch() // Require If-Match to prevent overwriting concurrent role changes
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

            // DELETE /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}
            nested.MapDelete("/{userId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromRoute] Guid userId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] ITaskAssignmentReadService taskAssignmentReadSvc,
                [FromServices] ITaskAssignmentWriteService taskAssignmentWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskAssignments.Remove");

                // Conditional delete of an assignment. Includes executor for audit logging
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => taskAssignmentReadSvc.GetAsync(taskId, userId, ct),
                    a => a.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation(
                        "Task assignment not found when resolving row version projectId={ProjectId} taskId={TaskId} userId={UserId}",
                        projectId,
                        taskId,
                        userId);
                    return Results.NotFound();
                }

                var executedBy = (Guid)currentUserSvc.UserId!;
                var result = await taskAssignmentWriteSvc.DeleteAsync(
                    projectId,
                    taskId,
                    targetUserId: userId,
                    executedBy,
                    rowVersion,
                    ct);

                log.LogInformation(
                    "Task assignment removed projectId={ProjectId} taskId={TaskId} userId={UserId} mutation={Mutation}",
                    projectId,
                    taskId,
                    userId,
                    result);

                // Map DomainMutation to HTTP (204 on success, 409/412 on conflicts)
                return result.ToHttp(context);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireIfMatch() // Requires If-Match to avoid deleting over stale state
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Remove assignment")
            .WithDescription("Admin-only. Removes a task assignment using optimistic concurrency (If-Match).")
            .WithName("Assignments_Remove");


            // Global utilities for assignment queries by current user or admin context
            var top = app.MapGroup("/assignments")
                .WithTags("Task Assignments")
                .RequireAuthorization();

            // GET /assignments/me
            top.MapGet("/me", async (
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserSvc,
                [FromServices] ITaskAssignmentReadService taskAssignmentReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskAssignments.Get_Mine");

                // Lists all assignments for the current user across accessible projects
                var userId = (Guid)currentUserSvc.UserId!;
                var assignments = await taskAssignmentReadSvc.ListByUserAsync(userId, ct);
                var responseDto = assignments.Select(a => a.ToReadDto()).ToList();

                log.LogInformation(
                    "Task assignments listed for current user userId={UserId} count={Count}",
                    userId,
                    responseDto.Count);
                return Results.Ok(responseDto);
            })
            .Produces<IEnumerable<TaskAssignmentReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("List my assignments")
            .WithDescription("Returns task assignments of the authenticated user across accessible projects.")
            .WithName("TaskAssignments_Get_Mine");

            // GET /assignments/users/{userId}
            top.MapGet("/users/{userId:guid}", async (
                [FromRoute] Guid userId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ITaskAssignmentReadService taskAssignmentReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskAssignments.Get_ByUser");

                // Admin-only listing of assignments for a specific user across projects
                var assignments = await taskAssignmentReadSvc.ListByUserAsync(userId, ct);
                var responseDto = assignments.Select(a => a.ToReadDto()).ToList();

                log.LogInformation(
                    "Task assignments listed for userId={UserId} count={Count}",
                    userId,
                    responseDto.Count);
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.SystemAdmin) // SystemAdmin-only
            .Produces<IEnumerable<TaskAssignmentReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List assignments by user")
            .WithDescription("Admin-only. Returns task assignments of the specified user across accessible projects.")
            .WithName("TaskAssignments_Get_ByUser");

            return top;
        }
    }
}
