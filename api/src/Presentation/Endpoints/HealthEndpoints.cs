namespace Api.Endpoints
{
    public static class HealthEndpoints
    {
        public static RouteGroupBuilder MapHealth(this IEndpointRouteBuilder app)
        {
            var group = app
                        .MapGroup("/health")
                        .WithTags("Health")
                        .AllowAnonymous();

            // GET /health
            group.MapGet("/", (HttpContext context) =>
            {
                var status = new
                {
                    Status = "Healthy",
                    Uptime = GetUptime(),
                    ServerTimeUtc = DateTimeOffset.UtcNow
                };

                return Results.Ok(status);
            })
            .WithName("Health_Get")
            .WithSummary("Health check")
            .WithDescription("Verifies API availability and basic server uptime.")
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

            return group;
        }

        private static string GetUptime()
        {
            var uptime = DateTimeOffset.UtcNow - System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime();
            return uptime.ToString(@"d\.hh\:mm\:ss");
        }
    }
}
