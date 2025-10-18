using Api.Auth.Authorization;
using Api.Extensions;
using Api.Helpers;
using Application.Columns.Abstractions;
using Application.Columns.DTOs;
using Application.Columns.Mapping;
using Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    public static class ColumnEndpoints
    {
        public static RouteGroupBuilder MapColumns(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/projects/{projectId:guid}/lanes/{laneId:guid}/columns")
                .WithTags("Columns")
                .RequireAuthorization(Policies.ProjectReader);

            // GET /projects/{projectId}/lanes/{laneId}/columns
            group.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromServices] IColumnReadService columnReadSvc,
                CancellationToken ct = default) =>
            {
                var columns = await columnReadSvc.ListByLaneAsync(laneId, ct);
                var dto = columns.Select(c => c.ToReadDto()).ToList();
                return Results.Ok(dto);
            })
            .Produces<IEnumerable<ColumnReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get all columns")
            .WithDescription("Returns columns belonging to the specified lane.")
            .WithName("Columns_Get_All");

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}
            group.MapGet("/{columnId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromServices] IColumnReadService columnReadSvc,
                CancellationToken ct = default) =>
            {
                var column = await columnReadSvc.GetAsync(columnId, ct);
                return column is null ? Results.NotFound() : Results.Ok(column.ToReadDto());
            })
            .Produces<ColumnReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get column")
            .WithDescription("Returns a column by id within a lane.")
            .WithName("Columns_Get");

            // POST /projects/{projectId}/lanes/{laneId}/columns
            group.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromBody] ColumnCreateDto dto,
                [FromServices] IColumnWriteService columnWriteSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var (result, column) = await columnWriteSvc.CreateAsync(projectId, laneId, dto.Name, dto.Order, ct);
                if (result != DomainMutation.Created) return result.ToHttp();

                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(column!.RowVersion)}\"";
                return Results.Created($"/projects/{projectId}/lanes/{laneId}/columns/{column.Id}", column.ToReadDto());
            })
            .RequireAuthorization(Policies.ProjectMember)
            .Produces<ColumnReadDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Create column")
            .WithDescription("Creates a column in the lane and returns it.")
            .WithName("Columns_Create");

            // PUT /projects/{projectId}/lanes/{laneId}/columns/{columnId}/rename
            group.MapPut("/{columnId:guid}/rename", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromBody] ColumnRenameDto dto,
                [FromServices] IColumnWriteService columnWriteSvc,
                [FromServices] IColumnReadService columnReadSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    http, () => columnReadSvc.GetAsync(columnId, ct), c => c.RowVersion);

                var result = await columnWriteSvc.RenameAsync(columnId, dto.NewName, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(http);

                var renamed = await columnReadSvc.GetAsync(columnId, ct);
                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(renamed!.RowVersion)}\"";
                return Results.Ok(renamed.ToReadDto());
            })
            .RequireAuthorization(Policies.ProjectMember)
            .RequireIfMatch()
            .Produces<ColumnReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Rename column")
            .WithDescription("Renames a column and returns the updated column.")
            .WithName("Columns_Rename");

            // PUT /projects/{projectId}/lanes/{laneId}/columns/{columnId}/reorder
            group.MapPut("/{columnId:guid}/reorder", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromBody] ColumnReorderDto dto,
                [FromServices] IColumnWriteService columnWriteSvc,
                [FromServices] IColumnReadService columnReadSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    http, () => columnReadSvc.GetAsync(columnId, ct), c => c.RowVersion);

                var result = await columnWriteSvc.ReorderAsync(columnId, dto.NewOrder, rowVersion, ct);
                if (result != DomainMutation.Updated) return result.ToHttp(http);

                var reordered = await columnReadSvc.GetAsync(columnId, ct);
                http.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(reordered!.RowVersion)}\"";
                return Results.Ok(reordered.ToReadDto());
            })
            .RequireAuthorization(Policies.ProjectMember)
            .RequireIfMatch()
            .Produces<ColumnReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .WithSummary("Reorder column")
            .WithDescription("Changes column order and returns the updated column.")
            .WithName("Columns_Reorder");

            // DELETE /projects/{projectId}/lanes/{laneId}/columns/{columnId}
            group.MapDelete("/{columnId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromServices] IColumnReadService columnReadSvc,
                [FromServices] IColumnWriteService columnWriteSvc,
                HttpContext http,
                CancellationToken ct = default) =>
            {
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    http, () => columnReadSvc.GetAsync(columnId, ct), c => c.RowVersion);

                var result = await columnWriteSvc.DeleteAsync(columnId, rowVersion, ct);
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
            .WithSummary("Delete column")
            .WithDescription("Deletes a column.")
            .WithName("Columns_Delete");

            return group;
        }
    }
}
