using Api.Auth.Authorization;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Api.Tests.Auth.Authorization
{
    public sealed class UserRoleAuthorizationHandlerTests
    {
        private static AuthorizationHandlerContext BuildContext(
            UserRole minimumRole,
            ClaimsPrincipal? user = null)
        {
            var requirement = new UserRoleRequirement(minimumRole);
            var principal = user ?? new ClaimsPrincipal(new ClaimsIdentity());
            var resource = new object();
            return new AuthorizationHandlerContext(new[] { requirement }, principal, resource);
        }

        private static ClaimsPrincipal BuildUser(UserRole? role, bool useClaimTypesRole = true)
        {
            var id = new ClaimsIdentity("test");
            if (role.HasValue)
            {
                var type = useClaimTypesRole ? ClaimTypes.Role : "role";
                id.AddClaim(new Claim(type, role.Value.ToString()));
            }
            return new ClaimsPrincipal(id);
        }

        [Fact]
        public async Task Succeeds_When_UserRole_Meets_Minimum()
        {
            var ctx = BuildContext(UserRole.Admin, BuildUser(UserRole.Admin));
            var handler = new UserRoleAuthorizationHandler();

            await handler.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task Succeeds_When_UserRole_Is_Greater_Than_Minimum()
        {
            var ctx = BuildContext(UserRole.User, BuildUser(UserRole.Admin));
            var handler = new UserRoleAuthorizationHandler();

            await handler.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task Fails_When_UserRole_Is_Lower_Than_Minimum()
        {
            var ctx = BuildContext(UserRole.Admin, BuildUser(UserRole.User));
            var handler = new UserRoleAuthorizationHandler();

            await handler.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task Fails_When_Missing_Role_Claim()
        {
            var ctx = BuildContext(UserRole.User, BuildUser(role: null));
            var handler = new UserRoleAuthorizationHandler();

            await handler.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task Succeeds_With_Custom_role_Claim_Type()
        {
            var ctx = BuildContext(UserRole.User, BuildUser(UserRole.Admin, useClaimTypesRole: false));
            var handler = new UserRoleAuthorizationHandler();

            await handler.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task Is_Case_Insensitive_On_Role_Value()
        {
            var id = new ClaimsIdentity("test");
            id.AddClaim(new Claim(ClaimTypes.Role, "admin")); // lower-case
            var principal = new ClaimsPrincipal(id);

            var ctx = new AuthorizationHandlerContext(
                new[] { new UserRoleRequirement(UserRole.Admin) }, principal, new object());

            var handler = new UserRoleAuthorizationHandler();
            await handler.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeTrue();
        }
    }
}
