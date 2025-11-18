using Microsoft.Extensions.Primitives;

namespace Api.Filters
{
    /// <summary>
    /// Endpoint filter that rejects requests containing an <c>If-Match</c> header.
    /// Intended for create endpoints where concurrency preconditions are meaningless.
    /// Throws a domain exception mapped to HTTP 400 (Bad Request).
    /// </summary>
    public sealed class RejectIfMatchFilter : IEndpointFilter
    {
        /// <summary>
        /// Blocks execution if the request includes an <c>If-Match</c> header.
        /// Continues the pipeline only when the header is absent.
        /// </summary>
        /// <param name="context">Current endpoint invocation context.</param>
        /// <param name="next">Next delegate in the execution pipeline.</param>
        /// <returns>The next result, or an exception if the header is present.</returns>
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var http = context.HttpContext;

            // Reject any request that includes If-Match (clients must not send ETags on POST)
            if (!StringValues.IsNullOrEmpty(http.Request.Headers.IfMatch))
                throw new ArgumentException("If-Match header is not allowed for create endpoints.");

            return await next(context);
        }
    }
}
