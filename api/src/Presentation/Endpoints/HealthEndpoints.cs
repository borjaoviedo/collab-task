namespace Api.Endpoints
{
    public static class HealthEndpoints
    {
        public static RouteGroupBuilder MapHealth(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/health").WithTags("Health");

            // GET /health
            group.MapGet("/", () => Results.Ok(new { status = "ok" }))
               .WithName("Health")
               .WithSummary("Health check")
               .Produces(StatusCodes.Status200OK);

            return group;
        }
    }
}
