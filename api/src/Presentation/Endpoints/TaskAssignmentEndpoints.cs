using Api.Auth.Authorization;
using Api.Extensions;
using Api.Filters;
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
            var group = app.MapGroup("/projects/{projectId:guid}/lanes/{laneId:guid}/columns/{columnId:guid}/tasks/{taskId:guid}/assignments")
                .WithTags("Task Assignments")
                .RequireAuthorization(Policies.ProjectReader);

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments
            group.MapGet("/", async (
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

            // POST /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/assignments
            group.MapPost("/", async (
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
            group.MapPatch("/{userId:guid}/role", async (
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
                if (m != DomainMutation.Updated) return m.ToHttp();

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
            group.MapDelete("/{userId:guid}", async (
                [FromRoute] Guid taskId,
                [FromRoute] Guid userId,
                [FromServices] ITaskAssignmentWriteService svc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = (byte[])http.Items["rowVersion"]!;
                var m = await svc.RemoveAsync(taskId, userId, rowVersion, ct);
                return m.ToHttp();
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

            return group;
        }
    }
}
