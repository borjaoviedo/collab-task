using Api.Errors;
using Infrastructure;
using Application;
using Application.Realtime;
using Api.Realtime;

namespace Api.Extensions
{
    /// <summary>
    /// Service collection extensions for registering all dependencies of the API layer.
    /// Composes infrastructure, authentication, validation, exception mapping, Swagger, and realtime services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures and registers the complete API layer service stack.
        /// Adds CORS, infrastructure, Swagger with JWT, authentication/authorization,
        /// validation, problem details, SignalR, and realtime notifier services.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="config">Application configuration.</param>
        /// <param name="connectionString">Database connection string.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddApiLayer(
            this IServiceCollection services,
            IConfiguration config,
            string connectionString)
        {
            services
                .AddProblemDetailsAndExceptionMapping()
                .AddCorsPolicies(config)
                .AddInfrastructure(connectionString)
                .AddApplication()
                .AddAppValidation()
                .AddSwaggerWithJwt()
                .AddJwtAuthAndPolicies(config)
                .AddSignalR();

            services.AddSingleton<IRealtimeNotifier, ProjectsHubNotifier>();

            return services;
        }
    }
}
