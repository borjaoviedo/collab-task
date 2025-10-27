using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Api.Realtime
{
    public sealed class ProjectsHub : Hub
    {
        [Authorize]
        public override async Task OnConnectedAsync()
            => await base.OnConnectedAsync();

        public static string GroupName(Guid projectId)
            => $"project:{projectId}";

        public async Task JoinProject(Guid projectId)
            => await Groups.AddToGroupAsync(Context.ConnectionId, $"project:{projectId}");

        public async Task LeaveProject(Guid projectId)
            => await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project:{projectId}");
    }
}
