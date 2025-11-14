using Api.Auth.Authorization;
using Api.Realtime;

namespace Api.Endpoints
{
    /// <summary>
    /// Provides centralized mapping for all HTTP API endpoints and real-time hubs
    /// exposed by the Presentation layer.
    /// </summary>
    public static class EndpointMappings
    {
        /// <summary>
        /// Maps all presentation endpoints (REST API routes and SignalR hubs)
        /// to the current <see cref="WebApplication"/>.
        /// </summary>
        /// <param name="app">The <see cref="WebApplication"/> instance.</param>
        /// <returns>The same <see cref="WebApplication"/> instance for chaining.</returns>
        public static WebApplication MapEndpoints(this WebApplication app)
        {
            app.MapApiEndpoints();
            app.MapRealtimeEndpoints();

            return app;
        }

        /// <summary>
        /// Maps all API endpoint groups (Health, Auth, Users, Projects, etc.)
        /// to the current <see cref="IEndpointRouteBuilder"/>.
        /// This centralizes registration for all feature modules.
        /// </summary>
        /// <param name="endpoints">The endpoint route builder.</param>
        /// <returns>The same endpoint builder for chaining.</returns>
        private static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder endpoints)
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

        /// <summary>
        /// Maps all real-time hubs exposed by the API (SignalR).
        /// </summary>
        /// <param name="app">The <see cref="WebApplication"/> instance.</param>
        /// <returns>The same <see cref="WebApplication"/> instance for chaining.</returns>
        private static WebApplication MapRealtimeEndpoints(this WebApplication app)
        {
            app.MapHub<ProjectsHub>("/hubs/projects")
               .RequireAuthorization(Policies.ProjectReader);

            return app;
        }
    }
}
