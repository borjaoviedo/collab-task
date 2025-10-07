using FluentAssertions;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using Api.Tests.Testing;
using System.Security.Claims;

namespace Api.Tests.Endpoints
{
    public sealed class AuthRegisterEndpointTests
    {
        private sealed record RegisterReq(string Email, string Name, string Password);
        private sealed record AuthTokenReadDtoContract(string AccessToken, Guid UserId, string Email, string Name, string Role);

        [Fact]
        public async Task Register_Returns200_And_Jwt_On_Success()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var payload = new RegisterReq($"ok_{Guid.NewGuid():N}@demo.com", "Valid Name", "Str0ngP@ss!");
            var resp = await client.PostAsJsonAsync("/auth/register", payload);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await resp.Content.ReadFromJsonAsync<AuthTokenReadDtoContract>();
            dto.Should().NotBeNull();
            dto!.AccessToken.Should().NotBeNullOrWhiteSpace();
            dto.Email.Should().Be(payload.Email);
            dto.Name.Should().Be(payload.Name);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(dto.AccessToken);

            jwt.Claims.Should().Contain(c =>
                c.Type == JwtRegisteredClaimNames.Email && c.Value == payload.Email);

            jwt.Claims.Should().Contain(c =>
                c.Type == ClaimTypes.Name && c.Value == payload.Name);

            jwt.Claims.Should().Contain(c =>
                c.Type == ClaimTypes.Role || c.Type == "role");

            jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);
            jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        }

        [Fact]
        public async Task Register_Returns409_On_Duplicate_Email_By_Precheck()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var email = $"dup_{Guid.NewGuid():N}@demo.com";
            var firstPayload = new RegisterReq(email, "User Name", "Str0ngP@ss!");
            var secondPayload = new RegisterReq(email, "Different User Name", "Str0ngP@ss!");

            var first = await client.PostAsJsonAsync("/auth/register", firstPayload);
            first.StatusCode.Should().Be(HttpStatusCode.OK);

            var second = await client.PostAsJsonAsync("/auth/register", secondPayload);
            second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Register_Returns409_On_Duplicate_Name_By_Precheck()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var name = "Dup Name";
            var firstPayload = new RegisterReq($"{Guid.NewGuid():N}@demo.com", name, "Str0ngP@ss!");
            var secondPayload = new RegisterReq($"{Guid.NewGuid():N}@demo.com", name, "Str0ngP@ss!");

            var first = await client.PostAsJsonAsync("/auth/register", firstPayload);
            first.StatusCode.Should().Be(HttpStatusCode.OK);

            var second = await client.PostAsJsonAsync("/auth/register", secondPayload);
            second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Register_Returns409_On_Duplicate_Email_By_UniqueConstraint_Race()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var email = $"race_{Guid.NewGuid():N}@demo.com";
            var firstPayload = new RegisterReq(email, "User Name", "Str0ngP@ss!");
            var secondPayload = new RegisterReq(email, "Different User Name", "Str0ngP@ss!");

            var t1 = client.PostAsJsonAsync("/auth/register", firstPayload);
            var t2 = client.PostAsJsonAsync("/auth/register", secondPayload);

            var results = await Task.WhenAll(t1, t2);
            results.Should().HaveCount(2);

            results.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
            results.Count(r => r.StatusCode == HttpStatusCode.Conflict).Should().Be(1);
        }

        [Fact]
        public async Task Register_Returns409_On_Duplicate_Name_By_UniqueConstraint_Race()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var name = "R Name";
            var firstPayload = new RegisterReq($"{Guid.NewGuid():N}@demo.com", name, "Str0ngP@ss!");
            var secondPayload = new RegisterReq($"{Guid.NewGuid():N}@demo.com", name, "Str0ngP@ss!");

            var t1 = client.PostAsJsonAsync("/auth/register", firstPayload);
            var t2 = client.PostAsJsonAsync("/auth/register", secondPayload);

            var results = await Task.WhenAll(t1, t2);
            results.Should().HaveCount(2);

            results.Count(r => r.StatusCode == HttpStatusCode.OK).Should().Be(1);
            results.Count(r => r.StatusCode == HttpStatusCode.Conflict).Should().Be(1);
        }

        [Theory]
        [InlineData("", "User Name", "Str0ngP@ss!")]
        [InlineData("not-an-email", "User Name", "Str0ngP@ss!")]
        [InlineData("bad@demo.com", "User-Name", "Str0ngP@ss!")]
        [InlineData("bad@demo.com", "User  Name", "Str0ngP@ss!")]
        [InlineData("bad@demo.com", "User Nam3", "Str0ngP@ss!")]
        [InlineData("bad@demo.com", "User Name", "")]
        [InlineData("bad2@demo.com", "User Name", "short")]
        public async Task Register_Returns400_On_Invalid_Payload(string email, string name, string password)
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var payload = new RegisterReq(email, name, password);
            var resp = await client.PostAsJsonAsync("/auth/register", payload);

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_Jwt_Claims_Are_Consistent_With_Response()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var email = $"claims_{Guid.NewGuid():N}@demo.com";
            var name = "Claim User";
            var payload = new RegisterReq(email, name, "Str0ngP@ss!");

            var resp = await client.PostAsJsonAsync("/auth/register", payload);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await resp.Content.ReadFromJsonAsync<AuthTokenReadDtoContract>();
            dto.Should().NotBeNull();

            var token = new JwtSecurityTokenHandler().ReadJwtToken(dto!.AccessToken);
            var emailClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Email)?.Value;
            emailClaim.Should().Be(dto.Email);

            var nameClaim = token.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
            nameClaim.Should().Be(dto.Name);

            var sub = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value;
            Guid.TryParse(sub, out var subGuid).Should().BeTrue();
            subGuid.Should().Be(dto.UserId);
        }
    }
}
