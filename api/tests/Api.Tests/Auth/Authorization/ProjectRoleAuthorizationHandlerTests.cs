using Api.Auth.Authorization;
using Api.Tests.Fakes;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Api.Tests.Auth.Authorization
{
    public sealed class ProjectRoleAuthorizationHandlerTests
    {
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

            var ctx = new AuthorizationHandlerContext([requirement], principal, http);
            return ctx;
        }

        private static ClaimsPrincipal BuildUser(Guid? userId = null)
        {
            var id = new ClaimsIdentity("test");
            if (userId is Guid uid)
            {
                id.AddClaim(new Claim("sub", uid.ToString()));
            }
            return new ClaimsPrincipal(id);
        }

        [Fact]
        public async Task Succeeds_When_Member_Role_Meets_Minimum()
        {
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var ctx = BuildContext(ProjectRole.Member, BuildUser(userId), projectId);

            var reader = new FakeProjectMemberReadService((pid, uid) => ProjectRole.Admin); // >= Member
            var handler = new ProjectRoleAuthorizationHandler(reader);

            await handler.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task Fails_When_Member_Role_Is_Lower_Than_Minimum()
        {
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var ctx= BuildContext(ProjectRole.Admin, BuildUser(userId), projectId);

            var reader = new FakeProjectMemberReadService((pid, uid) => ProjectRole.Member); // < Admin
            var handler = new ProjectRoleAuthorizationHandler(reader);

            await handler.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task Fails_When_User_Is_Not_Member()
        {
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var ctx = BuildContext(ProjectRole.Reader, BuildUser(userId), projectId);

            var reader = new FakeProjectMemberReadService((pid, uid) => null); // not in project
            var handler = new ProjectRoleAuthorizationHandler( reader);

            await handler.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task Fails_When_Missing_Sub_Claim()
        {
            var projectId = Guid.NewGuid();
            var ctx = BuildContext(ProjectRole.Reader, BuildUser(null), projectId);

            var reader = new FakeProjectMemberReadService((pid, uid) => ProjectRole.Owner);
            var handler = new ProjectRoleAuthorizationHandler( reader);

            await handler.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task Fails_When_Missing_ProjectId_In_Route()
        {
            var userId = Guid.NewGuid();
            var ctx = BuildContext(ProjectRole.Reader, BuildUser(userId), projectId: null);

            var reader = new FakeProjectMemberReadService((pid, uid) => ProjectRole.Owner);
            var handler = new ProjectRoleAuthorizationHandler( reader);

            await handler.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeFalse();
        }
    }
}
