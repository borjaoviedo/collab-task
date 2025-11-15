using Api.Filters;
using Microsoft.OpenApi.Models;

namespace Api.Concurrency
{
    /// <summary>
    /// Extensions to declare and document optimistic-concurrency preconditions on endpoints.
    /// Adds endpoint filters to enforce or reject <c>If-Match</c> and augments OpenAPI with the
    /// <c>If-Match</c> header parameter (weak/strong ETag) so clients can implement safe writes.
    /// </summary>
    public static class IfMatchEndpointExtensions
    {
        /// <summary>
        /// Metadata indicating whether an endpoint requires the <c>If-Match</c> header.
        /// Used by filters and OpenAPI enrichment.
        /// </summary>
        public sealed record IfMatchRequirementMetadata(bool Required);

        /// <summary>
        /// Marks the endpoint as requiring (or optionally accepting) an <c>If-Match</c> header,
        /// wires the concurrency filter, and documents the header in the OpenAPI spec.
        /// </summary>
        /// <param name="builder">The route handler builder to decorate.</param>
        /// <returns>The same builder for chaining.</returns>
        public static RouteHandlerBuilder RequireIfMatch(this RouteHandlerBuilder builder)
        {
            builder.AddEndpointFilter<IfMatchRowVersionFilter>()
                .WithMetadata(new IfMatchRequirementMetadata(Required: true));

            return builder.WithOpenApi(op =>
            {
                op.Parameters ??= [];
                op.Parameters.Add(new OpenApiParameter
                {
                    Name = "If-Match",
                    In = ParameterLocation.Header,
                    Required = true,
                    Schema = new OpenApiSchema { Type = "string" },
                    Description = "ETag from previous GET. Accepts multiple values and weak/strong: W/\"base64\" or \"base64\"."
                });
                return op;
            });
        }

        /// <summary>
        /// Explicitly rejects requests that include an <c>If-Match</c> header (e.g., for create endpoints),
        /// preventing meaningless or misleading preconditions.
        /// </summary>
        /// <param name="builder">The route handler builder to decorate.</param>
        /// <returns>The same builder for chaining.</returns>
        public static RouteHandlerBuilder RejectIfMatch(this RouteHandlerBuilder builder)
            => builder.AddEndpointFilter<RejectIfMatchFilter>();

        /// <summary>
        /// Enforces <c>If-Match</c> by loading the current DTO and comparing its Base64 <c>RowVersion</c>.
        /// Assumes route key <c>"id"</c> and a read service with <c>GetByIdAsync(Guid, CancellationToken)</c>.
        /// </summary>
        /// <typeparam name="TReadService">Service exposing <c>GetByIdAsync(Guid, CancellationToken)</c>.</typeparam>
        /// <typeparam name="TDto">DTO exposing <c>string RowVersion</c>.</typeparam>
        /// <param name="builder">Route handler to decorate.</param>
        /// <param name="routeValueKey">Route key used to resolve the identifier.</param>
        /// <returns>The same builder.</returns>
        public static RouteHandlerBuilder EnsureIfMatch<TReadService, TDto>(
            this RouteHandlerBuilder builder,
            string routeValueKey)
            where TReadService : class
            where TDto : class
            => builder.AddEndpointFilter(new EnsureIfMatchFilter<TReadService, TDto>(routeValueKey));

        /// <summary>
        /// Enforces If-Match for the authenticated principal on <c>/me</c> routes.
        /// </summary>
        /// <typeparam name="TReadService">Service exposing <c>GetCurrentAsync(CancellationToken)</c>.</typeparam>
        /// <typeparam name="TDto">DTO exposing public <c>string RowVersion</c>.</typeparam>
        /// <param name="builder">Route handler to decorate.</param>
        /// <returns>The same builder.</returns>
        public static RouteHandlerBuilder EnsureIfMatchSelf<TReadService, TDto>(
            this RouteHandlerBuilder builder)
            where TReadService : class
            where TDto : class
            => builder.AddEndpointFilter(new EnsureIfMatchFilter<TReadService, TDto>(routeValueKey: "$me"));
    }
}
