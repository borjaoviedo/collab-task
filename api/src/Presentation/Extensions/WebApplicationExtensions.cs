using Api.Realtime;

namespace Api.Extensions
{
    public static class WebApplicationExtensions
    {
        public static WebApplication MapApiLayer(this WebApplication app)
        {
            app.MapHub<ProjectsHub>("/hubs/board").RequireAuthorization();
            return app;
        }
    }
}
