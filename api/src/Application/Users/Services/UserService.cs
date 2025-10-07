using Application.Common.Abstractions.Persistence;
using Application.Common.Exceptions;
using Application.Common.Results;
using Application.Users.Abstractions;
using Domain.Entities;
using Domain.Enums;

namespace Application.Users.Services
{
    public sealed class UserService : IUserService
    {
        private readonly IUserRepository _repo;
        private readonly IUnitOfWork _uow;

        public UserService(IUserRepository repo, IUnitOfWork uow)
        {
            _repo = repo;
            _uow = uow;
        }

        public async Task<WriteResult> CreateAsync(User user, CancellationToken ct)
        {
            await _repo.AddAsync(user, ct);
            await _uow.SaveChangesAsync(ct);
            return WriteResult.Created;
        }

        public async Task<WriteResult> RenameAsync(Guid id, string newName, byte[] rowVersion, CancellationToken ct)
            => await ExecuteAsync(() => _repo.RenameAsync(id, newName, rowVersion, ct));

        public async Task<WriteResult> ChangeRoleAsync(Guid id, UserRole newRole, byte[] rowVersion, CancellationToken ct)
            => await ExecuteAsync(() => _repo.ChangeRoleAsync(id, newRole, rowVersion, ct));

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
