using Application.Common.Abstractions.Persistence;
using Application.Common.Exceptions;
using Application.Common.Results;
using Application.ProjectMembers.Abstractions;
using Domain.Entities;
using Domain.Enums;

namespace Application.ProjectMembers.Services
{
    public sealed class ProjectMemberService : IProjectMemberService
    {
        private readonly IProjectMemberRepository _repo;
        private readonly IUnitOfWork _uow;

        public ProjectMemberService(IProjectMemberRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task<WriteResult> AddAsync(Guid projectId, Guid userId, ProjectRole role, DateTimeOffset joinedAt, CancellationToken ct)
        {
            if (await _repo.ExistsAsync(projectId, userId, ct)) return WriteResult.NoOp;

            var member = ProjectMember.Create(projectId, userId, role, joinedAt);
            await _repo.AddAsync(member, ct);
            await _uow.SaveChangesAsync(ct);
            return WriteResult.Created;
        }

        public Task<WriteResult> ChangeRoleAsync(Guid projectId, Guid userId, ProjectRole newRole, byte[] rowVersion, CancellationToken ct)
            => ExecuteAsync(() => _repo.UpdateRoleAsync(projectId, userId, newRole, rowVersion, ct));

        public Task<WriteResult> RemoveAsync(Guid projectId, Guid userId, byte[] rowVersion, DateTimeOffset removedAt, CancellationToken ct)
            => ExecuteAsync(() => _repo.SetRemovedAsync(projectId, userId, removedAt, rowVersion, ct));

        public Task<WriteResult> RestoreAsync(Guid projectId, Guid userId, byte[] rowVersion, CancellationToken ct)
            => ExecuteAsync(() => _repo.SetRemovedAsync(projectId, userId, null, rowVersion, ct));

        private async Task<WriteResult> ExecuteAsync(Func<Task<DomainMutation>> op)
        {
            var m = await op();
            if (m is DomainMutation.NotFound or DomainMutation.NoOp) return m.ToWriteResult();

            try
            {
                var changes = await _uow.SaveChangesAsync();
                return changes > 0 ? m.ToWriteResult() : WriteResult.NoOp;
            }
            catch (ConcurrencyException)
            {
                return WriteResult.Conflict;
            }
        }
    }
}
