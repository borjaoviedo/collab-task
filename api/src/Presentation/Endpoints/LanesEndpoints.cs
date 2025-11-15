using Api.Auth.Authorization;
using Api.Concurrency;
using Api.Filters;
using Application.Lanes.Abstractions;
using Application.Lanes.DTOs;
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
        /// Registers the Lanes endpoints under:
        /// - /projects/{projectId}/lanes (project-scoped collection),
        /// - /lanes/{laneId} (lane item operations).
        /// Enforces auth at group level and adds per-endpoint
        /// validation and optimistic concurrency semantics.
        /// </summary>
        /// <param name="app">Endpoint route builder.</param>
        /// <returns>The configured route group for lane item endpoints.</returns>
        public static RouteGroupBuilder MapLanes(this IEndpointRouteBuilder app)
        {
            // /projects/{projectId}/lanes
            var projectLanesGroup = app
                .MapGroup("/projects/{projectId:guid}/lanes")
                .WithTags("Lanes")
                .RequireAuthorization(Policies.ProjectReader);

            // GET /projects/{projectId}/lanes
            projectLanesGroup.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromServices] ILaneReadService laneReadSvc,
                CancellationToken ct = default) =>
            {
                var laneReadDtoList = await laneReadSvc.ListByProjectIdAsync(projectId, ct);
                return Results.Ok(laneReadDtoList);
            })
            .Produces<IEnumerable<LaneReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List lanes")
            .WithDescription("Returns lanes for the project.")
            .WithName("Lanes_Get_All");

            // POST /projects/{projectId}/lanes
            projectLanesGroup.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromBody] LaneCreateDto dto,
                [FromServices] ILaneWriteService laneWriteSvc,
                CancellationToken ct = default) =>
            {
                var laneReadDto = await laneWriteSvc.CreateAsync(projectId, dto, ct);
                var etag = ETag.EncodeWeak(laneReadDto.RowVersion);

                var routeValues = new { laneId = laneReadDto.Id };
                return Results
                    .CreatedAtRoute("Lanes_Get_ById", routeValues, laneReadDto)
                    .WithETag(etag);
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

            // /lanes/{laneId}
            var lanesGroup = app
                .MapGroup("/lanes/{laneId:guid}")
                .WithTags("Lanes")
                .RequireAuthorization(Policies.ProjectReader);

            // GET /lanes/{laneId}
            lanesGroup.MapGet("/", async (
                [FromRoute] Guid laneId,
                [FromServices] ILaneReadService laneReadSvc,
                CancellationToken ct = default) =>
            {
                var laneReadDto = await laneReadSvc.GetByIdAsync(laneId, ct);
                var etag = ETag.EncodeWeak(laneReadDto.RowVersion);

                return Results.Ok(laneReadDto).WithETag(etag);
            })
            .Produces<LaneReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get lane")
            .WithDescription("Returns a lane in the project. Sets ETag.")
            .WithName("Lanes_Get_ById");

            // PUT /lanes/{laneId}/rename
            lanesGroup.MapPut("/rename", async (
                [FromRoute] Guid laneId,
                [FromBody] LaneRenameDto dto,
                [FromServices] ILaneWriteService laneWriteSvc,
                CancellationToken ct = default) =>
            {
                var laneReadDto = await laneWriteSvc.RenameAsync(laneId, dto, ct);
                var etag = ETag.EncodeWeak(laneReadDto.RowVersion);

                return Results.Ok(laneReadDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireValidation<LaneRenameDto>()
            .RequireIfMatch() // Require If-Match to prevent lost updates. Returns 428 if missing, 412 if stale
            .EnsureIfMatch<ILaneReadService, LaneReadDto>(routeValueKey: "laneId")
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

            // PUT /lanes/{laneId}/reorder
            lanesGroup.MapPut("/reorder", async (
                [FromRoute] Guid laneId,
                [FromBody] LaneReorderDto dto,
                [FromServices] ILaneWriteService laneWriteSvc,
                CancellationToken ct = default) =>
            {
                var laneReadDto = await laneWriteSvc.ReorderAsync(laneId, dto, ct);
                var etag = ETag.EncodeWeak(laneReadDto.RowVersion);

                return Results.Ok(laneReadDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireValidation<LaneReorderDto>()
            .RequireIfMatch() // Require If-Match to guarantee ordering changes are not applied over stale state
            .EnsureIfMatch<ILaneReadService, LaneReadDto>(routeValueKey: "laneId")
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

            // DELETE /lanes/{laneId}
            lanesGroup.MapDelete("/", async (
                [FromRoute] Guid laneId,
                [FromServices] ILaneWriteService laneWriteSvc,
                CancellationToken ct = default) =>
            {
                await laneWriteSvc.DeleteByIdAsync(laneId, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireIfMatch() // Require If-Match to enforce safe deletes under optimistic concurrency
            .EnsureIfMatch<ILaneReadService, LaneReadDto>(routeValueKey: "laneId")
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

            return lanesGroup;
        }
    }
}
