using FluentAssertions;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Api.Tests.Testing;

namespace Api.Tests.Endpoints.Auth
{
    public sealed class AuthRegisterEndpointTests
    {
        private sealed record RegisterReq(string Email, string Password);
        private sealed record AuthTokenReadDtoContract(string AccessToken, Guid UserId, string Email, string Role);

        [Fact]
        public async Task Register_Returns200_And_Jwt_On_Success()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var payload = new RegisterReq($"ok_{Guid.NewGuid():N}@demo.com", "Str0ngP@ss!");
            var resp = await client.PostAsJsonAsync("/auth/register", payload);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await resp.Content.ReadFromJsonAsync<AuthTokenReadDtoContract>();
            dto.Should().NotBeNull();
            dto!.AccessToken.Should().NotBeNullOrWhiteSpace();
            dto.Email.Should().Be(payload.Email);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(dto.AccessToken);

            jwt.Claims.Should().Contain(c =>
                c.Type == JwtRegisteredClaimNames.Email && c.Value == payload.Email);

            jwt.Claims.Should().Contain(c =>
                c.Type == System.Security.Claims.ClaimTypes.Role || c.Type == "role");

            jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);
            jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        }

        [Fact]
        public async Task Register_Returns409_On_Duplicate_By_Precheck()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var email = $"dup_{Guid.NewGuid():N}@demo.com";
            var payload = new RegisterReq(email, "Str0ngP@ss!");

            var first = await client.PostAsJsonAsync("/auth/register", payload);
            first.StatusCode.Should().Be(HttpStatusCode.OK);

            var second = await client.PostAsJsonAsync("/auth/register", payload);
            second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Register_Returns409_On_Duplicate_By_UniqueConstraint_Race()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var email = $"race_{Guid.NewGuid():N}@demo.com";
            var payload = new RegisterReq(email, "Str0ngP@ss!");

            var t1 = client.PostAsJsonAsync("/auth/register", payload);
            var t2 = client.PostAsJsonAsync("/auth/register", payload);

            var results = await Task.WhenAll(t1, t2);
            results.Should().HaveCount(2);

            results.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
            results.Count(r => r.StatusCode == HttpStatusCode.Conflict).Should().Be(1);
        }

        [Theory]
        [InlineData("", "Str0ngP@ss!")]
        [InlineData("not-an-email", "Str0ngP@ss!")]
        [InlineData("bad@demo.com", "")]
        [InlineData("bad2@demo.com", "short")]
        public async Task Register_Returns400_On_Invalid_Payload(string email, string password)
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var payload = new RegisterReq(email, password);
            var resp = await client.PostAsJsonAsync("/auth/register", payload);

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_Jwt_Claims_Are_Consistent_With_Response()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var email = $"claims_{Guid.NewGuid():N}@demo.com";
            var payload = new RegisterReq(email, "Str0ngP@ss!");

            var resp = await client.PostAsJsonAsync("/auth/register", payload);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await resp.Content.ReadFromJsonAsync<AuthTokenReadDtoContract>();
            dto.Should().NotBeNull();

            var token = new JwtSecurityTokenHandler().ReadJwtToken(dto!.AccessToken);
            var emailClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
            emailClaim.Should().Be(dto.Email);

            var sub = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            Guid.TryParse(sub, out var subGuid).Should().BeTrue();
            subGuid.Should().Be(dto.UserId);
        }
    }
}
