using Api.Auth.Authorization;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Api.Tests.Auth.Authorization
{
    public sealed class UserRoleAuthorizationHandlerTests
    {
        private readonly UserRoleAuthorizationHandler _sut = new();

        // ---------- SUCCESS CASES ----------

        [Theory]
        [InlineData(UserRole.Admin, UserRole.Admin, ClaimTypes.Role)]      // meets minimum
        [InlineData(UserRole.Admin, UserRole.User, ClaimTypes.Role)]       // greater than minimum
        [InlineData(UserRole.Admin, UserRole.User, "role")]                // custom claim type
        public async Task Authorize_Succeeds_When_Role_Meets_Or_Exceeds_Minimum(
            UserRole userRole,
            UserRole minimumRole,
            string claimType)
        {
            var principal = BuildPrincipalFromEnum(userRole, claimType);
            var context = BuildContext(minimumRole, principal);

            await _sut.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task Authorize_Succeeds_With_Case_Insensitive_Role_Value()
        {
            var principal = BuildPrincipalFromRaw("admin", ClaimTypes.Role); // lower-case text
            var context = BuildContext(UserRole.Admin, principal);

            await _sut.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task Authorize_Succeeds_When_Multiple_Role_Claims_Contain_A_Sufficient_One()
        {
            var id = new ClaimsIdentity("test");
            id.AddClaim(new Claim(ClaimTypes.Role, UserRole.User.ToString()));
            id.AddClaim(new Claim(ClaimTypes.Role, UserRole.Admin.ToString())); // higher one present
            var principal = new ClaimsPrincipal(id);

            var context = BuildContext(UserRole.Admin, principal);

            await _sut.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        // ---------- FAILURE CASES ----------

        [Theory]
        [InlineData(UserRole.User, UserRole.Admin, ClaimTypes.Role)] // below minimum
        public async Task Authorize_Fails_When_Role_Is_Below_Minimum(
            UserRole userRole,
            UserRole minimumRole,
            string claimType)
        {
            var principal = BuildPrincipalFromEnum(userRole, claimType);
            var context = BuildContext(minimumRole, principal);

            await _sut.HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
        }

        [Theory]
        [InlineData(null, UserRole.User, ClaimTypes.Role)]  // missing claim
        [InlineData("invalid", UserRole.User, ClaimTypes.Role)] // unparsable text
        [InlineData("", UserRole.User, "role")] // empty value on custom type
        public async Task Authorize_Fails_With_Missing_Empty_Or_Invalid_Role_Claim(
            string? roleValue,
            UserRole minimumRole,
            string claimType)
        {
            var principal = roleValue is null
                ? new ClaimsPrincipal(new ClaimsIdentity("test")) // no claim
                : BuildPrincipalFromRaw(roleValue, claimType);

            var context = BuildContext(minimumRole, principal);

            await _sut.HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
        }

        // ---------- HELPERS ----------

        private static AuthorizationHandlerContext BuildContext(UserRole minimumRole, ClaimsPrincipal principal)
        {
            var requirement = new UserRoleRequirement(minimumRole);
            return new AuthorizationHandlerContext([requirement], principal, resource: new object());
        }

        private static ClaimsPrincipal BuildPrincipalFromEnum(UserRole role, string claimType)
        {
            var id = new ClaimsIdentity("test");
            id.AddClaim(new Claim(claimType, role.ToString()));
            return new ClaimsPrincipal(id);
        }

        private static ClaimsPrincipal BuildPrincipalFromRaw(string value, string claimType)
        {
            var id = new ClaimsIdentity("test");
            id.AddClaim(new Claim(claimType, value));
            return new ClaimsPrincipal(id);
        }
    }
}
