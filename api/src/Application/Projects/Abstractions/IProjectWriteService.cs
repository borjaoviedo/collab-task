using Domain.Entities;
using Domain.Enums;

namespace Application.Projects.Abstractions
{
    public interface IProjectWriteService
    {
        Task<(DomainMutation, Project?)> CreateAsync(Guid ownerId, string name, DateTimeOffset now, CancellationToken ct = default);
        Task<DomainMutation> RenameAsync(Guid id, string newName, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct = default);
    }
}
