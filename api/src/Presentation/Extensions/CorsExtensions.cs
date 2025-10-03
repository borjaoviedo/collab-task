
namespace Api.Extensions
{
    public static class CorsExtensions
    {
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

    public static class CorsPolicies
    {
        public const string AllowFrontend = "AllowFrontend";
    }

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
