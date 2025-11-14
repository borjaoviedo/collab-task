using Api.Auth.Authorization;
using Api.Concurrency;
using Api.Filters;
using Api.HttpMapping;
using Application.Lanes.Abstractions;
using Application.Lanes.DTOs;
using Application.Lanes.Mapping;
using Domain.Enums;
using Domain.ValueObjects;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    /// <summary>
    /// Lane endpoints within a project: list, get, create, rename, reorder, and delete.
    /// Enforces project-level read access at the group; admin required for mutations.
    /// Employs domain value objects and optimistic concurrency via ETag/If-Match.
    /// </summary>
    public static class LanesEndpoints
    {
        /// <summary>
        /// Registers the Lanes endpoints under
        /// /projects/{projectId}/lanes/.
        /// Enforces auth at group level and adds per-endpoint
        /// validation and optimistic concurrency semantics.
        /// </summary>
        /// <param name="app">Endpoint route builder.</param>
        /// <returns>The configured route group.</returns>
        public static RouteGroupBuilder MapLanes(this IEndpointRouteBuilder app)
        {
            // Group all Lane endpoints and enforce minimum project-level authorization
            var group = app
                        .MapGroup("/projects/{projectId:guid}/lanes")
                        .WithTags("Lanes")
                        .RequireAuthorization(Policies.ProjectReader);

            // OpenAPI metadata across all endpoints: ensures generated clients and API docs
            // include consistent success/error shapes, auth requirements, and concurrency responses

            // GET /projects/{projectId}/lanes/
            group.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromServices] ILoggerFactory logger,
                [FromServices] ILaneReadService laneReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Lanes.Get_All");

                // Read-only list for the project
                var lanes = await laneReadSvc.ListByProjectAsync(projectId, ct);
                var responseDto = lanes.Select(l => l.ToReadDto()).ToList();

                log.LogInformation(
                    "Lanes list returned projectId={ProjectId} count={Count}",
                    projectId,
                    responseDto.Count);
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

                // Fetch by id from read side.
                // Return 404 if not found to avoid leaking existence across projects
                var lane = await laneReadSvc.GetAsync(laneId, ct);
                if (lane is null)
                {
                    log.LogInformation(
                        "Lane not found projectId={ProjectId} laneId={LaneId}",
                        projectId,
                        laneId);
                    return Results.NotFound();
                }

                // Compute weak ETag from RowVersion for conditional requests
                var responseDto = lane.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Lane fetched projectId={ProjectId} laneId={LaneId} etag={ETag}",
                    projectId,
                    laneId,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
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

                var laneName = LaneName.Create(dto.Name);

                // Write side: create lane with domain VO for name and explicit order
                // DomainMutation drives HTTP mapping (201, 400, 409, etc.)
                var (result, lane) = await laneWriteSvc.CreateAsync(projectId, laneName, dto.Order, ct);

                if (result != DomainMutation.Created || lane is null)
                {
                    log.LogInformation(
                        "Lane create rejected projectId={ProjectId} mutation={Mutation}",
                        projectId,
                        result);
                    return result.ToHttp(context);
                }

                // Shape response and emit ETag for the newly created resource
                var responseDto = lane.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Lane created projectId={ProjectId} laneId={LaneId} order={Order} etag={ETag}",
                    projectId,
                    lane.Id,
                    responseDto.Order,
                    etag);

                // Location header via route name ensures stable client navigation
                var routeValues = new { projectId, laneId = lane.Id };
                return Results.CreatedAtRoute("Lanes_Get_ById", routeValues, responseDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireValidation<LaneCreateDto>()
            .RejectIfMatch() // Reject If-Match on create: new resources must not carry preconditions
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

                // Resolve current RowVersion either from If-Match or storage fallback
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => laneReadSvc.GetAsync(laneId, ct),
                    l => l.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation(
                        "Lane not found when resolving row version projectId={ProjectId} laneId={LaneId}",
                        projectId,
                        laneId);
                    return Results.NotFound();
                }

                var newName = LaneName.Create(dto.NewName);

                // Domain-level rename guarded by optimistic concurrency
                var result = await laneWriteSvc.RenameAsync(laneId, newName, rowVersion, ct);

                if (result != DomainMutation.Updated)
                {
                    log.LogInformation(
                        "Lane rename rejected projectId={ProjectId} laneId={LaneId} mutation={Mutation}",
                        projectId,
                        laneId,
                        result);
                    return result.ToHttp(context);
                }

                // Read back to return the fresh representation and a new ETag
                var renamed = await laneReadSvc.GetAsync(laneId, ct);
                if (renamed is null)
                {
                    log.LogInformation(
                        "Lane rename readback missing projectId={ProjectId} laneId={LaneId}",
                        projectId,
                        laneId);
                    return Results.NotFound();
                }

                var responseDto = renamed.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Lane renamed projectId={ProjectId} laneId={LaneId} newName={NewName} etag={ETag}",
                    projectId,
                    laneId,
                    dto.NewName,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireValidation<LaneRenameDto>()
            .RequireIfMatch() // Require If-Match to prevent lost updates. Returns 428 if missing, 412 if stale
            .Produces<LaneReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
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

                // Resolve RowVersion and apply order change using optimistic concurrency
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => laneReadSvc.GetAsync(laneId, ct),
                    l => l.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation(
                        "Lane not found when resolving row version projectId={ProjectId} laneId={LaneId}",
                        projectId,
                        laneId);
                    return Results.NotFound();
                }

                var result = await laneWriteSvc.ReorderAsync(laneId, dto.NewOrder, rowVersion, ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation(
                        "Lane reorder rejected projectId={ProjectId} laneId={LaneId} mutation={Mutation}",
                        projectId,
                        laneId,
                        result);
                    return result.ToHttp(context);
                }

                // Return updated state with refreshed ETag for subsequent operations
                var reordered = await laneReadSvc.GetAsync(laneId, ct);
                if (reordered is null)
                {
                    log.LogInformation(
                        "Lane reorder readback missing projectId={ProjectId} laneId={LaneId}",
                        projectId,
                        laneId);
                    return Results.NotFound();
                }

                var responseDto = reordered.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Lane reordered projectId={ProjectId} laneId={LaneId} newOrder={NewOrder} etag={ETag}",
                    projectId,
                    laneId,
                    dto.NewOrder,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireValidation<LaneReorderDto>()
            .RequireIfMatch() // Require If-Match to guarantee ordering changes are not applied over stale state
            .Produces<LaneReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
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

                // Resolve RowVersion precondition; deletion must be conditional to avoid
                // removing an entity modified by someone else
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => laneReadSvc.GetAsync(laneId, ct),
                    l => l.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation(
                        "Lane not found when resolving row version projectId={ProjectId} laneId={LaneId}",
                        projectId,
                        laneId);
                    return Results.NotFound();
                }

                // Map DomainMutation to HTTP (204 on success, 404/409/412 as appropriate)
                var result = await laneWriteSvc.DeleteAsync(laneId, rowVersion, ct);

                log.LogInformation(
                    "Lane delete result projectId={ProjectId} laneId={LaneId} mutation={Mutation}",
                    projectId,
                    laneId,
                    result);
                return result.ToHttp(context);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireIfMatch() // Require If-Match to enforce safe deletes under optimistic concurrency
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Delete lane")
            .WithDescription("Admin-only. Deletes a lane using optimistic concurrency (If-Match).")
            .WithName("Lanes_Delete");

            return group;
        }
    }
}
