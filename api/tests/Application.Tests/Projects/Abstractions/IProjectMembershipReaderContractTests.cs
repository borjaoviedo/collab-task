using Application.Projects.Abstractions;
using Domain.Enums;
using FluentAssertions;

namespace Application.Tests.Projects.Abstractions
{
    public sealed class IProjectMembershipReaderContractTests
    {
        private sealed class FakeReader : IProjectMembershipReader
        {
            public Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default)
                => Task.FromResult<ProjectRole?>(ProjectRole.Member);
        }

        [Fact]
        public async Task Returns_Nullable_Role()
        {
            IProjectMembershipReader reader = new FakeReader();
            var role = await reader.GetRoleAsync(Guid.NewGuid(), Guid.NewGuid());
            role.HasValue.Should().BeTrue();
        }
    }
}
