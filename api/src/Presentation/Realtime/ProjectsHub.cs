using Api.Auth.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Api.Realtime
{
    /// <summary>
    /// SignalR hub providing real-time collaboration features for project-related updates.
    /// Enforces <see cref="Policies.ProjectReader"/> authorization and manages per-project connection groups.
    /// </summary>
    [Authorize(Policy = Policies.ProjectReader)]
    public sealed class ProjectsHub : Hub
    {
        /// <summary>
        /// Executes logic when a client connects to the hub.
        /// </summary>
        public override async Task OnConnectedAsync()
            => await base.OnConnectedAsync();

        /// <summary>
        /// Computes the SignalR group name associated with a given project identifier.
        /// </summary>
        public static string GroupName(Guid projectId)
            => $"project:{projectId}";

        /// <summary>
        /// Joins the connection to the project group.
        /// </summary>
        public async Task JoinProject(Guid projectId)
            => await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(projectId));

        /// <summary>
        /// Leaves the project group.
        /// </summary>
        public async Task LeaveProject(Guid projectId)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(projectId));
    }
}
