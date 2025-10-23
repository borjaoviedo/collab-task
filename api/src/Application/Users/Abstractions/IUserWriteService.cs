using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Users.Abstractions
{
    public interface IUserWriteService
    {
        Task<(DomainMutation, User?)> CreateAsync(
            Email email,
            UserName name,
            byte[] hash,
            byte[] salt,
            UserRole role,
            CancellationToken ct = default);
        Task<DomainMutation> RenameAsync(Guid id, UserName newName, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> ChangeRoleAsync(Guid id, UserRole newRole, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default);
    }
}
