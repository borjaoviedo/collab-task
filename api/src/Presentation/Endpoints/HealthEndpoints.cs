namespace Api.Endpoints
{
    /// <summary>
    /// Provides a basic health check endpoint for service availability monitoring.
    /// Does not require authentication. Used by load balancers and uptime probes.
    /// </summary>
    public static class HealthEndpoints
    {
        /// <summary>
        /// Registers the /health route group and exposes a GET endpoint that reports uptime and server time.
        /// </summary>
        /// <param name="app">Endpoint route builder.</param>
        /// <returns>The configured route group.</returns>
        public static RouteGroupBuilder MapHealth(this IEndpointRouteBuilder app)
        {
            // Health group is public (no auth) and tagged for OpenAPI grouping
            var group = app
                        .MapGroup("/health")
                        .WithTags("Health")
                        .AllowAnonymous();

            // OpenAPI metadata in the endpoint: ensures generated clients and API docs
            // include consistent success/error shapes and operational visibility

            // GET /health
            group.MapGet("/", (HttpContext context) =>
            {
                // Returns service status and uptime in days.hours:minutes:seconds format
                // Uptime derived from current process lifetime, useful for debugging restarts
                var status = new
                {
                    Status = "Healthy",
                    Uptime = GetUptime(),
                    ServerTimeUtc = DateTimeOffset.UtcNow
                };

                return Results.Ok(status);
            })
            .Produces(StatusCodes.Status200OK)
            .WithName("Health_Get")
            .WithSummary("Health check")
            .WithDescription("Verifies API availability and basic server uptime.");

            return group;
        }

        /// <summary>
        /// Calculates the elapsed time since the current process started, formatted as d.hh:mm:ss.
        /// </summary>
        private static string GetUptime()
        {
            var uptime = DateTimeOffset.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
            return uptime.ToString(@"d\.hh\:mm\:ss");
        }
    }
}
