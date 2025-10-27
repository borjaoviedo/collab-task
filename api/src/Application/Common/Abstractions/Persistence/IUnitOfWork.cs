using Domain.Enums;

namespace Application.Common.Abstractions.Persistence
{
    public interface IUnitOfWork
    {
        Task<DomainMutation> SaveAsync(MutationKind kind, CancellationToken ct = default);
    }
}
