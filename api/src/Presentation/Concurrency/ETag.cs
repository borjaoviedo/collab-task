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
        /// Encodes a <c>RowVersion</c> byte array into a weak ETag string (e.g., <c>W/"base64"</c>).
        /// Returns an empty string if the input is null or empty.
        /// </summary>
        /// <param name="rowVersion">The concurrency token to encode.</param>
        /// <returns>A weak ETag string suitable for HTTP headers.</returns>
        public static string EncodeWeak(byte[] rowVersion)
        {
            if (rowVersion is null || rowVersion.Length == 0) return string.Empty;

            var quoted = $"\"{Convert.ToBase64String(rowVersion)}\"";
            var etag = new EntityTagHeaderValue(quoted, isWeak: true);

            return etag.ToString();
        }
    }
}
