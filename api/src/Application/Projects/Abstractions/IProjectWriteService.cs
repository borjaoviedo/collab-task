using Domain.Entities;
using Domain.Enums;

namespace Application.Projects.Abstractions
{
    public interface IProjectWriteService
    {
        Task<(DomainMutation, Project?)> CreateAsync(Guid userId, string name, CancellationToken ct = default);
        Task<DomainMutation> RenameAsync(Guid projectId, string newName, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> DeleteAsync(Guid projectId, byte[] rowVersion, CancellationToken ct = default);
    }
}
