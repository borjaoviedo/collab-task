using Application.Users.Abstractions;
using Domain.Entities;
using Domain.ValueObjects;

namespace Application.Users.Services
{
    public sealed class UserReadService(IUserRepository repo) : IUserReadService
    {
        public async Task<User?> GetAsync(Guid userId, CancellationToken ct = default)
            => await repo.GetByIdAsync(userId, ct);
        public async Task<User?> GetByEmailAsync(Email email, CancellationToken ct = default)
            => await repo.GetByEmailAsync(email, ct);
        public async Task<IReadOnlyList<User>> ListAsync(CancellationToken ct = default)
            => await repo.ListAsync(ct);
    }
}
