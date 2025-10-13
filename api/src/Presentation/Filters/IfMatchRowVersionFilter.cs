
namespace Api.Filters
{
    public sealed class IfMatchRowVersionFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var http = context.HttpContext;
            var ifMatch = http.Request.Headers.IfMatch.ToString();
            if (!string.IsNullOrWhiteSpace(ifMatch))
            {
                http.Items["IfMatchPresent"] = true;

                // parse weak ETag: W/"base64"
                var token = ifMatch.Trim().TrimStart('W').TrimStart('/').Trim('"');
                if (Convert.TryFromBase64String(token, new Span<byte>(new byte[token.Length]), out _))
                    http.Items["rowVersion"] = Convert.FromBase64String(token);
            }
            return await next(context);
        }
    }
}
