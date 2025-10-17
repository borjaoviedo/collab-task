using Api.Realtime;

namespace Api.Extensions
{
    public static class WebApplicationExtensions
    {
        public static WebApplication MapApiLayer(this WebApplication app)
        {
            app.MapHub<BoardHub>("/hubs/board").RequireAuthorization();
            return app;
        }
    }
}
