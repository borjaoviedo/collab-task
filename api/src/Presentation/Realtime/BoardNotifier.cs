using Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Api.Realtime
{
    public sealed class BoardNotifier(IHubContext<BoardHub> hub) : IBoardNotifier
    {
        public Task NotifyAsync<TPayload>(Guid projectId, BoardEvent<TPayload> evt, CancellationToken ct = default)
            => hub.Clients.Group($"project:{projectId}")
                  .SendAsync("board:event", new
                  {
                      type = evt.Type,
                      projectId = evt.ProjectId,
                      occurredAt = evt.OccurredAt,
                      payload = evt.Payload
                  }, ct);
    }
}
