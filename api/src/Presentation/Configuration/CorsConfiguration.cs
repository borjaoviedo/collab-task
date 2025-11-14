namespace Api.Configuration
{
    /// <summary>
    /// Service collection extensions for configuring Cross-Origin Resource Sharing (CORS) policies.
    /// Loads allowed origins from configuration and registers the default frontend policy.
    /// </summary>
    public static class CorsConfiguration
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
                : CorsOptions.DefaultEmptyOrigins;

            services.AddCors(o =>
            {
                o.AddPolicy(CorsPolicies.Default, p =>
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
        public const string Default = "DefaultCorsPolicy";
    }

    /// <summary>
    /// Strongly typed configuration options for CORS settings.
    /// Defines allowed origins and default development fallback values.
    /// </summary>
    public sealed class CorsOptions
    {
        /// <summary>
        /// Name of the configuration section that stores CORS settings.
        /// </summary>
        public const string SectionName = "Cors";

        /// <summary>
        /// List of allowed origins for CORS requests.
        /// If empty, no external origins are permitted.
        /// </summary>
        public string[] AllowedOrigins { get; init; } = [];

        /// <summary>
        /// Default value representing an empty set of allowed origins.
        /// Used when no frontend is configured for EventDesk.
        /// </summary>
        public static readonly string[] DefaultEmptyOrigins = [];
    }
}
