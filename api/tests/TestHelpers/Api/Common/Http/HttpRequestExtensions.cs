using System.Net.Http.Json;

namespace TestHelpers.Api.Common.Http
{
    public static class HttpRequestExtensions
    {
        public static async Task<T> ReadContentAsDtoAsync<T>(this HttpResponseMessage response)
        {
            var result = await response.Content.ReadFromJsonAsync<T>();
            return result
                ?? throw new InvalidOperationException($"Response content could not be deserialized to {typeof(T).Name}.");
        }

        public static async Task<HttpResponseMessage> PostWithoutIfMatchAsync<T>(
            this HttpClient client,
            string url,
            T payload)
        {
            client.DefaultRequestHeaders.IfMatch.Clear();
            return await client.PostAsJsonAsync(url, payload);
        }

        public static async Task<HttpResponseMessage> PutWithIfMatchAsync<T>(
            this HttpClient client,
            string rowVersion,
            string url,
            T payload)
        {
            client.SetIfMatchFromRowVersion(rowVersion);
            return await client.PutAsJsonAsync(url, payload);
        }

        public static async Task<HttpResponseMessage> PatchWithIfMatchAsync<T>(
            this HttpClient client,
            string rowVersion,
            string url,
            T payload)
        {
            client.SetIfMatchFromRowVersion(rowVersion);
            return await client.PatchAsJsonAsync(url, payload);
        }

        public static async Task<HttpResponseMessage> DeleteWithIfMatchAsync(
            this HttpClient client,
            string rowVersion,
            string url)
        {
            client.SetIfMatchFromRowVersion(rowVersion);
            return await client.DeleteAsync(url);
        }
    }
}
