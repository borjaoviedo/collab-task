using Api.Endpoints;
using Api.Errors;

namespace Api.Extensions
{
    /// <summary>
    /// Application builder extensions for configuring the API layer pipeline and endpoint registration.
    /// Centralizes middleware setup (CORS, auth, Swagger, exception handling) and endpoint mapping
    /// for all route groups in the presentation layer.
    /// </summary>
    public static class ApplicationBuilderExtensions
    {
        /// <summary>
        /// Configures the HTTP request pipeline for the API layer.
        /// Registers global exception handling, CORS, authentication, authorization,
        /// and developer-only Swagger UI. Returns the configured <see cref="IApplicationBuilder"/>.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The same application builder for chaining.</returns>
        public static IApplicationBuilder UseApiLayer(this IApplicationBuilder app)
        {
            var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

            // Global exception handler / ProblemDetails mapping
            app.UseGlobalExceptionHandling();

            // CORS must precede auth and endpoint execution
            app.UseCors(CorsPolicies.AllowFrontend);

            // Identity & access checks
            app.UseAuthentication();
            app.UseAuthorization();

            // API explorer only for dev
            app.UseSwaggerUiIfDev(env);

            return app;
        }

        /// <summary>
        /// Maps all API endpoint groups (Health, Auth, Users, Projects, etc.)
        /// to the current <see cref="IEndpointRouteBuilder"/>.
        /// This centralizes registration for all feature modules.
        /// </summary>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <returns>The same endpoint builder for chaining.</returns>
        public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHealth();
            endpoints.MapAuth();
            endpoints.MapUsers();
            endpoints.MapProjects();
            endpoints.MapProjectMembers();
            endpoints.MapLanes();
            endpoints.MapColumns();
            endpoints.MapTaskItems();
            endpoints.MapTaskNotes();
            endpoints.MapTaskAssignments();
            endpoints.MapTaskActivities();

            return endpoints;
        }
    }
}
