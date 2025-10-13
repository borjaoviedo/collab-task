using Domain.Enums;

namespace Api.Extensions
{
    public static class DomainMutationExtensions
    {
        public static IResult ToHttp(this DomainMutation r, HttpContext? http = null, object? body = null, string? location = null)
        {
            // Detect If-Match → PreconditionFailed
            bool hasIfMatch = http?.Request.Headers.IfMatch.Count > 0;

            return r switch
            {
                DomainMutation.Created => location is not null
                    ? Results.Created(location, body ?? new { status = "created" })
                    : Results.Created(string.Empty, body ?? new { status = "created" }),

                DomainMutation.Updated => Results.NoContent(),
                DomainMutation.Deleted => Results.NoContent(),
                DomainMutation.NoOp => Results.Ok(body ?? new { status = "no-op" }),
                DomainMutation.NotFound => Results.NotFound(body ?? new { error = "not-found" }),

                DomainMutation.Conflict when hasIfMatch
                    => Results.StatusCode(StatusCodes.Status412PreconditionFailed),

                DomainMutation.Conflict
                    => Results.Conflict(body ?? new { error = "conflict" }),

                _ => Results.Problem(statusCode: 500, title: "unknown result")
            };
        }
    }
}
