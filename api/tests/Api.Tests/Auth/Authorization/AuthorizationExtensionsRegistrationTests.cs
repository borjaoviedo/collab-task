using Api.Auth.Authorization;
using Api.Extensions;
using Api.Tests.Fakes;
using Application.Projects.Abstractions;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Tests.Auth.Authorization
{
    public sealed class AuthorizationExtensionsRegistrationTests
    {
        [Fact]
        public void Registers_Handler_As_Scoped_And_Policies_Exist()
        {
            var services = new ServiceCollection();

            // Minimal JWT configuration for AddJwtAuthAndPolicies
            var cfg = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Jwt:Issuer"] = "test-issuer",
                    ["Jwt:Audience"] = "test-audience",
                    ["Jwt:Key"] = "0123456789ABCDEF0123456789ABCDEF"
                })
                .Build();

            // Fake membership reader so DI is satisfied
            services.AddScoped<IProjectMembershipReader, FakeMembershipReader>();

            services.AddJwtAuthAndPolicies(cfg);

            // Assert handler lifetime is scoped
            services.Any(sd =>
                    sd.ServiceType == typeof(IAuthorizationHandler) &&
                    sd.ImplementationType == typeof(ProjectRoleAuthorizationHandler) &&
                    sd.Lifetime == ServiceLifetime.Scoped)
                .Should().BeTrue();

            var sp = services.BuildServiceProvider();
            var provider = sp.GetRequiredService<IAuthorizationPolicyProvider>();

            provider.GetPolicyAsync(Policies.ProjectReader).Should().NotBeNull();
            provider.GetPolicyAsync(Policies.ProjectMember).Should().NotBeNull();
            provider.GetPolicyAsync(Policies.ProjectAdmin).Should().NotBeNull();
            provider.GetPolicyAsync(Policies.ProjectOwner).Should().NotBeNull();
            provider.GetPolicyAsync(Policies.SystemAdmin).Should().NotBeNull();
        }
    }
}
