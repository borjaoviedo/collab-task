using Application.Users.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Users.Services
{
    public sealed class UserWriteService(IUserRepository repo) : IUserWriteService
    {
        public async Task<(DomainMutation, User?)> CreateAsync(
            Email email,
            UserName name,
            byte[] hash,
            byte[] salt,
            UserRole role,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email)) return (DomainMutation.NoOp, null);

            var userExists = await repo.ExistsWithEmailAsync(email, excludeUserId: null, ct) || await repo.ExistsWithNameAsync(name, excludeUserId: null, ct);
            if (userExists) return (DomainMutation.Conflict, null);

            var user = User.Create(email, name, hash, salt, role);
            await repo.AddAsync(user, ct);

            try
            {
                await repo.SaveChangesAsync(ct);
                return (DomainMutation.Created, user);
            }
            catch
            {
                return (DomainMutation.Conflict, null);
            }
        }

        public async Task<DomainMutation> RenameAsync(Guid id, UserName newName, byte[] rowVersion, CancellationToken ct = default)
            => await repo.RenameAsync(id, newName, rowVersion, ct);

        public async Task<DomainMutation> ChangeRoleAsync(Guid id, UserRole newRole, byte[] rowVersion, CancellationToken ct = default)
            => await repo.ChangeRoleAsync(id, newRole, rowVersion, ct);

        public async Task<DomainMutation> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
            => await repo.DeleteAsync(id, rowVersion, ct);
    }
}
