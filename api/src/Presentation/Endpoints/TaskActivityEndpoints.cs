using Api.Auth.Authorization;
using Application.Common.Abstractions.Auth;
using Application.TaskActivities.Abstractions;
using Application.TaskActivities.DTOs;
using Application.TaskActivities.Mapping;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    public static class TaskActivityEndpoints
    {
        public static RouteGroupBuilder MapTaskActivities(this IEndpointRouteBuilder app)
        {
            var nested = app.MapGroup("/projects/{projectId:guid}/lanes/{laneId:guid}/columns/{columnId:guid}/tasks/{taskId:guid}/activities")
                .WithTags("Task Activities")
                .RequireAuthorization(Policies.ProjectReader);

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/activities
            nested.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromQuery] TaskActivityType? type,
                [FromServices] ITaskActivityReadService readSvc,
                CancellationToken ct = default) =>
            {
                var items = type is null
                    ? await readSvc.ListByTaskAsync(taskId, ct)
                    : await readSvc.ListByTypeAsync(taskId, type.Value, ct);

                var dto = items.Select(a => a.ToReadDto()).ToList();
                return Results.Ok(dto);
            })
            .Produces<IEnumerable<TaskActivityReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get task activities")
            .WithDescription("Returns all activities for the task. Optional filter by type.")
            .WithName("TaskActivities_Get_All");

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/activities/{activityId}
            nested.MapGet("/{activityId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromRoute] Guid activityId,
                [FromServices] ITaskActivityReadService readSvc,
                CancellationToken ct = default) =>
            {
                var a = await readSvc.GetAsync(activityId, ct);
                if (a is null || a.TaskId != taskId) return Results.NotFound();

                return Results.Ok(a.ToReadDto());
            })
            .Produces<TaskActivityReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get activity by id")
            .WithDescription("Returns a single activity if it belongs to the specified task.")
            .WithName("TaskActivities_Get_ById");

            
            var top = app.MapGroup("/activities")
                .WithTags("Task Activities")
                .RequireAuthorization();

            // GET /activities/me
            top.MapGet("/me", async (
                HttpContext http,
                [FromServices] ITaskActivityReadService activityReadSvc,
                [FromServices] ICurrentUserService currentUserService,
                CancellationToken ct = default) =>
            {
                var userId = (Guid)currentUserService.UserId!;
                var items = await activityReadSvc.ListByActorAsync(userId, ct);
                var dto = items.Select(a => a.ToReadDto()).ToList();
                return Results.Ok(dto);
            })
            .Produces<IEnumerable<TaskActivityReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("List my activities")
            .WithDescription("Lists task activities performed by the authenticated user.")
            .WithName("TaskActivities_ListMine");

            return top;
        }
    }
}
