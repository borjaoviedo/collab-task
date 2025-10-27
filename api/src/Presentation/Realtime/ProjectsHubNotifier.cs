using Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Api.Realtime
{
    public sealed class ProjectsHubNotifier(IHubContext<ProjectsHub> hub) : IRealtimeNotifier
    {
        public Task NotifyAsync<TPayload>(Guid projectId, RealtimeEvent<TPayload> evt, CancellationToken ct = default)
        {
            var message = new
            {
                type = evt.Type,
                projectId = evt.ProjectId,
                occurredAt = evt.OccurredAt,
                payload = evt.Payload
            };

            return hub.Clients.Group(ProjectsHub.GroupName(projectId)).SendAsync("board:event", message, ct);
        }
    }
}
