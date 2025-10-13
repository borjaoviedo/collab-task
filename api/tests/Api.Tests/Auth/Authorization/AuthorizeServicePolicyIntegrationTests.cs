using Api.Auth.Authorization;
using Api.Extensions;
using Application.ProjectMembers.Abstractions;
using Domain.Entities;
using Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;

namespace Api.Tests.Auth.Authorization
{
    public sealed class AuthorizeServicePolicyIntegrationTests
    {
        [Theory]
        [InlineData(ProjectRole.Reader, ProjectRole.Reader, true)]
        [InlineData(ProjectRole.Member, ProjectRole.Reader, true)]
        [InlineData(ProjectRole.Member, ProjectRole.Admin, false)]
        [InlineData(ProjectRole.Owner, ProjectRole.Owner, true)]
        public async Task Policies_Enforce_Minimum_Role(ProjectRole userRole, ProjectRole required, bool expected)
        {
            var services = new ServiceCollection();

            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "test-issuer",
                    ["Jwt:Audience"] = "test-audience",
                    ["Jwt:Key"] = "0123456789ABCDEF0123456789ABCDEF"
                })
                .Build();

            services.AddLogging();
            // Inject fake reader that returns the role for current user
            services.AddScoped<IProjectMemberReadService>(_ => new StubProjectMemberReadService(userRole));
            services.AddJwtAuthAndPolicies(cfg);

            var sp = services.BuildServiceProvider();
            var authz = sp.GetRequiredService<IAuthorizationService>();

            var http = new DefaultHttpContext();
            http.Request.RouteValues["projectId"] = Guid.NewGuid().ToString();

            var id = new ClaimsIdentity("test");
            id.AddClaim(new Claim("sub", Guid.NewGuid().ToString()));
            http.User = new ClaimsPrincipal(id);

            var policyName = required switch
            {
                ProjectRole.Reader => Policies.ProjectReader,
                ProjectRole.Member => Policies.ProjectMember,
                ProjectRole.Admin => Policies.ProjectAdmin,
                ProjectRole.Owner => Policies.ProjectOwner,
                _ => throw new ArgumentOutOfRangeException(nameof(required), required, "ProjectRole required value is not valid for the policy.")
            };

            var result = await authz.AuthorizeAsync(http.User, http, policyName);
            result.Succeeded.Should().Be(expected);
        }

        private sealed class StubProjectMemberReadService : IProjectMemberReadService
        {
            private readonly ProjectRole _role;

            public StubProjectMemberReadService(ProjectRole role) => _role = role;

            public Task<ProjectMember?> GetAsync(Guid projectId, Guid userId, CancellationToken ct = default)
                => Task.FromResult<ProjectMember?>(null);

            public Task<IReadOnlyList<ProjectMember>> ListByProjectAsync(Guid projectId, bool includeRemoved = false, CancellationToken ct = default)
                => Task.FromResult<IReadOnlyList<ProjectMember>>(Array.Empty<ProjectMember>());

            public Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default)
                => Task.FromResult<ProjectRole?>(_role);

            public Task<int> CountActiveAsync(Guid userId, CancellationToken ct = default)
                => Task.FromResult(1);
        }
    }
}
