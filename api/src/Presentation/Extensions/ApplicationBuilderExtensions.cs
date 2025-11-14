using Api.Configuration;
using Api.Endpoints;
using Api.ErrorHandling;

namespace Api.Extensions
{
    /// <summary>
    /// Configures the HTTP request pipeline and maps all API endpoint groups.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures the HTTP request pipeline and maps all API endpoint groups.
        /// </summary>
        /// <param name="app">The <see cref="IApplicationBuilder"/> instance used to build the middleware pipeline.</param>
        /// <returns>The same <see cref="IApplicationBuilder"/> instance for chaining.</returns>
        public static IApplicationBuilder AddApplicationBuilders(this IApplicationBuilder app)
        {
            var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

            app.UseGlobalExceptionHandling()                // Handles all unhandled exceptions and maps them to RFC 7807 ProblemDetails
               .UseCors(CorsPolicies.Default)               // Applies the configured CORS policy for cross-origin requests
               .UseAuthentication()                         // Enables identity-based request authentication
               .UseAuthorization()                          // Enforces authorization policies on secured endpoints
               .UseSwaggerUiIfDev(env);                     // Exposes Swagger UI for API exploration in development only

            if (app is WebApplication webApp)
            {
                webApp.MapEndpoints();                      // Map REST endpoints and SignalR hubs when running as a WebApplication
            }

            return app;
        }
    }
}
