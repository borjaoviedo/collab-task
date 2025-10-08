using Application.Common.Results;

namespace Application.Projects.Abstractions
{
    public interface IProjectService
    {
        Task<(WriteResult Result, Guid Id)> CreateAsync(Guid ownerId, string name, DateTimeOffset now, CancellationToken ct);
        Task<WriteResult> RenameAsync(Guid id, string newName, byte[] rowVersion, CancellationToken ct);
        Task<WriteResult> DeleteAsync(Guid id, byte[] rowVersion, CancellationToken ct);
    }
}
