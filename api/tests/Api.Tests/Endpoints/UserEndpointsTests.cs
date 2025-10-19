using Api.Tests.Testing;
using Application.Common.Abstractions.Security;
using Application.Users.DTOs;
using Domain.Enums;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TestHelpers;

namespace Api.Tests.Endpoints
{
    public sealed class UsersEndpointsTests
    {
        [Fact]
        public async Task Get_ById_Returns200_When_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // create subject user
            var u = await RegisterAndLogin(client);
            // mint admin token
            var adminBearer = await MintToken(app, u.UserId, u.Email, u.Name, UserRole.Admin);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);

            var resp = await client.GetAsync($"/users/{u.UserId}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await resp.Content.ReadFromJsonAsync<UserReadDto>(AuthTestHelper.Json);
            dto!.Id.Should().Be(u.UserId);
            dto.Email.Should().Be(u.Email);
        }

        [Fact]
        public async Task Get_ById_Returns403_When_Not_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var u = await RegisterAndLogin(client);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", u.AccessToken);
            var resp = await client.GetAsync($"/users/{u.UserId}");
            resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Get_All_Returns200_When_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var u = await RegisterAndLogin(client);
            var adminBearer = await MintToken(app, u.UserId, u.Email, u.Name, UserRole.Admin);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);

            var resp = await client.GetAsync($"/users");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Get_All_Returns403_When_Not_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var u = await RegisterAndLogin(client);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", u.AccessToken);
            var resp = await client.GetAsync($"/users");
            resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Rename_Self_Returns200_When_Valid_IfMatch()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var u = await RegisterAndLogin(client);
            var adminBearer = await MintToken(app, u.UserId, u.Email, u.Name, UserRole.Admin);

            var get = await GetUser(client, u.UserId, adminBearer);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", u.AccessToken);

            var base64 = Convert.ToBase64String(get.RowVersion);
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", $"W/\"{base64}\"");

            var payload = new UserRenameDto() { NewName = "Renamed User"};
            var resp = await client.PatchAsJsonAsync($"/users/{u.UserId}/rename", payload);

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await resp.Content.ReadFromJsonAsync<UserReadDto>();
            body!.Name.Should().Be("Renamed User");
        }

        [Fact]
        public async Task Change_Role_Returns200_When_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var target = await RegisterAndLogin(client);
            var adminBearer = await MintToken(app, target.UserId, target.Email, target.Name, UserRole.Admin);
            var current = await GetUser(client, target.UserId, adminBearer);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);

            var base64 = Convert.ToBase64String(current.RowVersion);
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", $"W/\"{base64}\"");

            var payload = new UserChangeRoleDto() { NewRole = Domain.Enums.UserRole.Admin };
            var resp = await client.PatchAsJsonAsync($"/users/{target.UserId}/role", payload);

            resp.StatusCode.Should().Be(HttpStatusCode.OK);
            var body = await resp.Content.ReadFromJsonAsync<UserReadDto>();
            body!.Role.Should().Be(UserRole.Admin);
        }

        [Fact]
        public async Task Delete_Returns204_When_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var victim = await RegisterAndLogin(client);
            var adminBearer = await MintToken(app, victim.UserId, victim.Email, victim.Name, UserRole.Admin);
            var current = await GetUser(client, victim.UserId, adminBearer);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);
            var etag = $"W/\"{Convert.ToBase64String(current.RowVersion)}\"";

            var req = new HttpRequestMessage(HttpMethod.Delete, $"/users/{victim.UserId}");
            req.Headers.TryAddWithoutValidation("If-Match", etag);

            var resp = await client.SendAsync(req);
            resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        // ---- helpers ----

        private static async Task<AuthTestHelper.AuthToken> RegisterAndLogin(HttpClient client)
        {
            var email = $"{Guid.NewGuid():N}@demo.com";
            var name = "Test User";
            var password = "Str0ngP@ss!";
            (await client.PostAsJsonAsync("/auth/register", new UserRegisterDto() { Email = email, Name = name, Password = password })).EnsureSuccessStatusCode();
            var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
            login.EnsureSuccessStatusCode();
            var dto = await login.Content.ReadFromJsonAsync<AuthTestHelper.AuthToken>(AuthTestHelper.Json);
            return dto!;
        }

        private static async Task<string> MintToken(TestApiFactory app, Guid userId, string email, string name, UserRole role)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var (token, _) = jwt.CreateToken(userId, email, name, role);
            return token;
        }

        private static async Task<UserReadDto> GetUser(HttpClient client, Guid userId, string adminBearer)
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);
            var resp = await client.GetAsync($"/users/{userId}");
            resp.EnsureSuccessStatusCode();
            var dto = await resp.Content.ReadFromJsonAsync<UserReadDto>(AuthTestHelper.Json);
            return dto!;
        }
    }
}
