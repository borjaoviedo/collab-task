using Application.Common.Results;
using Domain.Entities;
using Domain.Enums;

namespace Application.Users.Abstractions
{
    public interface IUserService
    {
        Task<WriteResult> CreateAsync(User user, CancellationToken ct);
        Task<WriteResult> RenameAsync(Guid id, string newName, byte[] rowVersion, CancellationToken ct);
        Task<WriteResult> ChangeRoleAsync(Guid id, UserRole newRole, byte[] rowVersion, CancellationToken ct);
        Task<WriteResult> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct);
    }
}
