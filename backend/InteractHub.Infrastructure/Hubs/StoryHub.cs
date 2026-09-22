using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace InteractHub.Infrastructure.Hubs;

[Authorize]
public class StoryHub : Hub
{
    private static string UserStoryGroup(string userId) => $"story-user-{userId}";

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? Context.User?.FindFirst("sub")?.Value;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserStoryGroup(userId));
        }

        await base.OnConnectedAsync();
    }

    public async Task JoinStoriesFeed()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "stories-feed");
    }
}
