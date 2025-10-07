using Api.Tests.Common.Helpers;
using Api.Tests.Testing;
using Application.Common.Abstractions.Security;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Tests.Endpoints
{
    public sealed class UsersEndpointsTests
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        private sealed record RegisterReq(string Email, string Name, string Password);
        private sealed record AuthToken(string AccessToken, Guid UserId, string Email, string Name, string Role);
        private sealed record UserReadDto(Guid Id, string Email, string Name, string Role, byte[] RowVersion);
        private sealed record RenameUserDto(string Name, byte[] RowVersion);
        private sealed record ChangeRoleDto(int Role, byte[] RowVersion);
        private sealed record DeleteUserDto(byte[] RowVersion);

        [Fact]
        public async Task Get_ById_Returns200_When_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // create subject user
            var u = await RegisterAndLogin(client);
            // mint admin token
            var adminBearer = await MintToken(app, u.UserId, u.Email, u.Name, "Admin");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);

            var resp = await client.GetAsync($"/users/{u.UserId}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await resp.Content.ReadFromJsonAsync<UserReadDto>(Json);
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
        public async Task Rename_Self_Returns204_When_Valid_RowVersion()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var u = await RegisterAndLogin(client);
            var adminBearer = await MintToken(app, u.UserId, u.Email, u.Name, "Admin");

            // get current RowVersion via admin GET
            var get = await GetUser(client, u.UserId, adminBearer);
            var row = get.RowVersion;

            // now rename with self token
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", u.AccessToken);
            var payload = new RenameUserDto("Renamed User", row);
            var resp = await client.PatchAsJsonAsync($"/users/{u.UserId}/name", payload);
            resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Change_Role_Returns204_When_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // target user
            var target = await RegisterAndLogin(client);

            // admin acts on target
            var adminBearer = await MintToken(app, target.UserId, target.Email, target.Name, "Admin");
            var current = await GetUser(client, target.UserId, adminBearer);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);
            var payload = new ChangeRoleDto(1, current.RowVersion);
            var resp = await client.PatchAsJsonAsync($"/users/{target.UserId}/role", payload);
            resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task Delete_Returns204_When_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var victim = await RegisterAndLogin(client);
            var adminBearer = await MintToken(app, victim.UserId, victim.Email, victim.Name, "Admin");
            var current = await GetUser(client, victim.UserId, adminBearer);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);
            var payload = new DeleteUserDto(current.RowVersion);
            var resp = await client.DeleteAsJsonAsync($"/users/{victim.UserId}", payload);
            resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        // ---- helpers ----

        private static async Task<AuthToken> RegisterAndLogin(HttpClient client)
        {
            var email = $"{Guid.NewGuid():N}@demo.com";
            var name = "Test User";
            var password = "Str0ngP@ss!";
            (await client.PostAsJsonAsync("/auth/register", new RegisterReq(email, name, password))).EnsureSuccessStatusCode();
            var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
            login.EnsureSuccessStatusCode();
            var dto = await login.Content.ReadFromJsonAsync<AuthToken>(Json);
            return dto!;
        }

        private static async Task<string> MintToken(TestApiFactory app, Guid userId, string email, string name, string role)
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
            var dto = await resp.Content.ReadFromJsonAsync<UserReadDto>(Json);
            return dto!;
        }
    }
}
