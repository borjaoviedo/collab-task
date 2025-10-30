using Api.Auth.Authorization;
using Api.Extensions;
using Api.Helpers;
using Application.Columns.Abstractions;
using Application.Columns.DTOs;
using Application.Columns.Mapping;
using Domain.Enums;
using Domain.ValueObjects;
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
        /// Registers the Columns endpoints under
        /// /projects/{projectId}/lanes/{laneId}/columns.
        /// Enforces auth at group level and adds per-endpoint
        /// validation and optimistic concurrency semantics.
        /// </summary>
        /// <param name="app">Endpoint route builder.</param>
        /// <returns>The configured route group.</returns>
        public static RouteGroupBuilder MapColumns(this IEndpointRouteBuilder app)
        {
            // Group all Column endpoints and enforce minimum project-level authorization
            var group = app
                        .MapGroup("/projects/{projectId:guid}/lanes/{laneId:guid}/columns")
                        .WithTags("Columns")
                        .RequireAuthorization(Policies.ProjectReader);

            // OpenAPI metadata across all endpoints: ensures generated clients and API docs
            // include consistent success/error shapes, auth requirements, and concurrency responses

            // GET /projects/{projectId}/lanes/{laneId}/columns
            group.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromServices] ILoggerFactory logger,
                [FromServices] IColumnReadService columnReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Columns.Get_All");

                // Read-only list for the lane
                var columns = await columnReadSvc.ListByLaneAsync(laneId, ct);
                var responseDto = columns.Select(c => c.ToReadDto()).ToList();

                log.LogInformation(
                    "Columns list returned projectId={ProjectId} laneId={LaneId} count={Count}",
                    projectId,
                    laneId,
                    responseDto.Count);
                return Results.Ok(responseDto);
            })
            .Produces<IEnumerable<ColumnReadDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .WithSummary("List columns")
            .WithDescription("Returns columns for the lane.")
            .WithName("Columns_Get_All");

            // GET /projects/{projectId}/lanes/{laneId}/columns/{columnId}
            group.MapGet("/{columnId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromServices] ILoggerFactory logger,
                [FromServices] IColumnReadService columnReadSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Columns.Get_ById");

                // Fetch by id from read side.
                // Return 404 if not found to avoid leaking existence across projects
                var column = await columnReadSvc.GetAsync(columnId, ct);
                if (column is null)
                {
                    log.LogInformation(
                        "Column not found projectId={ProjectId} laneId={LaneId} columnId={ColumnId}",
                        projectId,
                        laneId,
                        columnId);
                    return Results.NotFound();
                }

                // Compute weak ETag from RowVersion for conditional requests
                var responseDto = column.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Column fetched projectId={ProjectId} laneId={LaneId} columnId={ColumnId} etag={ETag}",
                    projectId,
                    laneId,
                    columnId,
                    etag);

                // Attach ETag so clients can use If-None-Match or track concurrency
                return Results.Ok(responseDto).WithETag(etag);
            })
            .Produces<ColumnReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithSummary("Get column")
            .WithDescription("Returns a column in the lane. Sets ETag.")
            .WithName("Columns_Get_ById");

            // POST /projects/{projectId}/lanes/{laneId}/columns
            group.MapPost("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromBody] ColumnCreateDto dto,
                [FromServices] ILoggerFactory logger,
                [FromServices] IColumnWriteService columnWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Columns.Create");

                var columnName = ColumnName.Create(dto.Name);

                // Write side: create column with domain VO for name and explicit order
                // DomainMutation drives HTTP mapping (201, 400, 409, etc.)
                var (result, column) = await columnWriteSvc.CreateAsync(
                    projectId,
                    laneId,
                    columnName,
                    dto.Order,
                    ct);

                if (result != DomainMutation.Created || column is null)
                {
                    log.LogInformation(
                        "Column create rejected projectId={ProjectId} laneId={LaneId} mutation={Mutation}",
                        projectId,
                        laneId,
                        result);
                    return result.ToHttp(context);
                }

                // Shape response and emit ETag for the newly created resource
                var responseDto = column.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Column created projectId={ProjectId} laneId={LaneId} columnId={ColumnId} order={Order} etag={ETag}",
                    projectId,
                    laneId,
                    column.Id,
                    responseDto.Order,
                    etag);

                // Location header via route name ensures stable client navigation
                var routeValues = new { projectId, laneId, columnId = column.Id };
                return Results.CreatedAtRoute("Columns_Get_ById", routeValues, responseDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
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

            // PUT /projects/{projectId}/lanes/{laneId}/columns/{columnId}/rename
            group.MapPut("/{columnId:guid}/rename", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromBody] ColumnRenameDto dto,
                [FromServices] ILoggerFactory logger,
                [FromServices] IColumnReadService columnReadSvc,
                [FromServices] IColumnWriteService columnWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Columns.Rename");

                // Resolve current RowVersion either from If-Match or storage fallback
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => columnReadSvc.GetAsync(columnId, ct),
                    c => c.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation(
                        "Column not found when resolving row version projectId={ProjectId} laneId={LaneId} columnId={ColumnId}",
                        projectId,
                        laneId,
                        columnId);
                    return Results.NotFound();
                }

                var newName = ColumnName.Create(dto.NewName);

                // Domain-level rename guarded by optimistic concurrency
                var result = await columnWriteSvc.RenameAsync(
                    columnId,
                    newName,
                    rowVersion,
                    ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation(
                        "Column rename rejected projectId={ProjectId} laneId={LaneId} columnId={ColumnId} mutation={Mutation}",
                        projectId,
                        laneId,
                        columnId,
                        result);
                    return result.ToHttp(context);
                }

                // Read back to return the fresh representation and a new ETag
                var renamed = await columnReadSvc.GetAsync(columnId, ct);
                if (renamed is null)
                {
                    log.LogInformation(
                        "Column rename readback missing projectId={ProjectId} laneId={LaneId} columnId={ColumnId}",
                        projectId,
                        laneId,
                        columnId);
                    return Results.NotFound();
                }

                var responseDto = renamed.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Column renamed projectId={ProjectId} laneId={LaneId} columnId={ColumnId} newName={NewName} etag={ETag}",
                    projectId,
                    laneId,
                    columnId,
                    dto.NewName,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireValidation<ColumnRenameDto>()
            .RequireIfMatch() // Require If-Match to prevent lost updates. Returns 428 if missing, 412 if stale
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

            // PUT /projects/{projectId}/lanes/{laneId}/columns/{columnId}/reorder
            group.MapPut("/{columnId:guid}/reorder", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromBody] ColumnReorderDto dto,
                [FromServices] ILoggerFactory logger,
                [FromServices] IColumnReadService columnReadSvc,
                [FromServices] IColumnWriteService columnWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Columns.Reorder");

                // Resolve RowVersion and apply order change using optimistic concurrency
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => columnReadSvc.GetAsync(columnId, ct),
                    c => c.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation(
                        "Column not found when resolving row version projectId={ProjectId} laneId={LaneId} columnId={ColumnId}",
                        projectId,
                        laneId,
                        columnId);
                    return Results.NotFound();
                }

                var result = await columnWriteSvc.ReorderAsync(columnId, dto.NewOrder, rowVersion, ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation(
                        "Column reorder rejected projectId={ProjectId} laneId={LaneId} columnId={ColumnId} mutation={Mutation}",
                        projectId,
                        laneId,
                        columnId,
                        result);
                    return result.ToHttp(context);
                }

                // Return updated state with refreshed ETag for subsequent operations
                var reordered = await columnReadSvc.GetAsync(columnId, ct);
                if (reordered is null)
                {
                    log.LogInformation(
                        "Column reorder readback missing projectId={ProjectId} laneId={LaneId} columnId={ColumnId}",
                        projectId,
                        laneId,
                        columnId);
                    return Results.NotFound();
                }

                var responseDto = reordered.ToReadDto();
                var etag = ETag.EncodeWeak(responseDto.RowVersion);

                log.LogInformation(
                    "Column reordered projectId={ProjectId} laneId={LaneId} columnId={ColumnId} newOrder={NewOrder} etag={ETag}",
                    projectId,
                    laneId,
                    columnId,
                    dto.NewOrder,
                    etag);
                return Results.Ok(responseDto).WithETag(etag);
            })
            .RequireAuthorization(Policies.ProjectAdmin) // ProjectAdmin-only
            .RequireValidation<ColumnReorderDto>()
            .RequireIfMatch() // Require If-Match to guarantee ordering changes are not applied over stale state
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

            // DELETE /projects/{projectId}/lanes/{laneId}/columns/{columnId}
            group.MapDelete("/{columnId:guid}", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromRoute] Guid columnId,
                [FromServices] ILoggerFactory logger,
                [FromServices] IColumnReadService columnReadSvc,
                [FromServices] IColumnWriteService columnWriteSvc,
                HttpContext context,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Columns.Delete");

                // Resolve RowVersion precondition; deletion must be conditional to avoid
                // removing an entity modified by someone else
                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context,
                    ct => columnReadSvc.GetAsync(columnId, ct),
                    c => c.RowVersion,
                    ct);

                if (rowVersion is null)
                {
                    log.LogInformation(
                        "Column not found when resolving row version projectId={ProjectId} laneId={LaneId} columnId={ColumnId}",
                        projectId,
                        laneId,
                        columnId);
                    return Results.NotFound();
                }

                // Map DomainMutation to HTTP (204 on success, 404/409/412 as appropriate)
                var result = await columnWriteSvc.DeleteAsync(columnId, rowVersion, ct);

                log.LogInformation(
                    "Column delete result projectId={ProjectId} laneId={LaneId} columnId={ColumnId} mutation={Mutation}",
                    projectId,
                    laneId,
                    columnId,
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
            .WithSummary("Delete column")
            .WithDescription("Admin-only. Deletes a column using optimistic concurrency (If-Match).")
            .WithName("Columns_Delete");

            return group;
        }
    }
}
