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
        /// <param name="required">Whether the header is mandatory (428/412 when missing/stale).</param>
        /// <returns>The same builder for chaining.</returns>
        public static RouteHandlerBuilder RequireIfMatch(this RouteHandlerBuilder builder, bool required = true)
        {
            builder.AddEndpointFilter<IfMatchRowVersionFilter>()
                .WithMetadata(new IfMatchRequirementMetadata(required));

            return builder.WithOpenApi(op =>
            {
                op.Parameters ??= [];
                op.Parameters.Add(new OpenApiParameter
                {
                    Name = "If-Match",
                    In = ParameterLocation.Header,
                    Required = required,
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
    }
}
