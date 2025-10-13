
namespace Api.Filters
{
    public sealed class IfMatchRowVersionFilter : IEndpointFilter
    {
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var ifMatch = context.HttpContext.Request.Headers.IfMatch.ToString();

            if (!string.IsNullOrWhiteSpace(ifMatch))
            {
                if (ifMatch.Trim() == "*")
                {
                    context.HttpContext.Items["IfMatchWildcard"] = true;
                    return next(context);
                }

                var first = ifMatch.Split(',')[0].Trim();
                if (first.StartsWith("W/")) first = first[2..].Trim();
                if (first.StartsWith("\"") && first.EndsWith("\""))
                {
                    try
                    {
                        context.HttpContext.Items["rowVersion"] = Convert.FromBase64String(first.Trim('"'));
                    }
                    catch { /* ignore invalid */ }
                }
            }
            return await next(context);
        }
    }
}
