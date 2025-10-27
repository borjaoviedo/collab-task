using Api.Auth.Authorization;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using TestHelpers.Api.Fakes;

namespace Api.Tests.Auth.Authorization
{
    public sealed class ProjectRoleAuthorizationHandlerTests
    {
        private readonly Guid _userId = Guid.NewGuid();
        private readonly Guid _projectId = Guid.NewGuid();

        // ---------- SUCCESS CASES ----------

        [Theory]
        [InlineData(ProjectRole.Member, ProjectRole.Member)] // meets minimum
        [InlineData(ProjectRole.Admin, ProjectRole.Member)]  // greater than minimum
        [InlineData(ProjectRole.Owner, ProjectRole.Admin)]   // greater than minimum
        [InlineData(ProjectRole.Admin, ProjectRole.Reader)]  // greater than minimum
        public async Task Authorize_Succeeds_When_Member_Role_Meets_Or_Exceeds_Minimum(
            ProjectRole memberRole,
            ProjectRole minimumRole)
        {
            var context = BuildContext(minimumRole, BuildUser(_userId), _projectId);
            var handler = BuildHandler(() => memberRole);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeTrue();
        }

        // ---------- FAILURE CASES ----------

        [Theory]
        [InlineData(ProjectRole.Member, ProjectRole.Admin)] // below minimum
        [InlineData(ProjectRole.Reader, ProjectRole.Member)] // below minimum
        [InlineData(ProjectRole.Reader, ProjectRole.Admin)] // below minimum
        public async Task Authorize_Fails_When_Member_Role_Is_Lower_Than_Minimum(
            ProjectRole memberRole,
            ProjectRole minimumRole)
        {
            var context = BuildContext(minimumRole, BuildUser(_userId), _projectId);
            var handler = BuildHandler(() => memberRole);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task Authorize_Fails_When_User_Is_Not_Member()
        {
            var context = BuildContext(ProjectRole.Reader, BuildUser(_userId), _projectId);
            var handler = BuildHandler(() => null); // not in project

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task Authorize_Fails_When_Missing_Sub_Claim()
        {
            var context = BuildContext(ProjectRole.Reader, BuildUser(userId: null), _projectId);
            var handler = BuildHandler(() => ProjectRole.Owner);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task Authorize_Fails_When_Missing_ProjectId_In_Route()
        {
            var context = BuildContext(ProjectRole.Reader, BuildUser(_userId), projectId: null);
            var handler = BuildHandler(() => ProjectRole.Owner);

            await handler.HandleAsync(context);

            context.HasSucceeded.Should().BeFalse();
        }

        // ---------- HELPERS ----------

        private static ProjectRoleAuthorizationHandler BuildHandler(Func<ProjectRole?> resolveRole)
        {
            var reader = new FakeProjectMemberReadService((pid, uid) => resolveRole());
            return new ProjectRoleAuthorizationHandler(reader);
        }

        private static AuthorizationHandlerContext BuildContext(
            ProjectRole minimumRole,
            ClaimsPrincipal? user = null,
            Guid? projectId = null)
        {
            var requirement = new ProjectRoleRequirement(minimumRole);
            var principal = user ?? new ClaimsPrincipal(new ClaimsIdentity());
            var http = new DefaultHttpContext();

            if (projectId is Guid pid)
                http.Request.RouteValues["projectId"] = pid.ToString();

            return new AuthorizationHandlerContext([requirement], principal, http);
        }

        private static ClaimsPrincipal BuildUser(Guid? userId)
        {
            var id = new ClaimsIdentity("test");
            if (userId is Guid uid)
                id.AddClaim(new Claim("sub", uid.ToString()));
            return new ClaimsPrincipal(id);
        }
    }
}
