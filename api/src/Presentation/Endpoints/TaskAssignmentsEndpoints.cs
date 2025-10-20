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
    public static class TaskAssignmentsEndpoints
    {
        public static RouteGroupBuilder MapTaskAssignments(this IEndpointRouteBuilder app)
        {
            var nested = app
                        .MapGroup("/projects/{projectId:guid}/lanes/{laneId:guid}/columns/{columnId:guid}/tasks/{taskId:guid}/assignments")
                        .WithTags("Task Assignments")
                        .RequireAuthorization(Policies.ProjectReader);

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

                var assignments = await taskAssignmentReadSvc.ListByTaskAsync(taskId, ct);
                var responseDto = assignments.Select(a => a.ToReadDto()).ToList();

                log.LogInformation("Task assignments listed projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} count={Count}",
                                    projectId, laneId, columnId, taskId, responseDto.Count);
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

                var assignment = await taskAssignmentReadSvc.GetAsync(taskId, userId, ct);
                if (assignment is null)
                {
                    log.LogInformation("Task assignment not found projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} userId={UserId}",
                                        projectId, laneId, columnId, taskId, userId);
                    return Results.NotFound();
                }

                var responseDto = assignment.ToReadDto();
                context.Response.SetETag(responseDto.RowVersion);

                log.LogInformation("Task assignment fetched projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} userId={UserId} etag={ETag}",
                                    projectId, laneId, columnId, taskId, userId, context.Response.Headers.ETag.ToString());
                return Results.Ok(responseDto);
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

                var performedById = (Guid)currentUserSvc.UserId!;

                var (result, assignment) = await taskAssignmentWriteSvc.CreateAsync(projectId, taskId, dto.UserId, dto.Role, performedById, ct);
                if (result == DomainMutation.Conflict || assignment is null)
                {
                    log.LogInformation("Task assignment create conflict projectId={ProjectId} taskId={TaskId} userId={UserId}",
                                        projectId, taskId, dto.UserId);
                    return result.ToHttp(context);
                }

                var created = await taskAssignmentReadSvc.GetAsync(taskId, dto.UserId, ct);
                if (created is null)
                {
                    log.LogInformation("Task assignment create readback missing projectId={ProjectId} taskId={TaskId} userId={UserId}",
                                        projectId, taskId, dto.UserId);
                    return Results.NotFound();
                }

                var responseDto = created.ToReadDto();
                context.Response.SetETag(responseDto.RowVersion);

                if (result != DomainMutation.Created)
                {
                    log.LogInformation("Task assignment updated projectId={ProjectId} taskId={TaskId} userId={UserId} role={Role} etag={ETag}",
                                        projectId, taskId, dto.UserId, created.Role, context.Response.Headers.ETag.ToString());
                    return Results.Ok(responseDto);
                }

                log.LogInformation("Task assignment created projectId={ProjectId} taskId={TaskId} userId={UserId} role={Role} etag={ETag}",
                                    projectId, taskId, dto.UserId, created.Role, context.Response.Headers.ETag.ToString());
                return Results.CreatedAtRoute("TaskAssignments_Get_ById", new { projectId, laneId, columnId, taskId, userId = dto.UserId }, responseDto);
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireValidation<TaskAssignmentCreateDto>()
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

                var performedById = (Guid)currentUserSvc.UserId!;
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => taskAssignmentReadSvc.GetAsync(taskId, userId, ct), a => a.RowVersion);

                var result = await taskAssignmentWriteSvc.ChangeRoleAsync(projectId, taskId, userId, dto.NewRole, performedById, rowVersion, ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation("Task assignment role change rejected projectId={ProjectId} taskId={TaskId} userId={UserId} mutation={Mutation}",
                                        projectId, taskId, userId, result);
                    return result.ToHttp(context);
                }

                var updated = await taskAssignmentReadSvc.GetAsync(taskId, userId, ct);
                if (updated is null)
                {
                    log.LogInformation("Task assignment role change readback missing projectId={ProjectId} taskId={TaskId} userId={UserId}",
                                        projectId, taskId, userId);
                    return Results.NotFound();
                }

                var responseDto = updated.ToReadDto();
                context.Response.SetETag(responseDto.RowVersion);

                log.LogInformation("Task assignment role changed projectId={ProjectId} taskId={TaskId} userId={UserId} newRole={NewRole} etag={ETag}",
                                    projectId, taskId, userId, dto.NewRole, context.Response.Headers.ETag.ToString());
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireValidation<TaskAssignmentChangeRoleDto>()
            .RequireIfMatch()
            .Produces<TaskAssignmentReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
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

                var performedById = (Guid)currentUserSvc.UserId!;
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => taskAssignmentReadSvc.GetAsync(taskId, userId, ct), a => a.RowVersion);

                var result = await taskAssignmentWriteSvc.RemoveAsync(projectId, taskId, userId, performedById, rowVersion, ct);

                log.LogInformation("Task assignment removed projectId={ProjectId} taskId={TaskId} userId={UserId} mutation={Mutation}",
                                    projectId, taskId, userId, result);
                return result.ToHttp(context);
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireIfMatch()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Remove assignment")
            .WithDescription("Admin-only. Removes a task assignment using optimistic concurrency (If-Match).")
            .WithName("Assignments_Remove");

            // top-level lists
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

                var userId = (Guid)currentUserSvc.UserId!;
                var assignments = await taskAssignmentReadSvc.ListByUserAsync(userId, ct);
                var responseDto = assignments.Select(a => a.ToReadDto()).ToList();

                log.LogInformation("Task assignments listed for current user userId={UserId} count={Count}",
                                    userId, responseDto.Count);
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

                var assignments = await taskAssignmentReadSvc.ListByUserAsync(userId, ct);
                var responseDto = assignments.Select(a => a.ToReadDto()).ToList();

                log.LogInformation("Task assignments listed for userId={UserId} count={Count}",
                                    userId, responseDto.Count);
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.SystemAdmin)
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
