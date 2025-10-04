using Api.Auth.Authorization;
using FluentAssertions;

namespace Api.Tests.Auth.Authorization
{
    public sealed class PoliciesConstantsTests
    {
        [Fact]
        public void Policy_Names_Are_Stable()
        {
            Policies.ProjectReader.Should().Be("Reader");
            Policies.ProjectMember.Should().Be("Member");
            Policies.ProjectAdmin.Should().Be("Admin");
            Policies.ProjectOwner.Should().Be("Owner");
        }
    }
}
