using Api.Auth.Authorization;
using Api.Realtime;

namespace Api.Extensions
{
    /// <summary>
    /// Web application extensions for mapping real-time communication endpoints.
    /// Registers SignalR hubs and applies appropriate authorization policies.
    /// </summary>
    public static class WebApplicationExtensions
    {
        /// <summary>
        /// Maps the API layer’s real-time hubs into the endpoint pipeline.
        /// Currently registers the <see cref="ProjectsHub"/> under <c>/hubs/projects</c>
        /// with project-level read authorization.
        /// </summary>
        /// <param name="app">The web application instance.</param>
        /// <returns>The same <see cref="WebApplication"/> for chaining.</returns>
        public static WebApplication MapApiLayer(this WebApplication app)
        {
            app.MapHub<ProjectsHub>("/hubs/projects")
               .RequireAuthorization(Policies.ProjectReader);
            return app;
        }
    }
}
