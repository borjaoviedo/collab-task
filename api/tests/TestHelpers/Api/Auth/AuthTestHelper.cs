using Api.Auth.DTOs;
using Application.Users.DTOs;
using System.Net.Http.Json;
using TestHelpers.Api.Defaults;
using TestHelpers.Api.Http;

namespace TestHelpers.Api.Auth
{
    public static class AuthTestHelper
    {
        public static async Task<HttpResponseMessage> PostRegisterResponseAsync(HttpClient client, UserRegisterDto? userRegisterDto = null)
        {
            // ensure anonymous for register
            client.SetAuthorization(null);

            userRegisterDto ??= UserDefaults.DefaultUserRegisterDto;
            var registerResponse = await client.PostAsJsonAsync("/auth/register", userRegisterDto);

            return registerResponse;
        }

        public static async Task<HttpResponseMessage> PostLoginResponseAsync(HttpClient client, UserLoginDto? userLoginDto = null)
        {
            // ensure anonymous for login
            client.SetAuthorization(null);

            userLoginDto ??= UserDefaults.DefaultUserLoginDto;
            var loginResponse = await client.PostAsJsonAsync("/auth/login", userLoginDto);

            return loginResponse;
        }

        public static async Task<HttpResponseMessage> GetMeResponseAsync(HttpClient client)
        {
            var response = await client.GetAsync($"/auth/me");
            return response;
        }

        public static async Task<AuthTokenReadDto> PostRegisterAndLoginAsync(
            HttpClient client,
            string? email = null,
            string? name = null,
            string? password = null)
        {
            email ??= UserDefaults.DefaultEmail;
            name ??= UserDefaults.DefaultUserName;
            password ??= UserDefaults.DefaultPassword;

            var userRegisterDto = new UserRegisterDto { Email = email, Name = name, Password = password };
            await PostRegisterResponseAsync(client, userRegisterDto);

            var userLoginDto = new UserLoginDto { Email = userRegisterDto.Email, Password = password };
            var login = await PostLoginResponseAsync(client, userLoginDto);

            var token = await login.ReadContentAsDtoAsync<AuthTokenReadDto>();
            return token!;
        }

        public static async Task<AuthTokenReadDto> RegisterLoginAndAuthorizeAsync(
            HttpClient client,
            UserRegisterDto? userRegisterDto = null)
        {
            var token = userRegisterDto is null
                ? await PostRegisterAndLoginAsync(client)
                : await PostRegisterAndLoginAsync(client, userRegisterDto.Email, userRegisterDto.Name, userRegisterDto.Password);

            client.SetAuthorization(token.AccessToken);

            return token;
        }
    }
}
