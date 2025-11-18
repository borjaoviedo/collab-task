using Api.Auth.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;
using TestHelpers.Common.Testing;

namespace Api.Tests.Auth.Services
{
    [UnitTest]
    public class CurrentUserServiceTests
    {
        // ---------- TESTS ----------

        [Fact]
        public void WhenAuthenticated_ExposesUserId()
        {
            var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var ctx = BuildContext(userId, "user@demo.com", "Admin");
            var sut = BuildSut(ctx);

            sut.UserId.Should().Be(userId);
        }

        [Theory]
        [InlineData(null, false)] // no HttpContext at all
        [InlineData("empty", true)] // HttpContext exists but no principal claims
        [InlineData("only-email", true)] // Missing NameIdentifier claim
        [InlineData("invalid-guid", true)] // Invalid GUID in NameIdentifier
        public void NullUserId_On_Absent_Context_Or_Missing_Or_Invalid_Claims(
            string? scenarioKey,
            bool hasEmail)
        {
            HttpContext? http = scenarioKey switch
            {
                null => null, // accessor.HttpContext = null
                "empty" => new DefaultHttpContext(), // empty principal, no claims
                "only-email" => BuildContext(userId: null, email: "user@demo.com"),
                "invalid-guid" => new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                    new Claim(ClaimTypes.NameIdentifier, "not-a-guid"),
                    new Claim(ClaimTypes.Email, hasEmail ? "user@demo.com" : "")
                ], "Test"))
                },
                _ => throw new ArgumentOutOfRangeException(nameof(scenarioKey))
            };

            var sut = BuildSut(http);

            sut.UserId.Should().BeNull();
        }

        // ---------- HELPERS ----------

        private static DefaultHttpContext BuildContext(
            Guid? userId = null,
            string? email = null,
            string? role = null)
        {
            var identity = new ClaimsIdentity(authenticationType: "Test");

            if (userId.HasValue)
                identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()));

            if (!string.IsNullOrWhiteSpace(email))
                identity.AddClaim(new Claim(ClaimTypes.Email, email));

            if (!string.IsNullOrWhiteSpace(role))
                identity.AddClaim(new Claim(ClaimTypes.Role, role));

            var principal = new ClaimsPrincipal(identity);
            return new DefaultHttpContext { User = principal };
        }

        private static CurrentUserService BuildSut(HttpContext? httpContext)
        {
            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(httpContext);

            return new CurrentUserService(accessor.Object);
        }
    }
}
