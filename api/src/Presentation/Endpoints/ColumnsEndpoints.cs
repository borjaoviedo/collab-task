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
    public static class ColumnsEndpoints
    {
        public static RouteGroupBuilder MapColumns(this IEndpointRouteBuilder app)
        {
            var group = app
                        .MapGroup("/projects/{projectId:guid}/lanes/{laneId:guid}/columns")
                        .WithTags("Columns")
                        .RequireAuthorization(Policies.ProjectReader);

            // GET /projects/{projectId}/lanes/{laneId}/columns
            group.MapGet("/", async (
                [FromRoute] Guid projectId,
                [FromRoute] Guid laneId,
                [FromServices] ILoggerFactory logger,
                [FromServices] IColumnReadService columnReadSvc,
                CancellationToken ct = default) =>
            {
                var log = logger.CreateLogger("Columns.Get_All");

                var columns = await columnReadSvc.ListByLaneAsync(laneId, ct);
                var responseDto = columns.Select(c => c.ToReadDto()).ToList();

                log.LogInformation("Columns list returned projectId={ProjectId} laneId={LaneId} count={Count}",
                                    projectId, laneId, responseDto.Count);
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

                var column = await columnReadSvc.GetAsync(columnId, ct);
                if (column is null)
                {
                    log.LogInformation("Column not found projectId={ProjectId} laneId={LaneId} columnId={ColumnId}",
                                        projectId, laneId, columnId);
                    return Results.NotFound();
                }

                var responseDto = column.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                log.LogInformation("Column fetched projectId={ProjectId} laneId={LaneId} columnId={ColumnId} etag={ETag}",
                                    projectId, laneId, columnId, context.Response.Headers.ETag.ToString());
                return Results.Ok(responseDto);
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

                var (result, column) = await columnWriteSvc.CreateAsync(projectId, laneId, dto.Name, dto.Order, ct);
                if (result != DomainMutation.Created || column is null)
                {
                    log.LogInformation("Column create rejected projectId={ProjectId} laneId={LaneId} mutation={Mutation}",
                                        projectId, laneId, result);
                    return result.ToHttp(context);
                }

                var responseDto = column.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                log.LogInformation("Column created projectId={ProjectId} laneId={LaneId} columnId={ColumnId} order={Order} etag={ETag}",
                                    projectId, laneId, column.Id, responseDto.Order, context.Response.Headers.ETag.ToString());
                return Results.CreatedAtRoute("Columns_Get_ById", new { projectId, laneId, columnId = column.Id }, responseDto);
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireValidation<ColumnCreateDto>()
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

                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => columnReadSvc.GetAsync(columnId, ct), c => c.RowVersion);

                var result = await columnWriteSvc.RenameAsync(columnId, dto.NewName, rowVersion, ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation("Column rename rejected projectId={ProjectId} laneId={LaneId} columnId={ColumnId} mutation={Mutation}",
                                        projectId, laneId, columnId, result);
                    return result.ToHttp(context);
                }

                var renamed = await columnReadSvc.GetAsync(columnId, ct);
                if (renamed is null)
                {
                    log.LogInformation("Column rename readback missing projectId={ProjectId} laneId={LaneId} columnId={ColumnId}",
                                        projectId, laneId, columnId);
                    return Results.NotFound();
                }

                var responseDto = renamed.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                log.LogInformation("Column renamed projectId={ProjectId} laneId={LaneId} columnId={ColumnId} newName={NewName} etag={ETag}",
                                    projectId, laneId, columnId, dto.NewName, context.Response.Headers.ETag.ToString());
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireValidation<ColumnRenameDto>()
            .RequireIfMatch()
            .Produces<ColumnReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
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

                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => columnReadSvc.GetAsync(columnId, ct), c => c.RowVersion);

                var result = await columnWriteSvc.ReorderAsync(columnId, dto.NewOrder, rowVersion, ct);
                if (result != DomainMutation.Updated)
                {
                    log.LogInformation("Column reorder rejected projectId={ProjectId} laneId={LaneId} columnId={ColumnId} mutation={Mutation}",
                                        projectId, laneId, columnId, result);
                    return result.ToHttp(context);
                }

                var reordered = await columnReadSvc.GetAsync(columnId, ct);
                if (reordered is null)
                {
                    log.LogInformation("Column reorder readback missing projectId={ProjectId} laneId={LaneId} columnId={ColumnId}",
                                        projectId, laneId, columnId);
                    return Results.NotFound();
                }

                var responseDto = reordered.ToReadDto();
                context.Response.Headers.ETag = $"W/\"{Convert.ToBase64String(responseDto.RowVersion)}\"";

                log.LogInformation("Column reordered projectId={ProjectId} laneId={LaneId} columnId={ColumnId} newOrder={NewOrder} etag={ETag}",
                                    projectId, laneId, columnId, dto.NewOrder, context.Response.Headers.ETag.ToString());
                return Results.Ok(responseDto);
            })
            .RequireAuthorization(Policies.ProjectAdmin)
            .RequireValidation<ColumnReorderDto>()
            .RequireIfMatch()
            .Produces<ColumnReadDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status412PreconditionFailed)
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

                var rowVersion = await ConcurrencyHelpers.ResolveRowVersionAsync(
                    context, () => columnReadSvc.GetAsync(columnId, ct), c => c.RowVersion);

                var result = await columnWriteSvc.DeleteAsync(columnId, rowVersion, ct);

                log.LogInformation("Column delete result projectId={ProjectId} laneId={LaneId} columnId={ColumnId} mutation={Mutation}",
                                    projectId, laneId, columnId, result);
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
            .WithSummary("Delete column")
            .WithDescription("Admin-only. Deletes a column using optimistic concurrency (If-Match).")
            .WithName("Columns_Delete");

            return group;
        }
    }
}
