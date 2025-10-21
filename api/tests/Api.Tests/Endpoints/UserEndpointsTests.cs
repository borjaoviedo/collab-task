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
            var u = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            // mint admin token
            var adminBearer = await MintToken(app, u.UserId, u.Email, u.Name, UserRole.Admin);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);

            var resp = await client.GetAsync($"/users/{u.UserId}");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await resp.Content.ReadFromJsonAsync<UserReadDto>(EndpointsTestHelper.Json);
            dto!.Id.Should().Be(u.UserId);
            dto.Email.Should().Be(u.Email);
        }

        [Fact]
        public async Task Get_ById_Returns403_When_Not_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var u = await EndpointsTestHelper.RegisterAndLoginAsync(client);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", u.AccessToken);
            var resp = await client.GetAsync($"/users/{u.UserId}");
            resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Get_All_Returns200_When_Admin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var u = await EndpointsTestHelper.RegisterAndLoginAsync(client);
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

            var u = await EndpointsTestHelper.RegisterAndLoginAsync(client);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", u.AccessToken);
            var resp = await client.GetAsync($"/users");
            resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Rename_Self_Returns200_When_Valid_IfMatch()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var u = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            var adminBearer = await MintToken(app, u.UserId, u.Email, u.Name, UserRole.Admin);

            var get = await EndpointsTestHelper.GetUser(client, u.UserId, adminBearer);

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

            var target = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            var adminBearer = await MintToken(app, target.UserId, target.Email, target.Name, UserRole.Admin);
            var current = await EndpointsTestHelper.GetUser(client, target.UserId, adminBearer);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);

            var base64 = Convert.ToBase64String(current.RowVersion);
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", $"W/\"{base64}\"");

            var payload = new UserChangeRoleDto() { NewRole = UserRole.Admin };
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

            var victim = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            var adminBearer = await MintToken(app, victim.UserId, victim.Email, victim.Name, UserRole.Admin);
            var current = await EndpointsTestHelper.GetUser(client, victim.UserId, adminBearer);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);
            var etag = $"W/\"{Convert.ToBase64String(current.RowVersion)}\"";

            var req = new HttpRequestMessage(HttpMethod.Delete, $"/users/{victim.UserId}");
            req.Headers.TryAddWithoutValidation("If-Match", etag);

            var resp = await client.SendAsync(req);
            resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        [Fact]
        public async Task GetById_And_List_Admin_vs_NotAdmin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var u = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            var adminBearer = await MintToken(app, u.UserId, u.Email, u.Name, UserRole.Admin);

            // /users (admin OK, non-admin 403)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);
            (await client.GetAsync("/users")).StatusCode.Should().Be(HttpStatusCode.OK);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", u.AccessToken);
            (await client.GetAsync("/users")).StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // /users/{id} (admin OK, non-admin 403)
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);
            var byId = await client.GetAsync($"/users/{u.UserId}");
            byId.StatusCode.Should().Be(HttpStatusCode.OK);
            byId.Headers.ETag.Should().NotBeNull();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", u.AccessToken);
            (await client.GetAsync($"/users/{u.UserId}")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetByEmail_Admin_200_And_404_And_403()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var u = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            var adminBearer = await MintToken(app, u.UserId, u.Email, u.Name, UserRole.Admin);

            // 200
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);
            (await client.GetAsync($"/users/by-email?email={Uri.EscapeDataString(u.Email)}"))
                .StatusCode.Should().Be(HttpStatusCode.OK);

            // 404
            (await client.GetAsync($"/users/by-email?email={Uri.EscapeDataString("no@x.com")}"))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);

            // 403
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", u.AccessToken);
            (await client.GetAsync($"/users/by-email?email={Uri.EscapeDataString(u.Email)}"))
                .StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Rename_Self_Valid_200_Stale_412_Missing_428()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var u = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            var adminBearer = await MintToken(app, u.UserId, u.Email, u.Name, UserRole.Admin);
            var current = await EndpointsTestHelper.GetUser(client, u.UserId, adminBearer);

            // Self rename with valid If-Match
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", u.AccessToken);
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", $"W/\"{Convert.ToBase64String(current.RowVersion)}\"");
            var ok = await client.PatchAsJsonAsync($"/users/{u.UserId}/rename", new UserRenameDto { NewName = "Renamed" });
            ok.StatusCode.Should().Be(HttpStatusCode.OK);

            // Stale 412 (reuse old rowversion)
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", $"W/\"{Convert.ToBase64String(current.RowVersion)}\"");
            var stale = await client.PatchAsJsonAsync($"/users/{u.UserId}/rename", new UserRenameDto { NewName = "Again" });
            stale.StatusCode.Should().Be((HttpStatusCode)412);

            // Missing 428
            client.DefaultRequestHeaders.IfMatch.Clear();
            var missing = await client.PatchAsJsonAsync($"/users/{u.UserId}/rename", new UserRenameDto { NewName = "New name" });
            missing.StatusCode.Should().Be((HttpStatusCode)428);
        }

        [Fact]
        public async Task ChangeRole_Admin_200_Stale_412_Missing_428_And_403_NotAdmin()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var target = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            var adminBearer = await MintToken(app, target.UserId, target.Email, target.Name, UserRole.Admin);
            var current = await EndpointsTestHelper.GetUser(client, target.UserId, adminBearer);

            // Admin OK
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", $"W/\"{Convert.ToBase64String(current.RowVersion)}\"");
            var ok = await client.PatchAsJsonAsync($"/users/{target.UserId}/role", new UserChangeRoleDto { NewRole = UserRole.Admin });
            ok.StatusCode.Should().Be(HttpStatusCode.OK);

            // Stale 412
            client.DefaultRequestHeaders.IfMatch.Clear();
            client.DefaultRequestHeaders.TryAddWithoutValidation("If-Match", $"W/\"{Convert.ToBase64String(current.RowVersion)}\"");
            var stale = await client.PatchAsJsonAsync($"/users/{target.UserId}/role", new UserChangeRoleDto { NewRole = UserRole.User });
            stale.StatusCode.Should().Be((HttpStatusCode)412);

            // Missing 428
            client.DefaultRequestHeaders.IfMatch.Clear();
            var missing = await client.PatchAsJsonAsync($"/users/{target.UserId}/role", new UserChangeRoleDto { NewRole = UserRole.User });
            missing.StatusCode.Should().Be((HttpStatusCode)428);

            // 403 not admin
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", target.AccessToken);
            client.DefaultRequestHeaders.IfMatch.Clear();
            var forbidden = await client.PatchAsJsonAsync($"/users/{target.UserId}/role", new UserChangeRoleDto { NewRole = UserRole.User });
            forbidden.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task Delete_Admin_204_And_428_When_Missing()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var victim = await EndpointsTestHelper.RegisterAndLoginAsync(client);
            var adminBearer = await MintToken(app, victim.UserId, victim.Email, victim.Name, UserRole.Admin);
            var current = await EndpointsTestHelper.GetUser(client, victim.UserId, adminBearer);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminBearer);

            // 428
            client.DefaultRequestHeaders.IfMatch.Clear();
            var miss = await client.DeleteAsync($"/users/{victim.UserId}");
            miss.StatusCode.Should().Be((HttpStatusCode)428);

            // 204
            var etag = $"W/\"{Convert.ToBase64String(current.RowVersion)}\"";
            var req = new HttpRequestMessage(HttpMethod.Delete, $"/users/{victim.UserId}");
            req.Headers.TryAddWithoutValidation("If-Match", etag);
            var del = await client.SendAsync(req);
            del.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        private static async Task<string> MintToken(TestApiFactory app, Guid userId, string email, string name, UserRole role)
        {
            await using var scope = app.Services.CreateAsyncScope();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            var (token, _) = jwt.CreateToken(userId, email, name, role);
            return token;
        }
    }
}
