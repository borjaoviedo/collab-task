using Api.Filters;
using Microsoft.OpenApi.Models;

namespace Api.Extensions
{
    /// <summary>
    /// Service collection and application builder extensions for Swagger (OpenAPI) configuration.
    /// Adds JWT-based security definition, authorization filters, and conditional Swagger UI setup for development.
    /// </summary>
    public static class SwaggerExtensions
    {
        /// <summary>
        /// Registers Swagger/OpenAPI generation with JWT bearer authentication support.
        /// Adds security definitions, authorization requirements, and operation filters for secured endpoints.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "CollabTask API", Version = "v1" });

                const string SchemeId = "bearerAuth";
                c.AddSecurityDefinition(SchemeId, new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "JWT in Authorization header"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = SchemeId
                            }
                        },
                        Array.Empty<string>() }
                });

                c.OperationFilter<AuthorizeOperationFilter>();
            });
            return services;
        }

        /// <summary>
        /// Enables Swagger UI only in development environments.
        /// Configures the UI endpoint and route prefix for interactive API exploration.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The current hosting environment.</param>
        /// <returns>The same application builder for chaining.</returns>
        public static IApplicationBuilder UseSwaggerUiIfDev(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (!env.IsDevelopment()) return app;

            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "CollabTask API v1");
                c.RoutePrefix = "swagger"; // => /swagger/index.html
            });

            return app;
        }
    }
}
