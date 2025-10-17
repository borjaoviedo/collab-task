
namespace Application.Realtime
{
    public interface IBoardNotifier
    {
        Task NotifyAsync<TPayload>(Guid projectId, BoardEvent<TPayload> evt, CancellationToken ct = default);
    }
}
