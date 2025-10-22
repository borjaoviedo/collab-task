using Microsoft.Extensions.Primitives;

namespace Api.Filters
{
    public sealed class IfMatchRowVersionFilter : IEndpointFilter
    {
        private const string RowVersionItemKey = "rowVersion";
        private const string IfMatchWildcardKey = "IfMatchWildcard";

        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var http = context.HttpContext;

            var required = IsIfMatchRequired(http);

            StringValues ifMatch = http.Request.Headers.IfMatch;

            if (StringValues.IsNullOrEmpty(ifMatch))
            {
                if (required) return Results.StatusCode(StatusCodes.Status428PreconditionRequired);
                return await next(context);
            }

            var all = string.Join(",", ifMatch.ToArray())
                             .Split(',')
                             .Select(s => s.Trim())
                             .Where(s => !string.IsNullOrEmpty(s))
                             .ToList();

            if (all.Count == 0)
                return Results.StatusCode(StatusCodes.Status400BadRequest);

            if (all.Contains("*"))
            {
                http.Items[IfMatchWildcardKey] = true;
                return await next(context);
            }

            foreach (var raw in all)
            {
                var token = NormalizeEtag(raw);
                if (string.IsNullOrWhiteSpace(token)) continue;

                try
                {
                    var rv = Convert.FromBase64String(token);
                    if (rv.Length > 0)
                    {
                        http.Items[RowVersionItemKey] = rv;
                        return await next(context);
                    }
                }
                catch { /* probar siguiente candidato */ }
            }

            return Results.StatusCode(StatusCodes.Status400BadRequest);
        }

        private static bool IsIfMatchRequired(HttpContext http)
        {
            var metadata = http.GetEndpoint()?.Metadata;
            if (metadata is null) return false;

            foreach (var m in metadata)
            {
                var t = m?.GetType();
                if (t is null) continue;
                if (t.Name == "IfMatchRequirementMetadata")
                {
                    var prop = t.GetProperty("Required");
                    if (prop is null) return false;
                    var val = prop.GetValue(m);
                    if (val is bool b) return b;
                    return false;
                }
            }
            return false;
        }

        private static string NormalizeEtag(string value)
        {
            var s = value.Trim();
            if (s.StartsWith("W/", StringComparison.Ordinal)) s = s[2..].Trim();
            if (s.Length >= 2 && s[0] == '"' && s[^1] == '"') s = s[1..^1];
            return s;
        }
    }
}
