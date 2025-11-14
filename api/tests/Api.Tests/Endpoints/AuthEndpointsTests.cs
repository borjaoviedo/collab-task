using Api.Tests.Testing;
using Application.Abstractions.Security;
using Application.Auth.DTOs;
using Application.Users.DTOs;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using TestHelpers.Api.Auth;
using TestHelpers.Api.Defaults;
using TestHelpers.Api.Http;

namespace Api.Tests.Endpoints
{
    public sealed class AuthEndpointsTests
    {
        [Fact]
        public async Task Login_Returns200_With_Token_And_Metadata()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.PostRegisterResponseAsync(client);

            var loginResponse = await AuthTestHelper.PostLoginResponseAsync(client);
            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await loginResponse.ReadContentAsDtoAsync<AuthTokenReadDto>();

            dto.Should().NotBeNull();
            dto.AccessToken.Should().NotBeNullOrWhiteSpace();
            dto.TokenType.Should().Be(AuthDefaults.DefaultScheme);
            dto.ExpiresAtUtc.Offset.Should().Be(TimeSpan.Zero);
            dto.Email.Should().Be(UserDefaults.DefaultEmail.ToLowerInvariant());
            dto.Name.Should().Be(UserDefaults.DefaultUserName);
            dto.Role.Should().NotBe(null);
        }

        [Fact]
        public async Task Login_ExpiresAtUtc_Matches_Jwt_And_Is_Reasonable_Future()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.PostRegisterResponseAsync(client);

            var t0 = DateTime.UtcNow;
            var loginResponse = await AuthTestHelper.PostLoginResponseAsync(client);

            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await loginResponse.ReadContentAsDtoAsync<AuthTokenReadDto>();
            dto.Should().NotBeNull();

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(dto.AccessToken);

            dto.ExpiresAtUtc.Should().BeCloseTo(jwt.ValidTo, TimeSpan.FromSeconds(1));
            dto.ExpiresAtUtc.Should().BeAfter(t0.AddMinutes(5));
            dto.ExpiresAtUtc.Should().BeBefore(t0.AddMinutes(180));
        }

        [Fact]
        public async Task Login_Returns401_With_ProblemDetails_When_Email_Not_Found()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var loginResponse = await AuthTestHelper.PostLoginResponseAsync(client, UserDefaults.DefaultUserLoginDto);

            loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            loginResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

            var problem = await loginResponse.ReadContentAsDtoAsync<ProblemDetailsLike>();

            problem.Status.Should().Be(401);
            problem.Title.Should().Be("Unauthorized");
            problem.Detail.Should().NotBeNullOrWhiteSpace();
            problem.Type.Should().Contain("unauthorized");
        }

        [Fact]
        public async Task Login_Returns401_With_ProblemDetails_When_Password_Wrong()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.PostRegisterResponseAsync(client);

            var userLoginDto = new UserLoginDto { Email = UserDefaults.DefaultEmail, Password = "wrongP@ss1" };
            var loginResponse = await AuthTestHelper.PostLoginResponseAsync(client, userLoginDto);

            loginResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            loginResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");

            var problem = await loginResponse.ReadContentAsDtoAsync<ProblemDetailsLike>();

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

            var userLoginDto = new UserLoginDto { Email = email, Password = password };
            var loginResponse = await AuthTestHelper.PostLoginResponseAsync(client, userLoginDto);

            loginResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            loginResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        }

        [Fact]
        public async Task Login_Response_Is_Json()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await AuthTestHelper.PostRegisterResponseAsync(client);

            var loginResponse = await AuthTestHelper.PostLoginResponseAsync(client);

            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            loginResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        }

        [Fact]
        public async Task Register_Returns200_And_Jwt_On_Success()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var registerResponse = await AuthTestHelper.PostRegisterResponseAsync(client);
            registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await registerResponse.ReadContentAsDtoAsync<AuthTokenReadDto>();

            dto.Should().NotBeNull();
            dto.AccessToken.Should().NotBeNullOrWhiteSpace();
            dto.Email.Should().Be(UserDefaults.DefaultEmail);
            dto.Name.Should().Be(UserDefaults.DefaultUserName);

            var jwt = new JwtSecurityTokenHandler().ReadJwtToken(dto.AccessToken);

            jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Email && c.Value == UserDefaults.DefaultEmail);
            jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == UserDefaults.DefaultUserName);
            jwt.Claims.Should().Contain(c => c.Type == ClaimTypes.Role || c.Type == "role");
            jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Sub);
            jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
        }

        [Fact]
        public async Task Register_Returns409_On_Duplicate_Email_By_Precheck()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var dupEmail = $"dup_{Guid.NewGuid():N}@demo.com";
            var firstUserRegisterDto = new UserRegisterDto()
            {
                Email = dupEmail,
                Name = "User Name",
                Password = UserDefaults.DefaultPassword
            };
            var secondUserRegisterDto = new UserRegisterDto()
            {
                Email = dupEmail,
                Name = "Different User Name",
                Password = UserDefaults.DefaultPassword
            };

            var firstRegisterResponse = await AuthTestHelper.PostRegisterResponseAsync(client, firstUserRegisterDto);
            firstRegisterResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var secondRegisterResponse = await AuthTestHelper.PostRegisterResponseAsync(client, secondUserRegisterDto);
            secondRegisterResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }

        [Fact]
        public async Task Register_Returns409_On_Duplicate_Name_By_Precheck()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var name = "Dup Name";
            var firstUserRegisterDto = new UserRegisterDto()
            {
                Email = $"{Guid.NewGuid():N}@demo.com",
                Name = name,
                Password = UserDefaults.DefaultPassword
            };
            var secondUserRegisterDto = new UserRegisterDto()
            {
                Email = $"{Guid.NewGuid():N}@demo.com",
                Name = name,
                Password = UserDefaults.DefaultPassword
            };

            var firstRegisterResponse = await AuthTestHelper.PostRegisterResponseAsync(client, firstUserRegisterDto);
            firstRegisterResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var secondRegisterResponse = await AuthTestHelper.PostRegisterResponseAsync(client, secondUserRegisterDto);
            secondRegisterResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
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

            var userRegisterDto = new UserRegisterDto() { Email = email, Name = name, Password = password };
            var registerResponse = await AuthTestHelper.PostRegisterResponseAsync(client, userRegisterDto);

            registerResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task Register_Jwt_Claims_Are_Consistent_With_Response()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var registerResponse = await AuthTestHelper.PostRegisterResponseAsync(client, UserDefaults.DefaultUserRegisterDto);
            registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await registerResponse.ReadContentAsDtoAsync<AuthTokenReadDto>();
            dto.Should().NotBeNull();

            var token = new JwtSecurityTokenHandler().ReadJwtToken(dto.AccessToken);
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

            await AuthTestHelper.PostRegisterResponseAsync(client);

            var loginResponse = await AuthTestHelper.PostLoginResponseAsync(client);
            loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var auth = await loginResponse.ReadContentAsDtoAsync<AuthTokenReadDto>();
            client.SetAuthorization(auth.AccessToken);

            var authMeResponse = await AuthTestHelper.GetMeResponseAsync(client);
            authMeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var dto = await authMeResponse.ReadContentAsDtoAsync<MeReadDto>();

            dto.Should().NotBeNull();
            dto.Email.Should().Be(UserDefaults.DefaultEmail.ToLowerInvariant());
            dto.Name.Should().Be(UserDefaults.DefaultUserName);
            dto.Role.ToString().Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Me_Returns401_When_No_Token()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            var authMeResponse = await AuthTestHelper.GetMeResponseAsync(client);
            authMeResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Me_Returns401_When_User_Not_Found()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            await using var scope = app.Services.CreateAsyncScope();
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();

            var randomUserId = Guid.NewGuid();
            var (token, _) = jwt.CreateToken(randomUserId, "ghost@demo.com", "Ghost Name", UserRole.User);

            client.SetAuthorization(token);

            var authMeResponse = await AuthTestHelper.GetMeResponseAsync(client);
            authMeResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
            var key = (SymmetricSecurityKey)tvp.IssuerSigningKey;
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

            client.SetAuthorization(tokenStr);

            var authMeResponse = await AuthTestHelper.GetMeResponseAsync(client);
            authMeResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Me_Returns401_When_Token_Is_Invalid()
        {
            using var app = new TestApiFactory();
            using var client = app.CreateClient();

            client.SetAuthorization("not.a.valid.jwt");

            var authMeResponse = await AuthTestHelper.GetMeResponseAsync(client);
            authMeResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ---------- HELPERS ----------

        private sealed record ProblemDetailsLike(
            string Type,
            string Title,
            int Status,
            string Detail,
            string Instance);
    }
}
