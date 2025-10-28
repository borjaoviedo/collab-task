using System.Net.Http.Json;
using TestHelpers.Api.Serialization;

namespace TestHelpers.Api.Http
{
    public static class HttpContentExtensions
    {
        public static async Task<T> ReadContentAsDtoAsync<T>(this HttpResponseMessage response)
        {
            var result = await response.Content.ReadFromJsonAsync<T>(JsonDefaults.Json);
            return result ?? throw new InvalidOperationException($"Response content could not be deserialized to {typeof(T).Name}.");
        }
    }
}
