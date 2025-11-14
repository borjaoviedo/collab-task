using Infrastructure;
using Application;
using Application.Realtime;
using Api.Realtime;
using Api.Configuration;
using Api.ErrorHandling;

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
        public static IServiceCollection AddServices(
            this IServiceCollection services,
            IConfiguration config,
            string connectionString)
        {
            services
                .AddProblemDetailsAndExceptionMapping() // Configures centralized ProblemDetails responses and global exception mapping
                .AddCorsPolicies(config)                // Configures default CORS policy (no origins unless defined in config)
                .AddInfrastructure(connectionString)    // Registers EF Core, repositories, and infrastructure-level services
                .AddApplication()                       // Registers application services, mappings, and validators
                .AddSwaggerDocs()                       // Registers Swagger/OpenAPI for API documentation
                .AddSecurity(config)                    // Registers authentication, authorization, and current user services
                .AddSignalR();                          // Registers SignalR services required for real-time communication

            // Provides a singleton notifier for broadcasting real-time updates via SignalR hubs
            services.AddSingleton<IRealtimeNotifier, ProjectsHubNotifier>();

            return services;
        }
    }
}
