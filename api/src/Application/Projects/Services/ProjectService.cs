using Application.Common.Abstractions.Persistence;
using Application.Common.Exceptions;
using Application.Common.Results;
using Application.Projects.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Projects.Services
{
    public sealed class ProjectService : IProjectService
    {
        private readonly IProjectRepository _repo;
        private readonly IUnitOfWork _uow;

        public ProjectService(IProjectRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task<(WriteResult Result, Guid Id)> CreateAsync(Guid ownerId, string name, DateTimeOffset now, CancellationToken ct)
        {
            var project = Project.Create(ownerId, ProjectName.Create(name), now);
            await _repo.AddAsync(project, ct);
            await _uow.SaveChangesAsync(ct);
            return (WriteResult.Created, project.Id);
        }

        public async Task<WriteResult> RenameAsync(Guid id, string newName, byte[] rowVersion, CancellationToken ct)
            => await ExecuteAsync(() => _repo.RenameAsync(id, newName, rowVersion, ct));

        public async Task<WriteResult> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct)
            => await ExecuteAsync(() => _repo.DeleteAsync(id, rowVersion, ct));

        private async Task<WriteResult> ExecuteAsync(Func<Task<DomainMutation>> op)
        {
            var mutation = await op();
            if (mutation is DomainMutation.NotFound or DomainMutation.NoOp)
                return mutation.ToWriteResult();

            try
            {
                var changes = await _uow.SaveChangesAsync();
                return changes > 0 ? mutation.ToWriteResult() : WriteResult.NoOp;
            }
            catch (ConcurrencyException)
            {
                return WriteResult.Conflict;
            }
        }
    }
}
