using Application.Users.Abstractions;
using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Users.Services
{
    public sealed class UserWriteService(IUserRepository repo) : IUserWriteService
    {
        public async Task<(DomainMutation, User?)> CreateAsync(string email, string name, byte[] hash, byte[] salt,
            UserRole role, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email))
                return (DomainMutation.NoOp, null);

            if (await repo.ExistsWithEmailAsync(email, ct: ct) || await repo.ExistsWithNameAsync(name, ct: ct))
                return (DomainMutation.Conflict, null);

            var user = User.Create(Email.Create(email), UserName.Create(name), hash, salt, role);

            await repo.AddAsync(user, ct);
            await repo.SaveChangesAsync(ct);
            return (DomainMutation.Created, user);
        }

        public async Task<DomainMutation> RenameAsync(Guid id, string newName, byte[] rowVersion, CancellationToken ct = default)
            => await repo.RenameAsync(id, newName, rowVersion, ct);

        public async Task<DomainMutation> ChangeRoleAsync(Guid id, UserRole newRole, byte[] rowVersion, CancellationToken ct = default)
            => await repo.ChangeRoleAsync(id, newRole, rowVersion, ct);

        public async Task<DomainMutation> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default)
            => await repo.DeleteAsync(id, rowVersion, ct);
    }
}
