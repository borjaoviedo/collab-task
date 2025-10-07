using Api.Auth.Authorization;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Api.Tests.Auth.Authorization
{
    public sealed class UserRoleAuthorizationHandlerTests
    {
        private static AuthorizationHandlerContext Ctx(UserRole min, ClaimsPrincipal user)
        => new(new[] { new UserRoleRequirement(min) }, user, new object());

        private static ClaimsPrincipal UserWithRoleClaim(string value, bool useStd = true)
        {
            var id = new ClaimsIdentity("test");
            id.AddClaim(new Claim(useStd ? ClaimTypes.Role : "role", value));
            return new ClaimsPrincipal(id);
        }

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

        [Fact]
        public async Task Fails_When_Role_Claim_Is_Invalid_Text()
        {
            var ctx = Ctx(UserRole.User, UserWithRoleClaim("superuser"));
            var h = new UserRoleAuthorizationHandler();

            await h.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task Fails_When_Role_Claim_Empty_Custom_Type()
        {
            var id = new ClaimsIdentity("test");
            id.AddClaim(new Claim("role", "")); // empty
            var ctx = Ctx(UserRole.User, new ClaimsPrincipal(id));
            var h = new UserRoleAuthorizationHandler();

            await h.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task Succeeds_When_Multiple_Role_Claims_Contain_Highest_Enough()
        {
            var id = new ClaimsIdentity("test");
            id.AddClaim(new Claim(ClaimTypes.Role, UserRole.User.ToString()));
            id.AddClaim(new Claim(ClaimTypes.Role, UserRole.Admin.ToString())); // higher
            var ctx = Ctx(UserRole.Admin, new ClaimsPrincipal(id));
            var h = new UserRoleAuthorizationHandler();

            await h.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeTrue();
        }
    }
}
