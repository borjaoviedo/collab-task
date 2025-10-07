using Application.Projects.Abstractions;
using Domain.Enums;

namespace Api.Tests.Fakes
{
    public sealed class FakeMembershipReader : IProjectMembershipReader
    {
        public Task<ProjectRole?> GetRoleAsync(Guid projectId, Guid userId, CancellationToken ct = default)
            => Task.FromResult<ProjectRole?>(ProjectRole.Owner);

        public Task<int> CountActiveAsync(Guid userId, CancellationToken ct = default)
            => Task.FromResult(1);
    }
}
