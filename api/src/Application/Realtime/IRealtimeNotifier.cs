
namespace Application.Realtime
{
    public interface IRealtimeNotifier
    {
        Task NotifyAsync<TPayload>(Guid projectId, RealtimeEvent<TPayload> evt, CancellationToken ct = default);
    }
}
