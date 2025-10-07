using Api.Auth.DTOs;
using Api.Tests.Testing;
using Application.Common.Abstractions.Security;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace Api.Tests.Endpoints
{
    public sealed class AuthMeEndpointTests
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

        [Fact]
        public async Task Me_Returns200_With_Profile_When_Authorized()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var email = $"me+{Guid.NewGuid():N}@demo.com";
            var name = "User Name";
            var password = "Str0ngP@ss!";

            (await client.PostAsJsonAsync("/auth/register", new { email, name, password }))
                .EnsureSuccessStatusCode();

            var login = await client.PostAsJsonAsync("/auth/login", new { email, password });
            login.StatusCode.Should().Be(HttpStatusCode.OK);
            var auth = await login.Content.ReadFromJsonAsync<AuthTokenReadDto>(Json);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

            var resp = await client.GetAsync("/auth/me");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await resp.Content.ReadFromJsonAsync<MeReadDto>(Json);
            dto.Should().NotBeNull();
            dto.Email.Should().Be(email.ToLowerInvariant());
            dto.Name.Should().Be(name);
            dto.Role.ToString().Should().NotBeNullOrWhiteSpace();
        }

        // ---------- 401: no token ----------

        [Fact]
        public async Task Me_Returns401_When_No_Token()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var resp = await client.GetAsync("/auth/me");
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ---------- 401: user not found (valid token for non-existing user) ----------

        [Fact]
        public async Task Me_Returns401_When_User_Not_Found()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await using var scope = app.Services.CreateAsyncScope();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

            var randomUserId = Guid.NewGuid(); // not in DB
            var (token, _) = jwt.CreateToken(randomUserId, "ghost@demo.com", "Ghost Name", "User");

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await client.GetAsync("/auth/me");
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ---------- 401: missing 'sub' claim but valid signature ----------

        [Fact]
        public async Task Me_Returns401_When_Token_Missing_Sub_Claim()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            // Build a signed JWT without 'sub' using the same signing key/issuer/audience
            await using var scope = app.Services.CreateAsyncScope();

            var jwtBearer = scope.ServiceProvider
                .GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                .Get(JwtBearerDefaults.AuthenticationScheme);

            var tvp = jwtBearer.TokenValidationParameters;
            var key = (SymmetricSecurityKey)tvp.IssuerSigningKey!;
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var now = DateTime.UtcNow;
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Email, "nosub@demo.com"),
                new(ClaimTypes.Role, "User"),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                // Intentionally no 'sub'
            };

            var token = new JwtSecurityToken(
                issuer: tvp.ValidIssuer,
                audience: tvp.ValidAudience,
                claims: claims,
                notBefore: now,
                expires: now.AddMinutes(5),
                signingCredentials: creds);

            var tokenStr = new JwtSecurityTokenHandler().WriteToken(token);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenStr);

            var resp = await client.GetAsync("/auth/me");
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ---------- 401: structurally invalid token ----------

        [Fact]
        public async Task Me_Returns401_When_Token_Is_Invalid()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not.a.valid.jwt");

            var resp = await client.GetAsync("/auth/me");
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }

    // -------- Helper DTO for tests --------

    public sealed class UserReadDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
}
