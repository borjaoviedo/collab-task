using System.Net.Http.Headers;

namespace Api.Helpers
{
    public static class ETag
    {
        public static string EncodeWeak(byte[] rowVersion)
        {
            if (rowVersion is null || rowVersion.Length == 0) return string.Empty;

            var quoted = $"\"{Convert.ToBase64String(rowVersion)}\"";
            var etag = new EntityTagHeaderValue(quoted, isWeak: true);

            return etag.ToString();
        }
    }
}
