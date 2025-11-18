using Api.Auth.Authorization;
using Application.TaskActivities.Abstractions;
using Application.TaskActivities.DTOs;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    /// <summary>
    /// Task activity endpoints: list by task, fetch single activity,
    /// and query by user. Read-only API for audit trails and history views.
    /// </summary>
    public static class TaskActivitiesEndpoints
    {
        /// <summary>
        /// Registers endpoints for task activity queries under:
        /// - /tasks/{taskId}/activities (task-scoped collection),
        /// - /activities/{activityId} (single activity),
        /// - /activities (global activity queries).
        /// Uses read-side services only. Requires ProjectReader or higher access
        /// for project-scoped queries and authenticated access for global queries.
        /// </summary>
        /// <param name="app">Endpoint route builder.</param>
        /// <returns>The configured route group for global activity endpoints.</returns>
        public static RouteGroupBuilder MapTaskActivities(this IEndpointRouteBuilder app)
        {
            // projects/{projectId}/tasks/{taskId}/activities
            var taskActivitiesGroup = app
                .MapGroup("projects/{projectId:guid}/tasks/{taskId:guid}/activities")
                .WithTags("Task Activities")
                .RequireAuthorization(Policies.ProjectReader);

            // OpenAPI metadata across all endpoints: ensures generated clients and API docs
            // include consistent success/error shapes, auth requirements, and read-only behavior

            // GET projects/{projectId:guid}/tasks/{taskId}/activities
            taskActivitiesGroup.MapGet("/", async (
                [FromRoute] Guid taskId,
                [FromQuery] TaskActivityType? activityType,
                [FromServices] ITaskActivityReadService taskActivityReadSvc,
                CancellationToken ct = default) =>
            {
                var taskActivityReadDtoList = activityType is null
                    ? await taskActivityReadSvc.ListByTaskIdAsync(taskId, ct)
                    : await taskActivityReadSvc.ListByActivityTypeAsync(taskId, activityType.Value, ct);

                return Results.Ok(taskActivityReadDtoList);
            })
            .Produces<IEnumerable<TaskActivityReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List task activities")
            .WithDescription("Returns activities for the task. Optional filter by activity type.")
            .WithName("TaskActivities_Get_All");

            // projects/{projectId}/activities/{activityId}
            var activityGroup = app
                .MapGroup("projects/{projectId:guid}/activities/{activityId:guid}")
                .WithTags("Task Activities")
                .RequireAuthorization(Policies.ProjectReader);

            // GET projects/{projectId:guid}/activities/{activityId}
            activityGroup.MapGet("/", async (
                [FromRoute] Guid activityId,
                [FromServices] ITaskActivityReadService taskActivityReadSvc,
                CancellationToken ct = default) =>
            {
                var taskActivityReadDto = await taskActivityReadSvc.GetByIdAsync(activityId, ct);
                return Results.Ok(taskActivityReadDto);
            })
            .Produces<TaskActivityReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get task activity")
            .WithDescription("Returns a single activity if it belongs to a project the user can read.")
            .WithName("TaskActivities_Get_ById");

            // Global access group for querying task activities by authenticated user or admin context
            var activitiesGroup = app.MapGroup("/activities")
                .WithTags("Task Activities")
                .RequireAuthorization();

            // GET /activities/me
            activitiesGroup.MapGet("/me", async (
                [FromServices] ITaskActivityReadService taskActivityReadSvc,
                CancellationToken ct = default) =>
            {
                var taskActivityReadDtoList = await taskActivityReadSvc.ListSelfAsync(ct);
                return Results.Ok(taskActivityReadDtoList);
            })
            .Produces<IEnumerable<TaskActivityReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("List my activities")
            .WithDescription("Returns activities performed by the authenticated user.")
            .WithName("TaskActivities_Get_Mine");

            // GET /activities/users/{userId}
            activitiesGroup.MapGet("/users/{userId:guid}", async (
                [FromRoute] Guid userId,
                [FromServices] ITaskActivityReadService taskActivityReadSvc,
                CancellationToken ct = default) =>
            {
                var taskActivityReadDtoList = await taskActivityReadSvc.ListByUserIdAsync(userId, ct);
                return Results.Ok(taskActivityReadDtoList);
            })
            .RequireAuthorization(Policies.SystemAdmin) // SystemAdmin-only
            .Produces<IEnumerable<TaskActivityReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List activities by user")
            .WithDescription("Admin-only. Returns activities performed by the specified user.")
            .WithName("TaskActivities_Get_ByUser");

            return activitiesGroup;
        }
    }
}
