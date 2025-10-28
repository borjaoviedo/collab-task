using System.Net.Http.Json;

namespace TestHelpers.Api.Http
{
    public static class HttpRequestExtensions
    {
        public static async Task<HttpResponseMessage> PostWithoutIfMatchAsync<T>(HttpClient client, string url, T payload)
        {
            client.DefaultRequestHeaders.IfMatch.Clear();
            return await client.PostAsJsonAsync(url, payload);
        }
    }
}
