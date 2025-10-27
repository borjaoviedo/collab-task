using Api.Auth.Authorization;
using Api.Realtime;

namespace Api.Extensions
{
    public static class WebApplicationExtensions
    {
        public static WebApplication MapApiLayer(this WebApplication app)
        {
            app.MapHub<ProjectsHub>("/hubs/projects").RequireAuthorization(Policies.ProjectReader);
            return app;
        }
    }
}
