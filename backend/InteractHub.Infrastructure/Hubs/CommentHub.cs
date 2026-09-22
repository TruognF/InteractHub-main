using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace InteractHub.Infrastructure.Hubs;

[Authorize]
public class CommentHub : Hub
{
    public async Task JoinPostGroup(int postId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(postId));
    }

    public async Task LeavePostGroup(int postId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(postId));
    }

    private static string GetGroupName(int postId) => $"comments-{postId}";
}
