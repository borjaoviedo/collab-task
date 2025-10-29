using System.Net.Http.Json;

namespace TestHelpers.Api.Http
{
    public static class HttpRequestExtensions
    {
        public static async Task<HttpResponseMessage> PostWithoutIfMatchAsync<T>(
            HttpClient client,
            string url,
            T payload)
        {
            client.DefaultRequestHeaders.IfMatch.Clear();
            return await client.PostAsJsonAsync(url, payload);
        }

        public static async Task<HttpResponseMessage> PutWithIfMatchAsync<T>(
            HttpClient client,
            byte[] rowVersion,
            string url,
            T payload)
        {
            IfMatchExtensions.SetIfMatchFromRowVersion(client, rowVersion);
            return await client.PutAsJsonAsync(url, payload);
        }

        public static async Task<HttpResponseMessage> PatchWithIfMatchAsync<T>(
            HttpClient client,
            byte[] rowVersion,
            string url,
            T payload)
        {
            IfMatchExtensions.SetIfMatchFromRowVersion(client, rowVersion);
            return await client.PatchAsJsonAsync(url, payload);
        }

        public static async Task<HttpResponseMessage> DeleteWithIfMatchAsync(
            HttpClient client,
            byte[] rowVersion,
            string url)
        {
            IfMatchExtensions.SetIfMatchFromRowVersion(client, rowVersion);
            return await client.DeleteAsync(url);
        }
    }
}
