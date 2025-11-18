namespace TestHelpers.Api.Common.Http
{
    public static class IfMatchExtensions
    {
        public static void SetIfMatchFromRowVersion(this HttpClient client, string rowVersion)
        {
            if (string.IsNullOrEmpty(rowVersion))
                throw new ArgumentException("RowVersion is null or empty.", nameof(rowVersion));

            client.DefaultRequestHeaders.IfMatch.Clear();
            var etag = $"W/\"{rowVersion}\"";
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag);
        }
    }
}
