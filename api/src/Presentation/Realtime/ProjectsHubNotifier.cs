using Application.Realtime;
using Microsoft.AspNetCore.SignalR;

namespace Api.Realtime
{
    /// <summary>
    /// Real-time notifier implementation that broadcasts project-related events to connected clients.
    /// Uses <see cref="ProjectsHub"/> to send structured messages to the SignalR group for the specified project.
    /// </summary>
    public sealed class ProjectsHubNotifier(IHubContext<ProjectsHub> hub) : IRealtimeNotifier
    {
        /// <summary>
        /// Sends a typed real-time event to all clients connected to the project’s SignalR group.
        /// Wraps the event data in a transport object containing type, project ID, timestamp, and payload.
        /// </summary>
        /// <typeparam name="TPayload">The payload type of the event.</typeparam>
        /// <param name="projectId">The project identifier used to select the group.</param>
        /// <param name="evt">The event to broadcast.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A task representing the asynchronous send operation.</returns>
        public Task NotifyAsync<TPayload>(
            Guid projectId,
            RealtimeEvent<TPayload> evt,
            CancellationToken ct = default)
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
