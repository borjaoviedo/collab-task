using Api.Concurrency;
using Api.ErrorHandling;
using Application.Common.Exceptions;
using Microsoft.Extensions.Primitives;

namespace Api.Filters
{
    /// <summary>
    /// Endpoint filter that enforces optimistic concurrency for <c>If-Match</c>.
    /// Validates and normalizes one or more ETags (weak/strong), ensures they are valid Base64,
    /// and stores the cleaned Base64 token in <see cref="HttpContext.Items"/> under
    /// <see cref="ConcurrencyContextKeys.RowVersionBase64"/>. Also flags <c>*</c> as wildcard.
    /// Throws domain exceptions for RFC 9110 outcomes (400, 412, 428) to be mapped by global middleware.
    /// </summary>
    public sealed class IfMatchRowVersionFilter : IEndpointFilter
    {
        /// <summary>
        /// Parses and validates the <c>If-Match</c> header, supports multiple values and weak validators,
        /// and propagates either a Base64 RowVersion token or a wildcard flag to the request context.
        /// </summary>
        public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
        {
            var http = context.HttpContext;

            var required = IsIfMatchRequired(http);
            StringValues ifMatch = http.Request.Headers.IfMatch;

            // Header missing
            if (StringValues.IsNullOrEmpty(ifMatch))
            {
                if (required)
                    throw new PreconditionRequiredException("If-Match header is required.");
                return await next(context);
            }

            // Flatten and sanitize multiple values: split by comma, trim, remove empties
            var all = string.Join(",", ifMatch.ToArray())
                            .Split(',')
                            .Select(s => s.Trim())
                            .Where(s => !string.IsNullOrEmpty(s))
                            .ToList();

            if (all.Count == 0)
                throw new ArgumentException("Malformed If-Match header.");

            // Wildcard means “accept any current version”
            if (all.Contains("*"))
            {
                http.Items[ConcurrencyContextKeys.IfMatchWildcard] = true;
                return await next(context);
            }

            // Try each candidate until one decodes to valid Base64 that is non-empty
            foreach (var raw in all)
            {
                var token = NormalizeEtag(raw);
                if (string.IsNullOrWhiteSpace(token)) continue;

                try
                {
                    // Validate it's Base64; we still store the normalized Base64 string
                    var bytes = Convert.FromBase64String(token);
                    if (bytes.Length > 0)
                    {
                        http.Items[ConcurrencyContextKeys.RowVersionBase64] = token; // normalized Base64
                        return await next(context);
                    }
                }
                catch
                {
                    // Decoding failed; try next ETag candidate
                }
            }

            // No valid candidate found
            throw new OptimisticConcurrencyException(
                "Precondition failed: If-Match header does not contain a valid row version token.");
        }

        /// <summary>
        /// Checks endpoint metadata to determine whether <c>If-Match</c> is required.
        /// </summary>
        private static bool IsIfMatchRequired(HttpContext http)
        {
            var metadata = http.GetEndpoint()?.Metadata;
            if (metadata is null) return false;

            foreach (var m in metadata)
            {
                var t = m?.GetType();
                if (t is null) continue;
                if (t.Name == nameof(IfMatchEndpointExtensions.IfMatchRequirementMetadata))
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
        /// Removes weak prefix (<c>W/</c>) and surrounding quotes, returning the raw Base64 token.
        /// </summary>
        private static string NormalizeEtag(string value)
        {
            var s = value.Trim();
            if (s.StartsWith("W/", StringComparison.Ordinal)) s = s[2..].Trim();
            if (s.Length >= 2 && s[0] == '"' && s[^1] == '"') s = s[1..^1];

            return s;
        }
    }
}
