using Application.Common.Abstractions.Extensions;
using Application.Common.Abstractions.Persistence;
using Application.Users.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Users.Services
{
    public sealed class UserWriteService(IUserRepository repo, IUnitOfWork uow) : IUserWriteService
    {
        public async Task<(DomainMutation, User?)> CreateAsync(
            Email email,
            UserName name,
            byte[] hash,
            byte[] salt,
            UserRole role,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                return (DomainMutation.NoOp, null);

            var existsWithEmail = await repo.ExistsWithEmailAsync(email, excludeUserId: null, ct);
            var existsWithName = await repo.ExistsWithNameAsync(name, excludeUserId: null, ct);
            var exists = existsWithEmail || existsWithName;

            if (exists) return (DomainMutation.Conflict, null);

            var user = User.Create(email, name, hash, salt, role);
            await repo.AddAsync(user, ct);

            var createResult = await uow.SaveAsync(MutationKind.Create, ct);
            return (createResult, user);
        }

        public async Task<DomainMutation> RenameAsync(Guid id, UserName newName, byte[] rowVersion, CancellationToken ct = default)
        {
            var rename = await repo.RenameAsync(id, newName, rowVersion, ct);
            if (rename != PrecheckStatus.Ready) return rename.ToErrorDomainMutation();

            var updateResult = await uow.SaveAsync(MutationKind.Update, ct);
            return updateResult;
        }

        public async Task<DomainMutation> ChangeRoleAsync(Guid id, UserRole newRole, byte[] rowVersion, CancellationToken ct = default)
        {
            var changeRole = await repo.ChangeRoleAsync(id, newRole, rowVersion, ct);
            if (changeRole != PrecheckStatus.Ready) return changeRole.ToErrorDomainMutation();

            var updateResult = await uow.SaveAsync(MutationKind.Update, ct);
            return updateResult;
        }

        public async Task<DomainMutation> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
        {
            var delete = await repo.DeleteAsync(id, rowVersion, ct);
            if (delete != PrecheckStatus.Ready) return delete.ToErrorDomainMutation();

            var deleteResult = await uow.SaveAsync(MutationKind.Delete, ct);
            return deleteResult;
        }
    }
}
