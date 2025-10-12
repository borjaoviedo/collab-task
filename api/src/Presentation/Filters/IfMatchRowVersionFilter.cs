using Application.Common.Exceptions;

namespace Api.Filters
{
    public sealed class IfMatchRowVersionFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var req = context.HttpContext.Request;
            var ifMatch = req.Headers.IfMatch.ToString();

            if (string.IsNullOrWhiteSpace(ifMatch))
                throw new PreconditionFailedException("If-Match required");

            var tag = ifMatch.Trim();
            if (tag.StartsWith("W/")) tag = tag[2..].Trim();
            var bytes = Convert.FromBase64String(tag.Trim('"'));
            context.HttpContext.Items["rowVersion"] = bytes;
            return await next(context);
        }
    }
}
