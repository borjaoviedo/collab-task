using Api.Auth.Authorization;
using Application.Common.Abstractions.Auth;
using Application.TaskActivities.Abstractions;
using Application.TaskActivities.DTOs;
using Application.TaskActivities.Mapping;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    public static class TaskActivitiesEndpoints
    {
        public static RouteGroupBuilder MapTaskActivities(this IEndpointRouteBuilder app)
        {
            var nested = app
                            .MapGroup("/projects/{projectId:guid}/lanes/{laneId:guid}/columns/{columnId:guid}/tasks/{taskId:guid}/activities")
                            .WithTags("Task Activities")
                            .RequireAuthorization(Policies.ProjectReader);

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

                var activities = activityType is null
                    ? await taskActivityReadSvc.ListByTaskAsync(taskId, ct)
                    : await taskActivityReadSvc.ListByTypeAsync(taskId, activityType.Value, ct);

                var responseDto = activities.Select(a => a.ToReadDto()).ToList();

                log.LogInformation("Task activities listed projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} activityType={ActivityType} count={Count}",
                                    projectId, laneId, columnId, taskId, activityType, responseDto.Count);
                return Results.Ok(responseDto);
            })
            .Produces<IEnumerable<TaskActivityReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
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
                [FromServices] ILoggerFactory logger,
                [FromServices] ITaskActivityReadService taskActivityReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskActivities.Get_ById");

                var activity = await taskActivityReadSvc.GetAsync(activityId, ct);
                if (activity is null)
                {
                    log.LogInformation("Task activity not found projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} activityId={ActivityId}",
                                        projectId, laneId, columnId, taskId, activityId);
                    return Results.NotFound();
                }

                var responseDto = activity.ToReadDto();

                log.LogInformation("Task activity fetched projectId={ProjectId} laneId={LaneId} columnId={ColumnId} taskId={TaskId} activityId={ActivityId} type={ActivityType}",
                                    projectId, laneId, columnId, taskId, activityId, activity.Type);
                return Results.Ok(responseDto);
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
                [FromServices] ILoggerFactory logger,
                [FromServices] ICurrentUserService currentUserService,
                [FromServices] ITaskActivityReadService taskActivityReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskActivities.Get_Mine");

                var userId = (Guid)currentUserService.UserId!;
                var activities = await taskActivityReadSvc.ListByActorAsync(userId, ct);
                var responseDto = activities.Select(a => a.ToReadDto()).ToList();

                log.LogInformation("Task activities listed for current user userId={UserId} count={Count}",
                                    userId, responseDto.Count);
                return Results.Ok(responseDto);
            })
            .Produces<IEnumerable<TaskActivityReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithSummary("List my activities")
            .WithDescription("Lists task activities performed by the authenticated user.")
            .WithName("TaskActivities_Get_Mine");

            // GET /activities/users/{userId}
            top.MapGet("/users/{userId:guid}", async(
                [FromRoute] Guid userId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ITaskActivityReadService taskActivityReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("TaskActivities.Get_ByUser");

                var activities = await taskActivityReadSvc.ListByActorAsync(userId, ct);
                var responseDto = activities.Select(a => a.ToReadDto()).ToList();

                log.LogInformation("Task activities listed for userId={UserId} count={Count}",
                                    userId, responseDto.Count);
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.SystemAdmin)
            .Produces<IEnumerable<TaskActivityReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List activities by user")
            .WithDescription("Lists task activities performed by the specified user.")
            .WithName("TaskActivities_Get_ByUser");

            return top;
        }
    }
}
