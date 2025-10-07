using Application.Common.Results;

namespace Api.Common
{
    public static class WriteResultHttp
    {
        public static IResult ToHttp(this WriteResult r, object? body = null, string? location = null) => r switch
        {
            WriteResult.Created => location is not null
                ? Results.Created(location, body ?? new { status = "created" })
                : Results.Created(string.Empty, body ?? new { status = "created" }),
            WriteResult.Updated => Results.NoContent(),
            WriteResult.Deleted => Results.NoContent(),
            WriteResult.NoOp => Results.Ok(body ?? new { status = "no-op" }),
            WriteResult.NotFound => Results.NotFound(body ?? new { error = "not-found" }),
            WriteResult.Conflict => Results.Conflict(body ?? new { error = "conflict" }),
            _ => Results.Problem(statusCode: 500, title: "unknown result")
        };
    }
}
