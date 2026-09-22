using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using InteractHub.Application.Interfaces;

namespace InteractHub.Infrastructure.Hubs;

[Authorize]
public class UserHub : Hub
{
    private readonly IUserService _userService;

    public UserHub(IUserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// User joins their personal hub for receiving updates
    /// This allows broadcasting profile changes to all connected clients
    /// </summary>
    public async Task JoinUserUpdatesGroup(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return;

        var currentUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
            return;

        // User can only join their own group
        if (currentUserId != userId)
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
        Console.WriteLine($"[UserHub] 📌 User {userId} joined personal updates group");
    }

    /// <summary>
    /// User leaves their personal hub
    /// </summary>
    public async Task LeaveUserUpdatesGroup(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return;

        var currentUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId) || currentUserId != userId)
            return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
        Console.WriteLine($"[UserHub] 📌 User {userId} left personal updates group");
    }

    /// <summary>
    /// Auto-join user to their personal group on connection
    /// Emit UserOnline event to all users
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
            Console.WriteLine($"[UserHub] ✅ User {userId} auto-joined on connection");

            // 📡 Emit UserOnline event to all clients
            await Clients.All.SendAsync("UserOnline", new { userId = userId, timestamp = DateTime.UtcNow });
            Console.WriteLine($"[UserHub] 🟢 Emitted UserOnline event for {userId}");
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// On disconnect, update LastActiveAt and emit UserOffline event
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userId))
        {
            try
            {
                // Update user's LastActiveAt to current UTC time
                var user = await _userService.GetByIdAsync(userId);
                if (user != null)
                {
                    user.LastActiveAt = DateTime.UtcNow;
                    await _userService.UpdateAsync(user);
                    Console.WriteLine($"[UserHub] ⏰ Updated LastActiveAt for user {userId}: {user.LastActiveAt}");
                }

                // 📡 Emit UserOffline event to all clients
                await Clients.All.SendAsync("UserOffline", new { userId = userId, lastActiveAt = DateTime.UtcNow, timestamp = DateTime.UtcNow });
                Console.WriteLine($"[UserHub] 🔴 Emitted UserOffline event for {userId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[UserHub] ❌ Error in OnDisconnected for user {userId}: {ex.Message}");
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Helper method to get consistent group name for a user
    /// Format: "user-updates-{userId}"
    /// </summary>
    public static string GetUserGroupName(string userId) => $"user-updates-{userId}";
}
