using Api.Auth.Authorization;
using Api.Extensions;
using Api.Filters;
using Application.Common.Abstractions.Auth;
using Application.TaskAssignments.Abstractions;
using Application.TaskAssignments.DTOs;
using Application.TaskAssignments.Mapping;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    public static class TaskAssignmentEndpoints
    {
        public static RouteGroupBuilder MapTaskAssignments(this IEndpointRouteBuilder app)
        {
            var nested = app.MapGroup("/projects/{projectId:guid}/lanes/{laneId:guid}/columns/{columnId:guid}/tasks/{taskId:guid}/assignments")
                .WithTags("Task Assignments")
                .RequireAuthorization(Policies.ProjectReader);

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments
            nested.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromServices] ITaskAssignmentReadService taskAssignmentReadSvc,
                CancellationToken ct = default) =>
            {
                var items = await taskAssignmentReadSvc.ListByTaskAsync(taskId, ct);
                var dto = items.Select(a => a.ToReadDto()).ToList();
                return Results.Ok(dto);
            })
            .Produces<IEnumerable<TaskAssignmentReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get task assignments")
            .WithDescription("Returns all assignments for the specified task.")
            .WithName("TaskAssignments_Get_All");

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}
            nested.MapGet("/{userId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromRoute] Guid userId,
                [FromServices] ITaskAssignmentReadService assignmentReadSvc,
                CancellationToken ct = default) =>
            {
                var a = await assignmentReadSvc.GetAsync(taskId, userId, ct);
                return a is null ? Results.NotFound() : Results.Ok(a.ToReadDto());
            })
            .Produces<TaskAssignmentReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get task assignment")
            .WithDescription("Returns the assignment of a given user on a task.")
            .WithName("TaskAssignments_Get");

            // POST /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments
            nested.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromBody] TaskAssignmentCreateDto dto,
                [FromServices] ITaskAssignmentWriteService taskAssignmentWriteSvc,
                [FromServices] ITaskAssignmentReadService taskAssignmentReadSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var (m, _) = await taskAssignmentWriteSvc.CreateAsync(taskId, dto.UserId, dto.Role, ct); // may return Created/Updated/NoOp/Conflict
                if (m == DomainMutation.Conflict) return m.ToHttp();

                var assigned = await taskAssignmentReadSvc.GetAsync(taskId, dto.UserId, ct);
                if (assigned is null) return Results.NotFound();

                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(assigned!.RowVersion)}\"";
                var body = assigned.ToReadDto();

                return m == DomainMutation.Created
                    ? Results.Created($"/projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{dto.UserId}", body)
                    : Results.Ok(body);
            })
            .RequireAuthorization(Policies.ProjectMember)
            .Produces<TaskAssignmentReadDto>(StatusCodes.Status201Created)
            .Produces<TaskAssignmentReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Assign user to task")
            .WithDescription("Creates or updates a task assignment for a user and role.")
            .WithName("Assignments_Create");

            // PATCH /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}/role
            nested.MapPatch("/{userId:guid}/role", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromRoute] Guid userId,
                [FromBody] TaskAssignmentChangeRoleDto dto,
                [FromServices] ITaskAssignmentWriteService taskAssignmentWriteSvc,
                [FromServices] ITaskAssignmentReadService taskAssignmentReadSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = (byte[])http.Items["rowVersion"]!;
                var m = await taskAssignmentWriteSvc.ChangeRoleAsync(taskId, userId, dto.NewRole, rowVersion, ct);
                if (m != DomainMutation.Updated) return m.ToHttp(http);

                var updated = await taskAssignmentReadSvc.GetAsync(taskId, userId, ct);
                if (updated is null) return Results.NotFound();

                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(updated.RowVersion)}\"";
                return Results.Ok(updated.ToReadDto());
            })
            .AddEndpointFilter<IfMatchRowVersionFilter>()
            .RequireAuthorization(Policies.ProjectMember)
            .Produces<TaskAssignmentReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Change assignment role")
            .WithDescription("Changes the role of a task assignment using optimistic concurrency (If-Match).")
            .WithName("Assignments_Change_Role");

            // DELETE /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments/{userId}
            nested.MapDelete("/{userId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromRoute] Guid userId,
                [FromServices] ITaskAssignmentWriteService svc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = (byte[])http.Items["rowVersion"]!;
                var m = await svc.RemoveAsync(taskId, userId, rowVersion, ct);
                return m.ToHttp(http);
            })
            .AddEndpointFilter<IfMatchRowVersionFilter>()
            .RequireAuthorization(Policies.ProjectMember)
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Remove assignment")
            .WithDescription("Removes a task assignment using optimistic concurrency (If-Match).")
            .WithName("Assignments_Remove");

            // top-level lists
            var top = app.MapGroup("/assignments")
                .WithTags("Task Assignments")
                .RequireAuthorization();

            // GET /assignments/me
            top.MapGet("/me", async (
                HttpContext http,
                [FromServices] ITaskAssignmentReadService assignmentReadSvc,
                [FromServices] ICurrentUserService currentUserSvc,
                CancellationToken ct = default) =>
            {
                var userId = (Guid)currentUserSvc.UserId!;
                var items = await assignmentReadSvc.ListByUserAsync(userId, ct);
                var dto = items.Select(a => a.ToReadDto()).ToList();
                return Results.Ok(dto);
            })
            .Produces<IEnumerable<TaskAssignmentReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("List my task assignments")
            .WithDescription("Lists task assignments of the authenticated user across accessible projects.")
            .WithName("TaskAssignments_ListMine");

            // GET /assignments/users/{userId}
            top.MapGet("/users/{userId:guid}", async (
                [FromRoute] Guid userId,
                [FromServices] ITaskAssignmentReadService assignmentReadSvc,
                CancellationToken ct = default) =>
            {
                var items = await assignmentReadSvc.ListByUserAsync(userId, ct);
                var dto = items.Select(a => a.ToReadDto()).ToList();
                return Results.Ok(dto);
            })
            .Produces<IEnumerable<TaskAssignmentReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List task assignments by user")
            .WithDescription("Lists task assignments of the specified user across accessible projects.")
            .WithName("TaskAssignments_ListByUser");

            return top;
        }
    }
}
