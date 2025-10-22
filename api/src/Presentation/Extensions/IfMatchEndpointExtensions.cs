using Api.Filters;
using Microsoft.OpenApi.Models;

namespace Api.Extensions
{
    public static class IfMatchEndpointExtensions
    {
        public sealed record IfMatchRequirementMetadata(bool Required);
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
        public static RouteHandlerBuilder RejectIfMatch(this RouteHandlerBuilder builder)
            => builder.AddEndpointFilter<RejectIfMatchFilter>();
    }
}
