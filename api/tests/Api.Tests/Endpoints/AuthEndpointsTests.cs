using Api.Auth.DTOs;
using Api.Tests.Testing;
using Application.Common.Abstractions.Security;
using Application.Users.DTOs;
using Domain.Enums;
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
using TestHelpers;

namespace Api.Tests.Endpoints
{
    public sealed class AuthEndpointsTests
    {
        public sealed record ProblemDetailsLike(string Type, string Title, int Status, string Detail, string Instance);
        [Fact]
        public async Task Login_Returns200_With_Token_And_Metadata()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var email = $"ok+{Guid.NewGuid():N}@demo.com";
            var name = "User Name";
            var password = "Str0ngP@ss!";

            (await client.PostAsJsonAsync("/auth/register", new { email, name, password })).EnsureSuccessStatusCode();

            var resp = await client.PostAsJsonAsync("/auth/login", new { email, password });
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await resp.Content.ReadFromJsonAsync<AuthTokenReadDto>(AuthTestHelper.Json);
            dto.Should().NotBeNull();

            dto!.AccessToken.Should().NotBeNullOrWhiteSpace();
            dto.TokenType.Should().Be("Bearer");
            dto.ExpiresAtUtc.Offset.Should().Be(TimeSpan.Zero);
            dto.Email.Should().Be(email.ToLowerInvariant());
            dto.Name.Should().Be(name);
            dto.Role.Should().NotBe(null);
        }

        [Fact]
        public async Task Login_ExpiresAtUtc_Matches_Jwt_And_Is_Reasonable_Future()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var email = $"exp+{Guid.NewGuid():N}@demo.com";
            var name = "User Name";
            var password = "Str0ngP@ss!";

            (await client.PostAsJsonAsync("/auth/register", new { email, name, password })).EnsureSuccessStatusCode();

            var t0 = DateTime.UtcNow;
            var resp = await client.PostAsJsonAsync("/auth/login", new { email, password });
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await resp.Content.ReadFromJsonAsync<AuthTokenReadDto>(AuthTestHelper.Json);
            dto.Should().NotBeNull();

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(dto!.AccessToken);
            dto.ExpiresAtUtc.Should().BeCloseTo(jwt.ValidTo, TimeSpan.FromSeconds(1));

            dto.ExpiresAtUtc.Should().BeAfter(t0.AddMinutes(5));
            dto.ExpiresAtUtc.Should().BeBefore(t0.AddMinutes(180));
        }

        [Fact]
        public async Task Login_Returns401_With_ProblemDetails_When_Email_Not_Found()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var resp = await client.PostAsJsonAsync("/auth/login", new
            {
                email = $"missing+{Guid.NewGuid():N}@demo.com",
                password = "S0mething_"
            });

            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            resp.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetailsLike>(AuthTestHelper.Json);
            problem!.Status.Should().Be(401);
            problem.Title.Should().Be("Unauthorized");
            problem.Detail.Should().NotBeNullOrWhiteSpace();
            problem.Type.Should().Contain("unauthorized");
        }

        [Fact]
        public async Task Login_Returns401_With_ProblemDetails_When_Password_Wrong()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var email = $"badpwd+{Guid.NewGuid():N}@demo.com";
            var name = "User Name";
            var password = "Str0ngP@ss!";

            (await client.PostAsJsonAsync("/auth/register", new { email, name, password }))
                .EnsureSuccessStatusCode();

            var resp = await client.PostAsJsonAsync("/auth/login", new { email, password = "wrongP@ss1" });

            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            resp.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetailsLike>(AuthTestHelper.Json);
            problem!.Status.Should().Be(401);
            problem.Title.Should().Be("Unauthorized");
            problem.Detail.Should().NotBeNullOrWhiteSpace();
        }

        [Theory]
        [InlineData("", "Str0ngP@ss!")]
        [InlineData("not-an-email", "Str0ngP@ss!")]
        [InlineData("user@demo.com", "")]
        public async Task Login_Returns400_On_Invalid_Payload(string email, string password)
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var resp = await client.PostAsJsonAsync("/auth/login", new { email, password });

            resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            resp.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        }

        [Fact]
        public async Task Login_Response_Is_Json()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var email = $"cache+{Guid.NewGuid():N}@demo.com";
            var name = "User Name";
            var password = "Str0ngP@ss!";

            (await client.PostAsJsonAsync("/auth/register", new { email, name, password }))
                .EnsureSuccessStatusCode();

            var resp = await client.PostAsJsonAsync("/auth/login", new { email, password });
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            resp.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        }

        [Fact]
        public async Task Register_Returns200_And_Jwt_On_Success()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var payload = new UserRegisterDto() { Email = $"ok_{Guid.NewGuid():N}@demo.com" , Name = "Valid Name", Password = "Str0ngP@ss!" };
            var resp = await client.PostAsJsonAsync("/auth/register", payload);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await resp.Content.ReadFromJsonAsync<AuthTokenReadDto>();
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
            var firstPayload = new UserRegisterDto() { Email = email, Name = "User Name", Password = "Str0ngP@ss!" };
            var secondPayload = new UserRegisterDto() { Email = email, Name = "Different User Name", Password = "Str0ngP@ss!" };

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
            var firstPayload = new UserRegisterDto() { Email = $"{Guid.NewGuid():N}@demo.com", Name = name, Password = "Str0ngP@ss!" };
            var secondPayload = new UserRegisterDto() { Email = $"{Guid.NewGuid():N}@demo.com", Name = name, Password = "Str0ngP@ss!" };

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
            var firstPayload = new UserRegisterDto() { Email = email, Name = "User Name", Password = "Str0ngP@ss!" };
            var secondPayload = new UserRegisterDto() { Email = email, Name = "Different User Name", Password = "Str0ngP@ss!" };

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
            var firstPayload = new UserRegisterDto() { Email = $"{Guid.NewGuid():N}@demo.com", Name = name, Password = "Str0ngP@ss!" };
            var secondPayload = new UserRegisterDto() { Email = $"{Guid.NewGuid():N}@demo.com", Name = name, Password = "Str0ngP@ss!" };

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

            var payload = new UserRegisterDto() { Email = email, Name = name, Password = password } ;
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
            var payload = new UserRegisterDto() { Email = email, Name = name, Password = "Str0ngP@ss!" };

            var resp = await client.PostAsJsonAsync("/auth/register", payload);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await resp.Content.ReadFromJsonAsync<AuthTokenReadDto>();
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
            var auth = await login.Content.ReadFromJsonAsync<AuthTokenReadDto>(AuthTestHelper.Json);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

            var resp = await client.GetAsync("/auth/me");
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await resp.Content.ReadFromJsonAsync<MeReadDto>(AuthTestHelper.Json);
            dto.Should().NotBeNull();
            dto.Email.Should().Be(email.ToLowerInvariant());
            dto.Name.Should().Be(name);
            dto.Role.ToString().Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Me_Returns401_When_No_Token()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var resp = await client.GetAsync("/auth/me");
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Me_Returns401_When_User_Not_Found()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await using var scope = app.Services.CreateAsyncScope();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

            var randomUserId = Guid.NewGuid(); // not in DB
            var (token, _) = jwt.CreateToken(randomUserId, "ghost@demo.com", "Ghost Name", UserRole.User);

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var resp = await client.GetAsync("/auth/me");
            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

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
}
