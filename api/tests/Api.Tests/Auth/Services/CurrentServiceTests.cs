using Api.Auth.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Security.Claims;

namespace Api.Tests.Auth.Services
{
    public class CurrentUserService_Tests
    {
        private static DefaultHttpContext BuildContext(Guid userId, string email, string role)
        {
            var claims = new[]
            {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(ClaimTypes.Role, role)
        };
            var identity = new ClaimsIdentity(claims, authenticationType: "Test");
            var principal = new ClaimsPrincipal(identity);
            var ctx = new DefaultHttpContext { User = principal };
            return ctx;
        }

        [Fact]
        public void WhenAuthenticated_ExposesUserId()
        {
            var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var ctx = BuildContext(userId, "user@demo.com", "Admin");

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(ctx);

            var sut = new CurrentUserService(accessor.Object);
            sut.UserId.Should().Be(userId);
        }

        [Fact]
        public void WhenNoPrincipal_NullUserId()
        {
            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext());

            var sut = new CurrentUserService(accessor.Object);
            sut.UserId.Should().BeNull();
        }

        [Fact]
        public void MissingRequiredClaims_NullUserId()
        {
            var identity = new ClaimsIdentity(authenticationType: "Test");
            identity.AddClaim(new Claim(ClaimTypes.Email, "user@demo.com"));

            var principal = new ClaimsPrincipal(identity);
            var ctx = new DefaultHttpContext { User = principal };

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(ctx);

            var sut = new CurrentUserService(accessor.Object);
            sut.UserId.Should().BeNull();
        }
    }
}
