using Api.Auth.Authorization;
using Api.Extensions;
using Api.Filters;
using Api.Helpers;
using Application.Lanes.Abstractions;
using Application.Lanes.DTOs;
using Application.Lanes.Mapping;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    public static class LaneEndpoints
    {
        public static RouteGroupBuilder MapLanes(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/projects/{projectId:guid}/lanes")
                .WithTags("Lanes")
                .RequireAuthorization(Policies.ProjectReader);

            // GET /projects/{projectId}/lanes/
            group.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromServices] ILaneReadService svc,
                CancellationToken ct = default) =>
            {
                var lanes = await svc.ListByProjectAsync(projectId, ct);
                var dto = lanes.Select(l => l.ToReadDto()).ToList();
                return Results.Ok(dto);
            })
            .Produces<IEnumerable<LaneReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("Get all lanes")
            .WithDescription("Returns lanes belonging to the specified project.")
            .WithName("Lanes_Get_All");

            // GET /projects/{projectId}/lanes/{laneId}
            group.MapGet("/{laneId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromServices] ILaneReadService laneReadSvc,
                CancellationToken ct = default) =>
            {
                var lane = await laneReadSvc.GetAsync(laneId, ct);
                return lane is null ? Results.NotFound() : Results.Ok(lane.ToReadDto());
            })
            .Produces<LaneReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get lane")
            .WithDescription("Returns a lane by id within a project.")
            .WithName("Lanes_Get");

            // POST /projects/{projectId}/lanes/
            group.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromBody] LaneCreateDto dto,
                [FromServices] ILaneWriteService svc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var (result, lane) = await svc.CreateAsync(projectId, dto.Name, dto.Order, ct);
                if (result != DomainMutation.Created) return result.ToHttp();

                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(lane!.RowVersion)}\"";
                return Results.Created($"/projects/{projectId}/lanes/{lane.Id}", lane.ToReadDto());
            })
            .RequireAuthorization(Policies.ProjectMember)
            .Produces<LaneReadDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Create lane")
            .WithDescription("Creates a lane in the project and returns it.")
            .WithName("Lanes_Create");

            // PUT /projects/{projectId}/lanes/{laneId}/rename
            group.MapPut("/{laneId:guid}/rename", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromBody] LaneRenameDto dto,
                [FromServices] ILaneWriteService laneWriteSvc,
                [FromServices] ILaneReadService laneReadSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    http, () => laneReadSvc.GetAsync(laneId, ct), l => l.RowVersion);

                var result = await laneWriteSvc.RenameAsync(laneId, dto.NewName, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(http);

                var renamed = await laneReadSvc.GetAsync(laneId, ct);
                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(renamed!.RowVersion)}\"";
                return Results.Ok(renamed.ToReadDto());
            })
            .RequireAuthorization(Policies.ProjectMember)
            .RequireIfMatch()
            .Produces<LaneReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Rename lane")
            .WithDescription("Renames a lane and returns the updated lane.")
            .WithName("Lanes_Rename");

            // PUT /projects/{projectId}/lanes/{laneId}/reorder
            group.MapPut("/{laneId:guid}/reorder", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromBody] LaneReorderDto dto,
                [FromServices] ILaneWriteService laneWriteSvc,
                [FromServices] ILaneReadService laneReadSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    http, () => laneReadSvc.GetAsync(laneId, ct), l => l.RowVersion);

                var result = await laneWriteSvc.ReorderAsync(laneId, dto.NewOrder, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(http);

                var reordered = await laneReadSvc.GetAsync(laneId, ct);
                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(reordered!.RowVersion)}\"";
                return Results.Ok(reordered.ToReadDto());
            })
            .RequireAuthorization(Policies.ProjectMember)
            .RequireIfMatch()
            .Produces<LaneReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Reorder lane")
            .WithDescription("Changes lane order and returns the updated lane.")
            .WithName("Lanes_Reorder");

            // DELETE /projects/{projectId}/lanes/{laneId}
            group.MapDelete("/{laneId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromServices] ILaneWriteService laneWriteSvc,
                [FromServices] ILaneReadService laneReadSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    http, () => laneReadSvc.GetAsync(laneId, ct), l => l.RowVersion);

                var result = await laneWriteSvc.DeleteAsync(laneId, rowVersion, ct);
                return result.ToHttp(http);
            })
            .RequireAuthorization(Policies.ProjectMember)
            .RequireIfMatch()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Delete lane")
            .WithDescription("Deletes a lane in the specified project.")
            .WithName("Lanes_Delete");

            return group;
        }
    }
}
