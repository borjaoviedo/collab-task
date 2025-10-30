
namespace Api.Helpers
{
    /// <summary>
    /// Wraps an existing <see cref="IResult"/> and adds a custom HTTP response header before executing it.
    /// Used to attach metadata such as ETags or custom tracing headers to outgoing responses.
    /// </summary>
    public sealed class HeaderResult(IResult inner, string name, string value) : IResult
    {
        private readonly IResult _inner = inner;
        private readonly string _name = name;
        private readonly string _value = value;

        /// <summary>
        /// Executes the wrapped result after injecting the specified header into the HTTP response.
        /// </summary>
        /// <param name="httpContext">The current HTTP context.</param>
        /// <returns>A task that completes when the inner result has executed.</returns>
        public async Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.Headers[_name] = _value;
            await _inner.ExecuteAsync(httpContext);
        }
    }

    /// <summary>
    /// Result extensions for attaching ETags to responses.
    /// Wraps the given <see cref="IResult"/> in a <see cref="HeaderResult"/> if a non-empty ETag is provided.
    /// </summary>
    public static class ETagResults
    {
        /// <summary>
        /// Adds an <c>ETag</c> header to the response if the provided value is not null or empty.
        /// Returns the original result unchanged otherwise.
        /// </summary>
        /// <param name="result">The inner HTTP result to decorate.</param>
        /// <param name="etag">The ETag header value to include.</param>
        /// <returns>The decorated or original result.</returns>
        public static IResult WithETag(this IResult result, string etag)
            => string.IsNullOrEmpty(etag) ? result : new HeaderResult(result, "ETag", etag);
    }
}
