using Api.Auth.Authorization;
using Api.Extensions;
using Api.Helpers;
using Application.Lanes.Abstractions;
using Application.Lanes.DTOs;
using Application.Lanes.Mapping;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    public static class LanesEndpoints
    {
        public static RouteGroupBuilder MapLanes(this IEndpointRouteBuilder app)
        {
            var group = app
                        .MapGroup("/projects/{projectId:guid}/lanes")
                        .WithTags("Lanes")
                        .RequireAuthorization(Policies.ProjectReader);

            // GET /projects/{projectId}/lanes/
            group.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ILaneReadService laneReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Lanes.Get_All");

                var lanes = await laneReadSvc.ListByProjectAsync(projectId, ct);
                var responseDto = lanes.Select(l => l.ToReadDto()).ToList();

                log.LogInformation("Lanes list returned projectId={ProjectId} count={Count}",
                                    projectId, responseDto.Count);
                return Results.Ok(responseDto);
            })
            .Produces<IEnumerable<LaneReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List lanes")
            .WithDescription("Returns lanes for the project.")
            .WithName("Lanes_Get_All");

            // GET /projects/{projectId}/lanes/{laneId}
            group.MapGet("/{laneId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ILaneReadService laneReadSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Lanes.Get_ById");

                var lane = await laneReadSvc.GetAsync(laneId, ct);
                if (lane is null)
                {
                    log.LogInformation("Lane not found projectId={ProjectId} laneId={LaneId}",
                                        projectId, laneId);
                    return Results.NotFound();
                }

                var responseDto = lane.ToReadDto();
                context.Response.SetETag(responseDto.RowVersion);

                log.LogInformation("Lane fetched projectId={ProjectId} laneId={LaneId} etag={ETag}",
                                    projectId, laneId, context.Response.Headers.ETag.ToString());
                return Results.Ok(responseDto);
            })
            .Produces<LaneReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get lane")
            .WithDescription("Returns a lane in the project. Sets ETag.")
            .WithName("Lanes_Get_ById");

            // POST /projects/{projectId}/lanes/
            group.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromBody] LaneCreateDto dto,
                [FromServices] ILoggerFactory logger,
                [FromServices] ILaneWriteService laneWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Lanes.Create");

                var (result, lane) = await laneWriteSvc.CreateAsync(projectId, dto.Name, dto.Order, ct);
                if (result != DomainMutation.Created || lane is null)
                {
                    log.LogInformation("Lane create rejected projectId={ProjectId} mutation={Mutation}",
                                        projectId, result);
                    return result.ToHttp(context);
                }

                var responseDto = lane.ToReadDto();
                context.Response.SetETag(responseDto.RowVersion);

                log.LogInformation("Lane created projectId={ProjectId} laneId={LaneId} order={Order} etag={ETag}",
                                    projectId, lane.Id, responseDto.Order, context.Response.Headers.ETag.ToString());
                return Results.CreatedAtRoute("Lanes_Get_ById", new { projectId, laneId = lane.Id }, responseDto);
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireValidation<LaneCreateDto>()
            .Produces<LaneReadDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Create lane")
            .WithDescription("Admin-only. Creates a lane in the project. Returns the resource with ETag.")
            .WithName("Lanes_Create");

            // PUT /projects/{projectId}/lanes/{laneId}/rename
            group.MapPut("/{laneId:guid}/rename", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromBody] LaneRenameDto dto,
                [FromServices] ILoggerFactory logger,
                [FromServices] ILaneReadService laneReadSvc,
                [FromServices] ILaneWriteService laneWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Lanes.Rename");

                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => laneReadSvc.GetAsync(laneId, ct), l => l.RowVersion);

                var result = await laneWriteSvc.RenameAsync(laneId, dto.NewName, rowVersion, ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation("Lane rename rejected projectId={ProjectId} laneId={LaneId} mutation={Mutation}",
                                        projectId, laneId, result);
                    return result.ToHttp(context);
                }

                var renamed = await laneReadSvc.GetAsync(laneId, ct);
                if (renamed is null)
                {
                    log.LogInformation("Lane rename readback missing projectId={ProjectId} laneId={LaneId}",
                                        projectId, laneId);
                    return Results.NotFound();
                }

                var responseDto = renamed.ToReadDto();
                context.Response.SetETag(responseDto.RowVersion);

                log.LogInformation("Lane renamed projectId={ProjectId} laneId={LaneId} newName={NewName} etag={ETag}",
                                    projectId, laneId, dto.NewName, context.Response.Headers.ETag.ToString());
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireValidation<LaneRenameDto>()
            .RequireIfMatch()
            .Produces<LaneReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Rename lane")
            .WithDescription("Admin-only. Renames a lane using optimistic concurrency (If-Match). Returns the updated resource and ETag.")
            .WithName("Lanes_Rename");

            // PUT /projects/{projectId}/lanes/{laneId}/reorder
            group.MapPut("/{laneId:guid}/reorder", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromBody] LaneReorderDto dto,
                [FromServices] ILoggerFactory logger,
                [FromServices] ILaneReadService laneReadSvc,
                [FromServices] ILaneWriteService laneWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Lanes.Reorder");

                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => laneReadSvc.GetAsync(laneId, ct), l => l.RowVersion);

                var result = await laneWriteSvc.ReorderAsync(laneId, dto.NewOrder, rowVersion, ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation("Lane reorder rejected projectId={ProjectId} laneId={LaneId} mutation={Mutation}",
                                        projectId, laneId, result);
                    return result.ToHttp(context);
                }

                var reordered = await laneReadSvc.GetAsync(laneId, ct);
                if (reordered is null)
                {
                    log.LogInformation("Lane reorder readback missing projectId={ProjectId} laneId={LaneId}",
                                        projectId, laneId);
                    return Results.NotFound();
                }

                var responseDto = reordered.ToReadDto();
                context.Response.SetETag(responseDto.RowVersion);

                log.LogInformation("Lane reordered projectId={ProjectId} laneId={LaneId} newOrder={NewOrder} etag={ETag}",
                                    projectId, laneId, dto.NewOrder, context.Response.Headers.ETag.ToString());
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireValidation<LaneReorderDto>()
            .RequireIfMatch()
            .Produces<LaneReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Reorder lane")
            .WithDescription("Admin-only. Changes lane order using optimistic concurrency (If-Match). Returns the updated resource and ETag.")
            .WithName("Lanes_Reorder");

            // DELETE /projects/{projectId}/lanes/{laneId}
            group.MapDelete("/{laneId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ILaneReadService laneReadSvc,
                [FromServices] ILaneWriteService laneWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Lanes.Delete");

                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => laneReadSvc.GetAsync(laneId, ct), l => l.RowVersion);

                var result = await laneWriteSvc.DeleteAsync(laneId, rowVersion, ct);

                log.LogInformation("Lane delete result projectId={ProjectId} laneId={LaneId} mutation={Mutation}",
                                    projectId, laneId, result);
                return result.ToHttp(context);
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireIfMatch()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Delete lane")
            .WithDescription("Admin-only. Deletes a lane using optimistic concurrency (If-Match).")
            .WithName("Lanes_Delete");

            return group;
        }
    }
}
