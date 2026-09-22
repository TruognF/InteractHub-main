using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace InteractHub.Infrastructure.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    /// <summary>
    /// User joins their notifications group for receiving realtime updates
    /// This allows receiving friend requests, notifications, etc.
    /// </summary>
    public async Task JoinNotificationsGroup(string userId)
    {
        var currentUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(currentUserId));
        Console.WriteLine($"[NotificationHub] 📌 User {currentUserId} joined notifications group");
    }

    /// <summary>
    /// User leaves their notifications group
    /// </summary>
    public async Task LeaveNotificationsGroup(string userId)
    {
        var currentUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(currentUserId));
        Console.WriteLine($"[NotificationHub] 📌 User {currentUserId} left notifications group");
    }

    /// <summary>
    /// Auto-join user to their notifications group on connection
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(userId));
            Console.WriteLine($"[NotificationHub] ✅ User {userId} auto-joined notifications on connection");
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Helper method to get consistent group name for notifications
    /// Format: "notifications-{userId}"
    /// </summary>
    private static string GetGroupName(string userId) => $"notifications-{userId}";
}
