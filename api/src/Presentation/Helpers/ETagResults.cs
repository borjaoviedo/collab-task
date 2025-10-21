
namespace Api.Helpers
{
    public sealed class HeaderResult(IResult inner, string name, string value) : IResult
    {
        private readonly IResult _inner = inner;
        private readonly string _name = name;
        private readonly string _value = value;

        public async Task ExecuteAsync(HttpContext httpContext)
        {
            httpContext.Response.Headers[_name] = _value;
            await _inner.ExecuteAsync(httpContext);
        }
    }

    public static class ETagResults
    {
        public static IResult WithETag(this IResult result, string etag)
            => string.IsNullOrEmpty(etag) ? result : new HeaderResult(result, "ETag", etag);
    }
}
