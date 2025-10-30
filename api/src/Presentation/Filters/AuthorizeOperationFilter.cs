using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Filters
{
    /// <summary>
    /// Swagger operation filter that adds JWT bearer authentication requirements
    /// to operations decorated with <see cref="AuthorizeAttribute"/> at method or class level.
    /// Ensures secured endpoints are documented with the proper security scheme reference.
    /// </summary>
    public sealed class AuthorizeOperationFilter : IOperationFilter
    {
        /// <summary>
        /// Applies the JWT security requirement to OpenAPI operations that require authorization.
        /// Checks for <see cref="AuthorizeAttribute"/> on the method or declaring type
        /// and appends the bearerAuth scheme if applicable.
        /// </summary>
        /// <param name="operation">The OpenAPI operation being processed.</param>
        /// <param name="context">Context providing reflection and schema information.</param>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var hasAuthorize =
                context.MethodInfo
                    .GetCustomAttributes(true)
                    .OfType<AuthorizeAttribute>()
                    .Any()
                || context.MethodInfo.DeclaringType?
                    .GetCustomAttributes(true)
                    .OfType<AuthorizeAttribute>()
                    .Any() == true;

            if (!hasAuthorize) return;

            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "bearerAuth"
                        }
                    }
                ] = []
            });
        }
    }
}
