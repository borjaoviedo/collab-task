using Api.Auth.Authorization;
using Api.Concurrency;
using Api.Filters;
using Application.Columns.Abstractions;
using Application.Columns.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints
{
    /// <summary>
    /// Column endpoints within a lane: list, get, create, rename, reorder, and delete.
    /// Enforces project-level read access at the group; admin required for mutations.
    /// Employs domain value objects and optimistic concurrency via ETag/If-Match.
    /// </summary>
    public static class ColumnsEndpoints
    {
        /// <summary>
        /// Registers the Columns endpoints under:
        /// - /projects/{projectId}/lanes/{laneId}/columns (create),
        /// - /lanes/{laneId}/columns (list by lane),
        /// - /columns/{columnId} (item operations).
        /// Enforces auth at group level and adds per-endpoint
        /// validation and optimistic concurrency semantics.
        /// </summary>
        /// <param name="app">Endpoint route builder.</param>
        /// <returns>The configured route group for column item endpoints.</returns>
        public static RouteGroupBuilder MapColumns(this IEndpointRouteBuilder app)
        {
            // /projects/{projectId}/lanes/{laneId}/columns
            var projectColumnsGroup = app
                .MapGroup("/projects/{projectId:guid}/lanes/{laneId:guid}/columns")
                .WithTags("Columns")
                .RequireAuthorization(Policies.ProjectAdmin);

            // POST /projects/{projectId}/lanes/{laneId}/columns
            projectColumnsGroup.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromBody] ColumnCreateDto dto,
                [FromServices] IColumnWriteService columnWriteSvc,
                CancellationToken ct = default) =>
            {
                var columnReadDto = await columnWriteSvc.CreateAsync(projectId, laneId, dto, ct);
                var etag = ETag.EncodeWeak(columnReadDto.RowVersion);

                var routeValues = new { columnId = columnReadDto.Id };
                return Results
                    .CreatedAtRoute("Columns_Get_ById", routeValues, columnReadDto)
                    .WithETag(etag);
            })
            .RequireValidation<ColumnCreateDto>()
            .RejectIfMatch() // Reject If-Match on create: new resources must not carry preconditions
            .Produces<ColumnReadDto>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithSummary("Create column")
            .WithDescription("Admin-only. Creates a column in the lane. Returns the resource with ETag.")
            .WithName("Columns_Create");

            // /lanes/{laneId}/columns
            var lanesGroup = app
                .MapGroup("/lanes/{laneId:guid}/columns")
                .WithTags("Columns")
                .RequireAuthorization(Policies.ProjectReader);

            // GET /lanes/{laneId}/columns
            lanesGroup.MapGet("/", async (
                [FromRoute] Guid laneId,
                [FromServices] IColumnReadService columnReadSvc,
                CancellationToken ct = default) =>
            {
                var columnReadDtoList = await columnReadSvc.ListByLaneIdAsync(laneId, ct);
                return Results.Ok(columnReadDtoList);
            })
            .Produces<IEnumerable<ColumnReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List columns")
            .WithDescription("Returns columns for the lane.")
            .WithName("Columns_Get_All");

            // /columns/{columnId}
            var columnsGroup = app
                .MapGroup("/columns/{columnId:guid}")
                .WithTags("Columns")
                .RequireAuthorization(Policies.ProjectReader);

            // GET /columns/{columnId}
            columnsGroup.MapGet("/", async (
                [FromRoute] Guid columnId,
                [FromServices] IColumnReadService columnReadSvc,
                CancellationToken ct = default) =>
            {
                var columnReadDto = await columnReadSvc.GetByIdAsync(columnId, ct);
                var etag = ETag.EncodeWeak(columnReadDto.RowVersion);

                return Results.Ok(columnReadDto).WithETag(etag);
            })
            .Produces<ColumnReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get column")
            .WithDescription("Returns a column in the lane. Sets ETag.")
            .WithName("Columns_Get_ById");

            // PUT /columns/{columnId}/rename
            columnsGroup.MapPut("/rename", async (
                [FromRoute] Guid columnId,
                [FromBody] ColumnRenameDto dto,
                [FromServices] IColumnWriteService columnWriteSvc,
                CancellationToken ct = default) =>
            {
                var columnReadDto = await columnWriteSvc.RenameAsync(columnId, dto, ct);
                var etag = ETag.EncodeWeak(columnReadDto.RowVersion);

                return Results.Ok(columnReadDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireValidation<ColumnRenameDto>()
            .RequireIfMatch() // Require If-Match to prevent lost updates. Returns 428 if missing, 412 if stale
            .EnsureIfMatch<IColumnReadService, ColumnReadDto>(routeValueKey: "columnId")
            .Produces<ColumnReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Rename column")
            .WithDescription("Admin-only. Renames a column using optimistic concurrency (If-Match). Returns the updated resource and ETag.")
            .WithName("Columns_Rename");

            // PUT /columns/{columnId}/reorder
            columnsGroup.MapPut("/reorder", async (
                [FromRoute] Guid columnId,
                [FromBody] ColumnReorderDto dto,
                [FromServices] IColumnWriteService columnWriteSvc,
                CancellationToken ct = default) =>
            {
                var columnReadDto = await columnWriteSvc.ReorderAsync(columnId, dto, ct);
                var etag = ETag.EncodeWeak(columnReadDto.RowVersion);

                return Results.Ok(columnReadDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireValidation<ColumnReorderDto>()
            .RequireIfMatch() // Require If-Match to guarantee ordering changes are not applied over stale state
            .EnsureIfMatch<IColumnReadService, ColumnReadDto>(routeValueKey: "columnId")
            .Produces<ColumnReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Reorder column")
            .WithDescription("Admin-only. Changes column order using optimistic concurrency (If-Match). Returns the updated resource and ETag.")
            .WithName("Columns_Reorder");

            // DELETE /columns/{columnId}
            columnsGroup.MapDelete("/", async (
                [FromRoute] Guid columnId,
                [FromServices] IColumnWriteService columnWriteSvc,
                CancellationToken ct = default) =>
            {
                await columnWriteSvc.DeleteByIdAsync(columnId, ct);
                return Results.NoContent();
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireIfMatch() // Require If-Match to enforce safe deletes under optimistic concurrency
            .EnsureIfMatch<IColumnReadService, ColumnReadDto>(routeValueKey: "columnId")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
            .ProducesProblem(StatusCodes.Status428PreconditionRequired)
            .WithSummary("Delete column")
            .WithDescription("Admin-only. Deletes a column using optimistic concurrency (If-Match).")
            .WithName("Columns_Delete");

            return columnsGroup;
        }
    }
}
