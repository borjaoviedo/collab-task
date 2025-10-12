using Api.Endpoints;
using Api.Errors;

namespace Api.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UseApiLayer(this IApplicationBuilder app)
        {
            var env = app.ApplicationServices.GetRequiredService<IWebHostEnvironment>();

            app.UseGlobalExceptionHandling();
            app.UseCors(CorsPolicies.AllowFrontend);
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseSwaggerUiIfDev(env);

            return app;
        }

        public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapHealth();
            endpoints.MapAuth();
            endpoints.MapUsers();
            endpoints.MapProjects();
            endpoints.MapProjectMembers();
            endpoints.MapLanes();
            endpoints.MapColumns();

            return endpoints;
        }
    }
}
