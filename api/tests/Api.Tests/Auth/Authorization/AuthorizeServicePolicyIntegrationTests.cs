using Api.Auth.Authorization;
using Api.Configuration;
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
            using var sp = BuildServiceProvider(userRole);
            var authz = sp.GetRequiredService<IAuthorizationService>();
            var http = BuildHttpContextWithUser();

            var policyName = MapPolicy(required);

            var result = await authz.AuthorizeAsync(http.User, http, policyName);

            result.Succeeded.Should().Be(expected);
        }

        // ---------- HELPERS ----------

        // Builds a minimal DI container with auth + policies and a stubbed role reader.
        private static ServiceProvider BuildServiceProvider(ProjectRole userRole)
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
            services.AddScoped<IProjectMemberReadService>(_ => new StubProjectMemberReadService(userRole));
            services.AddSecurity(cfg);

            return services.BuildServiceProvider();
        }

        // Creates an HttpContext with route projectId and authenticated user (sub).
        private static DefaultHttpContext BuildHttpContextWithUser()
        {
            var http = new DefaultHttpContext();
            http.Request.RouteValues["projectId"] = Guid.NewGuid().ToString();

            var id = new ClaimsIdentity("test");
            id.AddClaim(new Claim("sub", Guid.NewGuid().ToString()));
            http.User = new ClaimsPrincipal(id);

            return http;
        }

        // Maps required ProjectRole to policy name registered by AddJwtAuthAndPolicies.
        private static string MapPolicy(ProjectRole required) => required switch
        {
            ProjectRole.Reader => Policies.ProjectReader,
            ProjectRole.Member => Policies.ProjectMember,
            ProjectRole.Admin => Policies.ProjectAdmin,
            ProjectRole.Owner => Policies.ProjectOwner,
            _ => throw new ArgumentOutOfRangeException(
                nameof(required),
                required,
                "Invalid ProjectRole for policy mapping.")
        };

        // ---------- TEST DOUBLE ----------

        // Self-contained stub. No DB. Deterministic role for current user in any project.
        private sealed class StubProjectMemberReadService(ProjectRole role) : IProjectMemberReadService
        {
            private readonly ProjectRole _role = role;

            public Task<ProjectMember?> GetAsync(Guid projectId, Guid userId, CancellationToken ct = default)
                => Task.FromResult<ProjectMember?>(null);

            public Task<IReadOnlyList<ProjectMember>> ListByProjectAsync(
                Guid projectId,
                bool includeRemoved = false,
                CancellationToken ct = default)
                => Task.FromResult<IReadOnlyList<ProjectMember>>([]);

            public Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default)
                => Task.FromResult<ProjectRole?>(_role);

            public Task<int> CountActiveAsync(Guid userId, CancellationToken ct = default)
                => Task.FromResult(1);
        }
    }
}
