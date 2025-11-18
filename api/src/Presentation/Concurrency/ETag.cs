using System.Net.Http.Headers;

namespace Api.Concurrency
{
    /// <summary>
    /// Utility class for generating weak ETags from database <c>RowVersion</c> values.
    /// Used for conditional requests and optimistic concurrency handling.
    /// </summary>
    public static class ETag
    {
        /// <summary>
        /// Wraps a Base64-encoded <c>RowVersion</c> string into a weak ETag (e.g., <c>W/"base64"</c>).
        /// </summary>
        /// <param name="rowVersionBase64">The Base64 string representing the concurrency token.</param>
        /// <returns>A weak ETag string suitable for HTTP headers.</returns>
        public static string EncodeWeak(string rowVersionBase64)
        {
            if (string.IsNullOrWhiteSpace(rowVersionBase64))
                return string.Empty;

            var quoted = $"\"{rowVersionBase64}\"";
            var etag = new EntityTagHeaderValue(quoted, isWeak: true);
            return etag.ToString();
        }
    }
}
