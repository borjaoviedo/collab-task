using System.Net.Http.Headers;
using TestHelpers.Api.Endpoints.Defaults;

namespace TestHelpers.Api.Common.Http
{
    public static class AuthHeaderExtensions
    {
        public static void SetAuthorization(this HttpClient client, string? parameter, string? scheme = null)
        {
            scheme ??= AuthDefaults.DefaultScheme;
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(scheme, parameter);
        }
    }
}
