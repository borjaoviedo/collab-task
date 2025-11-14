using Api.Auth.Authorization;
using Application.Abstractions.Auth;
using Application.TaskActivities.Abstractions;
using Application.TaskActivities.DTOs;
using Application.TaskActivities.Mapping;
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
        /// Registers endpoints for task activity queries under both project-scoped and global routes.
        /// Uses read-side services only. Requires ProjectReader or higher access.
        /// </summary>
        /// <param name="app">Endpoint route builder.</param>
        /// <returns>The configured route group.</returns>
        public static RouteGroupBuilder MapTaskActivities(this IEndpointRouteBuilder app)
        {
            // Group task-scoped activity endpoints; ProjectReader required for visibility
            var nested = app
                .MapGroup("/projects/{projectId:guid}/lanes/{laneId:guid}/columns/{columnId:guid}/tasks/{taskId:guid}/activities")
                .WithTags("Task Activities")
                .RequireAuthorization(Policies.ProjectReader);

            // OpenAPI metadata across all endpoints: ensures generated clients and API docs
            // include consistent success/error shapes, auth requirements, and read-only behavior

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/activities
            nested.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromServices] ILoggerFactory logger,
                [FromQuery] TaskActivityType? activityType,
                [FromServices] ITaskActivityReadService taskActivityReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskActivities.Get_All");

                // Lists all recorded activities for a specific task
                // Optional query parameter filters by activity type for client-side grouping
                var activities = activityType is null
                    ? await taskActivityReadSvc.ListByTaskAsync(taskId, ct)
                    : await taskActivityReadSvc.ListByTypeAsync(taskId, activityType.Value, ct);

                var responseDto = activities.Select(a => a.ToReadDto()).ToList();

                log.LogInformation(
                    "Task activities listed projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} activityType={ActivityType} count={Count}",
                    projectId,
                    laneId,
                    columnId,
                    taskId,
                    activityType,
                    responseDto.Count);
                return Results.Ok(responseDto);
            })
            .Produces<IEnumerable<TaskActivityReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List task activities")
            .WithDescription("Returns activities for the task. Optional filter by activity type.")
            .WithName("TaskActivities_Get_All");

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}/tasks/{taskId}/activities/{activityId}
            nested.MapGet("/{activityId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromRoute] Guid taskId,
                [FromRoute] Guid activityId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ITaskActivityReadService taskActivityReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskActivities.Get_ById");

                // Fetches one activity entry by id. Returns 404 if not found in the specified task scope
                var activity = await taskActivityReadSvc.GetAsync(activityId, ct);
                if (activity is null)
                {
                    log.LogInformation(
                        "Task activity not found projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} activityId={ActivityId}",
                        projectId,
                        laneId,
                        columnId,
                        taskId,
                        activityId);
                    return Results.NotFound();
                }

                log.LogInformation(
                    "Task activity fetched projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} activityId={ActivityId} type={ActivityType}",
                    projectId,
                    laneId,
                    columnId,
                    taskId,
                    activityId,
                    activity.Type);

                // Maps the entity to a DTO with full context for auditing
                var responseDto = activity.ToReadDto();
                return Results.Ok(responseDto);
            })
            .Produces<TaskActivityReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get task activity")
            .WithDescription("Returns a single activity if it belongs to the task.")
            .WithName("TaskActivities_Get_ById");


            // Global access group for querying task activities by authenticated user or admin context
            var top = app.MapGroup("/activities")
                .WithTags("Task Activities")
                .RequireAuthorization();

            // GET /activities/me
            top.MapGet("/me", async (
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserService,
                [FromServices] ITaskActivityReadService taskActivityReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskActivities.Get_Mine");

                // Lists all task activities performed by the currently authenticated user
                var userId = (Guid)currentUserService.UserId!;
                var activities = await taskActivityReadSvc.ListByUserAsync(userId, ct);
                var responseDto = activities.Select(a => a.ToReadDto()).ToList();

                log.LogInformation(
                    "Task activities listed for current user userId={UserId} count={Count}",
                    userId,
                    responseDto.Count);
                return Results.Ok(responseDto);
            })
            .Produces<IEnumerable<TaskActivityReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("List my activities")
            .WithDescription("Returns activities performed by the authenticated user.")
            .WithName("TaskActivities_Get_Mine");

            // GET /activities/users/{userId}
            top.MapGet("/users/{userId:guid}", async(
                [FromRoute] Guid userId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ITaskActivityReadService taskActivityReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskActivities.Get_ByUser");

                // Admin-only listing of all task activities performed by a given user across projects
                var activities = await taskActivityReadSvc.ListByUserAsync(userId, ct);
                var responseDto = activities.Select(a => a.ToReadDto()).ToList();

                log.LogInformation(
                    "Task activities listed for userId={UserId} count={Count}",
                    userId,
                    responseDto.Count);
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.SystemAdmin) // SystemAdmin-only
            .Produces<IEnumerable<TaskActivityReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List activities by user")
            .WithDescription("Admin-only. Returns activities performed by the specified user.")
            .WithName("TaskActivities_Get_ByUser");

            return top;
        }
    }
}
