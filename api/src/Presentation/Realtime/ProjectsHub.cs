using Api.Auth.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Api.Realtime
{
    [Authorize(Policy = Policies.ProjectReader)]
    public sealed class ProjectsHub : Hub
    {
        public override async Task OnConnectedAsync()
            => await base.OnConnectedAsync();

        public static string GroupName(Guid projectId)
            => $"project:{projectId}";

        public async Task JoinProject(Guid projectId)
            => await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(projectId));

        public async Task LeaveProject(Guid projectId)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(projectId));
    }
}
