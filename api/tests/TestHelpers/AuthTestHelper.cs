using Application.Users.DTOs;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TestHelpers
{
    public static class AuthTestHelper
    {
        public sealed record AuthToken(string AccessToken, Guid UserId, string Email, string Name, string Role);

        public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

        public static async Task<AuthToken> RegisterAndLoginAsync(HttpClient client, string? email = null, string name = "User Name", string password = "Str0ngP@ss!")
        {
            // ensure anonymous for register/login
            client.DefaultRequestHeaders.Authorization = null;

            email ??= $"{Guid.NewGuid():N}@demo.com";

            var register = await client.PostAsJsonAsync("/auth/register", new UserRegisterDto { Email = email, Name = name, Password = password });
            register.EnsureSuccessStatusCode();

            var login = await client.PostAsJsonAsync("/auth/login", new { Email = email, Password = password });
            login.EnsureSuccessStatusCode();

            var token = await login.Content.ReadFromJsonAsync<AuthToken>(Json);
            return token!;
        }
        public static void UseBearer(this HttpClient client, string accessToken)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }
}
