
namespace Application.Realtime
{
    /// <summary>
    /// Dispatches real-time events to all connected clients of a given project.
    /// </summary>
    public interface IRealtimeNotifier
    {
        /// <summary>
        /// Sends a real-time event with a typed payload to all clients subscribed to the project.
        /// </summary>
        /// <typeparam name="TPayload">The type of the event payload.</typeparam>
        /// <param name="projectId">The project identifier that scopes the broadcast.</param>
        /// <param name="evt">The event instance containing metadata and payload.</param>
        /// <param name="ct">Cancellation token.</param>
        Task NotifyAsync<TPayload>(Guid projectId, RealtimeEvent<TPayload> evt, CancellationToken ct = default);
    }
}
