namespace Api.Extensions
{
    public static class HttpResponseExtensions
    {
        public static void SetETag(this HttpResponse response, byte[] rowVersion)
        {
            if (rowVersion == null || rowVersion.Length == 0) return;

            var encoded = Convert.ToBase64String(rowVersion);
            response.Headers.ETag = $"W/\"{encoded}\"";
        }
    }
}
