using Api.Auth.Authorization;
using Application.Projects.Abstractions;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace Api.Tests.Auth.Authorization
{
    public sealed class ProjectRoleAuthorizationHandlerTests
    {
        private static (AuthorizationHandlerContext Ctx, DefaultHttpContext Http) BuildContext(
            ProjectRole minimumRole,
            ClaimsPrincipal? user = null,
            Guid? projectId = null)
        {
            var requirement = new ProjectRoleRequirement(minimumRole);
            var resource = new object();
            var principal = user ?? new ClaimsPrincipal(new ClaimsIdentity());
            var ctx = new AuthorizationHandlerContext(new[] { requirement }, principal, resource);

            var http = new DefaultHttpContext();
            var rd = new RouteData();
            if (projectId is Guid pid) rd.Values["projectId"] = pid.ToString();
            http.SetEndpoint(new Endpoint(c => default!, new EndpointMetadataCollection(), "test"));
            http.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = rd });

            return (ctx, http);
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

        private sealed class FakeMembershipReader : IProjectMembershipReader
        {
            private readonly ProjectRole? _role;
            private readonly Func<Guid, Guid, ProjectRole?>? _selector;

            public FakeMembershipReader(ProjectRole? role) => _role = role;
            public FakeMembershipReader(Func<Guid, Guid, ProjectRole?> selector) => _selector = selector;

            public Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default)
                => Task.FromResult(_selector is null ? _role : _selector(projectId, userId));

            public Task<int> CountActiveAsync(Guid userId, CancellationToken ct = default)
                => Task.FromResult(1);
        }

        [Fact]
        public async Task Succeeds_When_Member_Role_Meets_Minimum()
        {
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var (ctx, http) = BuildContext(ProjectRole.Member, BuildUser(userId), projectId);

            var accessor = new HttpContextAccessor { HttpContext = http };
            var reader = new FakeMembershipReader((pid, uid) => ProjectRole.Admin); // >= Member
            var handler = new ProjectRoleAuthorizationHandler(accessor, reader);

            await handler.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeTrue();
        }

        [Fact]
        public async Task Fails_When_Member_Role_Is_Lower_Than_Minimum()
        {
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var (ctx, http) = BuildContext(ProjectRole.Admin, BuildUser(userId), projectId);

            var accessor = new HttpContextAccessor { HttpContext = http };
            var reader = new FakeMembershipReader((pid, uid) => ProjectRole.Member); // < Admin
            var handler = new ProjectRoleAuthorizationHandler(accessor, reader);

            await handler.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task Fails_When_User_Is_Not_Member()
        {
            var userId = Guid.NewGuid();
            var projectId = Guid.NewGuid();
            var (ctx, http) = BuildContext(ProjectRole.Reader, BuildUser(userId), projectId);

            var accessor = new HttpContextAccessor { HttpContext = http };
            var reader = new FakeMembershipReader((pid, uid) => null); // not in project
            var handler = new ProjectRoleAuthorizationHandler(accessor, reader);

            await handler.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task Fails_When_Missing_Sub_Claim()
        {
            var projectId = Guid.NewGuid();
            var (ctx, http) = BuildContext(ProjectRole.Reader, BuildUser(null), projectId);

            var accessor = new HttpContextAccessor { HttpContext = http };
            var reader = new FakeMembershipReader((pid, uid) => ProjectRole.Owner);
            var handler = new ProjectRoleAuthorizationHandler(accessor, reader);

            await handler.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeFalse();
        }

        [Fact]
        public async Task Fails_When_Missing_ProjectId_In_Route()
        {
            var userId = Guid.NewGuid();
            var (ctx, http) = BuildContext(ProjectRole.Reader, BuildUser(userId), projectId: null);

            var accessor = new HttpContextAccessor { HttpContext = http };
            var reader = new FakeMembershipReader((pid, uid) => ProjectRole.Owner);
            var handler = new ProjectRoleAuthorizationHandler(accessor, reader);

            await handler.HandleAsync(ctx);

            ctx.HasSucceeded.Should().BeFalse();
        }
    }
}
