using Application.Common.Abstractions.Persistence;
using Application.Common.Results;
using Application.ProjectMembers.Abstractions;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.ProjectMembers.Services
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

        public async Task<WriteResult> UpdateRoleAsync(
            Guid projectId, Guid userId, ProjectRole role, byte[] rowVersion, CancellationToken ct = default)
        {
            var mutation = await _repo.UpdateRoleAsync(projectId, userId, role, rowVersion, ct);

            if (mutation == DomainMutation.NotFound)
                return new WriteResult(DataWriteConflict.NotFound, null);
            if (mutation == DomainMutation.NoOp)
                return new WriteResult(DataWriteConflict.None, null);

            try
            {
                await _uow.SaveChangesAsync(ct);
                var refreshed = await _repo.GetAsync(projectId, userId, ct);
                return new WriteResult(DataWriteConflict.None, refreshed?.RowVersion);
            }
            catch (DbUpdateConcurrencyException)
            {
                return new WriteResult(DataWriteConflict.Concurrency, null);
            }
        }

        public async Task<WriteResult> RemoveAsync(
            Guid projectId, Guid userId, DateTimeOffset? removedAt, byte[] rowVersion, CancellationToken ct = default)
        {
            var mutation = await _repo.SetRemovedAsync(projectId, userId, removedAt, rowVersion, ct);

            if (mutation == DomainMutation.NotFound)
                return new WriteResult(DataWriteConflict.NotFound, null);
            if (mutation == DomainMutation.NoOp)
                return new WriteResult(DataWriteConflict.None, null);

            try
            {
                await _uow.SaveChangesAsync(ct);
                var refreshed = await _repo.GetAsync(projectId, userId, ct);
                return new WriteResult(DataWriteConflict.None, refreshed?.RowVersion);
            }
            catch (DbUpdateConcurrencyException)
            {
                return new WriteResult(DataWriteConflict.Concurrency, null);
            }
        }
    }
}
