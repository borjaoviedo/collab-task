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
        public void WhenAuthenticated_ExposesUserData()
        {
            var userId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
            var ctx = BuildContext(userId, "user@demo.com", "Admin");

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(ctx);

            var sut = new CurrentUserService(accessor.Object);

            sut.IsAuthenticated.Should().BeTrue();
            sut.UserId.Should().Be(userId);
            sut.Email.Should().Be("user@demo.com");
            sut.Role.Should().Be("Admin");
        }

        [Fact]
        public void WhenNoPrincipal_IsAuthenticatedFalse_AndNullData()
        {
            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext());

            var sut = new CurrentUserService(accessor.Object);

            sut.IsAuthenticated.Should().BeFalse();
            sut.UserId.Should().BeNull();
            sut.Email.Should().BeNull();
            sut.Role.Should().BeNull();
        }

        [Fact]
        public void MissingRequiredClaims_IsAuthenticatedTrue_ButNullForMissingOnes()
        {
            var identity = new ClaimsIdentity(authenticationType: "Test");
            identity.AddClaim(new Claim(ClaimTypes.Email, "user@demo.com"));

            var principal = new ClaimsPrincipal(identity);
            var ctx = new DefaultHttpContext { User = principal };

            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(ctx);

            var sut = new CurrentUserService(accessor.Object);

            sut.IsAuthenticated.Should().BeTrue();
            sut.UserId.Should().BeNull();
            sut.Email.Should().Be("user@demo.com");
            sut.Role.Should().BeNull();
        }
    }
}
