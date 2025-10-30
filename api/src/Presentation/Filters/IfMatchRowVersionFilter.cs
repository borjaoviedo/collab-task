using Microsoft.Extensions.Primitives;

namespace Api.Filters
{
    /// <summary>
    /// Endpoint filter that validates and parses the <c>If-Match</c> HTTP header for optimistic concurrency.
    /// Decodes base64-encoded ETags into byte[] row versions and stores them in <see cref="HttpContext.Items"/>.
    /// Returns appropriate RFC 9110 status codes (400, 412, 428) for malformed or missing headers.
    /// </summary>
    public sealed class IfMatchRowVersionFilter : IEndpointFilter
    {
        private const string RowVersionItemKey = "rowVersion";
        private const string IfMatchWildcardKey = "IfMatchWildcard";

        /// <summary>
        /// Invokes the filter to enforce <c>If-Match</c> header semantics.
        /// Parses and normalizes one or multiple ETag values, supports weak and strong validators,
        /// and propagates a decoded RowVersion or wildcard flag to the request context.
        /// </summary>
        /// <param name="context">The endpoint invocation context.</param>
        /// <param name="next">The next delegate in the filter pipeline.</param>
        /// <returns>An HTTP result or the continuation of the request pipeline.</returns>
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

        /// <summary>
        /// Determines from endpoint metadata whether <c>If-Match</c> is required for the current route.
        /// Reads <see cref="IfMatchRequirementMetadata"/> values applied via endpoint extensions.
        /// </summary>
        /// <param name="http">Current HTTP context.</param>
        /// <returns><c>true</c> if the header is mandatory; otherwise <c>false</c>.</returns>
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

        /// <summary>
        /// Removes weak validator prefix (<c>W/</c>) and surrounding quotes from an ETag value,
        /// returning the base64 token used to decode a row version.
        /// </summary>
        /// <param name="value">The raw ETag string.</param>
        /// <returns>Normalized base64 token without quotes or prefix.</returns>
        private static string NormalizeEtag(string value)
        {
            var s = value.Trim();
            if (s.StartsWith("W/", StringComparison.Ordinal)) s = s[2..].Trim();
            if (s.Length >= 2 && s[0] == '"' && s[^1] == '"') s = s[1..^1];
            return s;
        }
    }
}
