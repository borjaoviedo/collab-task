using Api.Tests.Testing;
using FluentAssertions;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Api.Tests.Endpoints
{
    public sealed class AuthLoginEndpointTests
    {
        private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

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

            var dto = await resp.Content.ReadFromJsonAsync<AuthTokenReadDto>(Json);
            dto.Should().NotBeNull();

            dto!.AccessToken.Should().NotBeNullOrWhiteSpace();
            dto.TokenType.Should().Be("Bearer");
            dto.ExpiresAtUtc.Kind.Should().Be(DateTimeKind.Utc);

            dto.Email.Should().Be(email.ToLowerInvariant());
            dto.Name.Should().Be(name);
            dto.Role.Should().NotBeNullOrWhiteSpace();
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

            var dto = await resp.Content.ReadFromJsonAsync<AuthTokenReadDto>(Json);
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

            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetailsLike>(Json);
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

            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetailsLike>(Json);
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
    }

    // -------- Helper DTOs --------

    public sealed class AuthTokenReadDto
    {
        public string AccessToken { get; set; } = null!;
        public string TokenType { get; set; } = null!;
        public DateTime ExpiresAtUtc { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Role { get; set; } = null!;
    }

    public sealed class JwtOptions
    {
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        public string Key { get; set; } = null!;
        public int ExpMinutes { get; set; }
    }

    public sealed class ProblemDetailsLike
    {
        public string? Type { get; set; }
        public string? Title { get; set; }
        public int? Status { get; set; }
        public string? Detail { get; set; }
        public string? Instance { get; set; }
    }
}
