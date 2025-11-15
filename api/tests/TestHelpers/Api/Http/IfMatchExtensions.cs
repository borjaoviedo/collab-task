
namespace TestHelpers.Api.Http
{
    public static class IfMatchExtensions
    {
        public static void SetIfMatchFromRowVersion(HttpClient client, byte[] rowVersion)
        {
            if (rowVersion is null || rowVersion.Length == 0)
                throw new ArgumentException("RowVersion is null or empty.", nameof(rowVersion));

            client.DefaultRequestHeaders.IfMatch.Clear();
            var etag = $"W/\"{Convert.ToBase64String(rowVersion)}\"";
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", etag);
        }
    }
}
