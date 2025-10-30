
namespace Api.Extensions
{
    /// <summary>
    /// Service collection extensions for configuring Cross-Origin Resource Sharing (CORS) policies.
    /// Loads allowed origins from configuration and registers the default frontend policy.
    /// </summary>
    public static class CorsExtensions
    {
        /// <summary>
        /// Adds CORS policies based on configuration or default development origins.
        /// Enables credentials, all headers, and all methods for the allowed origins.
        /// </summary>
        /// <param name="services">The service collection to configure.</param>
        /// <param name="config">Application configuration containing the CORS section.</param>
        /// <returns>The same service collection for chaining.</returns>
        public static IServiceCollection AddCorsPolicies(this IServiceCollection services, IConfiguration config)
        {
            var options = config.GetSection(CorsOptions.SectionName).Get<CorsOptions>() ?? new CorsOptions();

            var origins = options.AllowedOrigins.Length > 0
                ? options.AllowedOrigins
                : CorsOptions.DefaultDevOrigins;

            services.AddCors(o =>
            {
                o.AddPolicy(CorsPolicies.AllowFrontend, p =>
                    p.WithOrigins(origins)
                     .AllowAnyHeader()
                     .AllowAnyMethod()
                     .AllowCredentials());
            });

            return services;
        }
    }

    /// <summary>
    /// Centralized names for CORS policies used across the application.
    /// </summary>
    public static class CorsPolicies
    {
        public const string AllowFrontend = "AllowFrontend";
    }

    /// <summary>
    /// Strongly typed configuration options for CORS settings.
    /// Defines allowed origins and default development fallback values.
    /// </summary>
    public sealed class CorsOptions
    {
        public const string SectionName = "Cors";
        public string[] AllowedOrigins { get; init; } = [];

        public static readonly string[] DefaultDevOrigins =
        [
            "http://localhost:8081"
        ];
    }
}
