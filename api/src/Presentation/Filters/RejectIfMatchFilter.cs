using Microsoft.Extensions.Primitives;

namespace Api.Filters
{
    /// <summary>
    /// Endpoint filter that rejects requests containing an <c>If-Match</c> header.
    /// Used for create endpoints where preconditions and concurrency tokens are invalid.
    /// Returns HTTP 400 (Bad Request) if the header is present.
    /// </summary>
    public sealed class RejectIfMatchFilter : IEndpointFilter
    {
        /// <summary>
        /// Executes the filter, blocking requests that include an <c>If-Match</c> header.
        /// Continues the pipeline only if the header is absent.
        /// </summary>
        /// <param name="context">The endpoint invocation context.</param>
        /// <param name="next">The next delegate in the filter pipeline.</param>
        /// <returns>An HTTP 400 result or the next delegate’s result.</returns>
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            StringValues ifMatch = context.HttpContext.Request.Headers.IfMatch;

            if (!StringValues.IsNullOrEmpty(ifMatch))
                return Results.StatusCode(StatusCodes.Status400BadRequest); // client misuse on create

            return await next(context);
        }
    }
}
