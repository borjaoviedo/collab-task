using System.Net.Http.Json;

namespace Api.Tests.Common.Helpers
{
    internal static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> DeleteAsJsonAsync<T>(this HttpClient client, string url, T value)
        {
            var req = new HttpRequestMessage(HttpMethod.Delete, url)
            {
                Content = JsonContent.Create(value)
            };
            return await client.SendAsync(req);
        }
    }
}
