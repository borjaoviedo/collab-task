using Domain.Enums;

namespace Api.Extensions
{
    public static class DomainMutationExtensions
    {
        public static IResult ToHttp(this DomainMutation result, HttpContext? context = null, object? body = null, string? location = null)
        {
            // Detect If-Match -> PreconditionFailed
            bool hasIfMatch = context?.Request.Headers.IfMatch.Count > 0;

            return result switch
            {
                DomainMutation.Created => location is not null
                    ? Results.Created(location, body ?? new { status = "created" })
                    : Results.StatusCode(StatusCodes.Status201Created),

                DomainMutation.Updated => Results.NoContent(),
                DomainMutation.Deleted => Results.NoContent(),

                DomainMutation.NoOp => Results.Ok(body ?? new { status = "no-op" }),
                DomainMutation.NotFound => Results.NotFound(body ?? new { error = "not-found" }),

                DomainMutation.Conflict when hasIfMatch
                    => Results.Problem(
                        title: "Precondition Failed",
                        detail: "ETag mismatch. The resource has been modified.",
                        statusCode: StatusCodes.Status412PreconditionFailed,
                        instance: context?.Request.Path.Value,
                        extensions: context is null ? null : new Dictionary<string, object?>
                        {
                            ["traceId"] = context.TraceIdentifier
                        }),

                DomainMutation.Conflict
                    => Results.Problem(
                        title: "Conflict",
                        detail: "A conflict prevented the operation from succeeding.",
                        statusCode: StatusCodes.Status409Conflict,
                        instance: context?.Request.Path.Value,
                        extensions: context is null ? null : new Dictionary<string, object?>
                        {
                            ["traceId"] = context.TraceIdentifier
                        }),

                _ => Results.Problem(
                        title: "Unknown result",
                        statusCode: StatusCodes.Status500InternalServerError,
                        instance: context?.Request.Path.Value,
                        extensions: context is null ? null : new Dictionary<string, object?>
                        {
                            ["traceId"] = context.TraceIdentifier
                        })
            };
        }
    }
}
