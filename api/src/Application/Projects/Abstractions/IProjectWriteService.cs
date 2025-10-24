using Domain.Entities;
using Domain.Enums;
using Domain.ValueObjects;

namespace Application.Projects.Abstractions
{
    public interface IProjectWriteService
    {
        Task<(DomainMutation, Project?)> CreateAsync(Guid userId, ProjectName name, CancellationToken ct = default);
        Task<DomainMutation> RenameAsync(Guid projectId, ProjectName newName, byte[] rowVersion, CancellationToken ct = default);
        Task<DomainMutation> DeleteAsync(Guid projectId, byte[] rowVersion, CancellationToken ct = default);
    }
}
