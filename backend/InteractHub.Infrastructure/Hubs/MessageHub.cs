using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Linq;
using InteractHub.Application.Interfaces;

namespace InteractHub.Infrastructure.Hubs;

[Authorize]
public class MessageHub : Hub
{
    private readonly IFriendshipService _friendshipService;
    private readonly IGroupService _groupService;
    private readonly IUserPresenceService _userPresenceService;

    public MessageHub(
        IFriendshipService friendshipService,
        IGroupService groupService,
        IUserPresenceService userPresenceService)
    {
        _friendshipService = friendshipService;
        _groupService = groupService;
        _userPresenceService = userPresenceService;
    }
    /// <summary>
    /// User joins the conversation group for real-time messaging
    /// Group format: "conversation_{userId1}_{userId2}" (sorted to ensure consistency)
    /// </summary>
    public async Task JoinConversation(string userId)
    {
        Console.WriteLine($"[MessageHub] 🔍 JoinConversation called with userId: {userId}");
        
        if (string.IsNullOrEmpty(userId))
        {
            Console.WriteLine($"[MessageHub] ⚠️ JoinConversation: userId is empty");
            return;
        }

        var currentUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Console.WriteLine($"[MessageHub] 🔐 Current user from token: {currentUserId}");
        
        if (string.IsNullOrEmpty(currentUserId))
        {
            Console.WriteLine($"[MessageHub] ⚠️ JoinConversation: currentUserId is empty");
            return;
        }

        var friendshipStatus = await _friendshipService.CheckFriendshipStatusAsync(currentUserId, userId);
        if (friendshipStatus != InteractHub.Application.Entities.Enums.FriendshipStatus.Accepted)
        {
            Console.WriteLine($"[MessageHub] ⛔ JoinConversation denied. Users are not accepted friends.");
            return;
        }

        // Create a group name for the conversation (sorted IDs to ensure consistency)
        var groupName = GetConversationGroupName(currentUserId, userId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        Console.WriteLine($"[MessageHub] 📌 User {currentUserId} joined conversation group: {groupName}");
    }

    /// <summary>
    /// User joins a group conversation
    /// Group format: "group_{groupId}"
    /// </summary>
    public async Task JoinGroupConversation(int groupId)
    {
        Console.WriteLine($"[MessageHub] 🔍 JoinGroupConversation called with groupId: {groupId}");
        
        var currentUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Console.WriteLine($"[MessageHub] 🔐 Current user from token: {currentUserId}");
        
        if (string.IsNullOrEmpty(currentUserId))
        {
            Console.WriteLine($"[MessageHub] ⚠️ JoinGroupConversation: currentUserId is empty");
            return;
        }

        var group = await _groupService.GetByIdAsync(groupId);
        if (group == null || !group.Memberships.Any(m => m.UserId == currentUserId))
        {
            Console.WriteLine($"[MessageHub] ⛔ JoinGroupConversation denied for user {currentUserId}, group {groupId}");
            return;
        }

        var groupName = GetGroupConversationGroupName(groupId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        Console.WriteLine($"[MessageHub] 📌 User {currentUserId} joined group conversation: {groupName}");
    }

    /// <summary>
    /// User leaves the conversation group
    /// </summary>
    public async Task LeaveConversation(string userId)
    {
        if (string.IsNullOrEmpty(userId))
            return;

        var currentUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
            return;

        var groupName = GetConversationGroupName(currentUserId, userId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// User leaves a group conversation
    /// </summary>
    public async Task LeaveGroupConversation(int groupId)
    {
        var currentUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
            return;

        var groupName = GetGroupConversationGroupName(groupId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Called automatically when user connects
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Console.WriteLine($"[MessageHub] 🔌 User connected: {userId}, ConnectionId: {Context.ConnectionId}");
        
        if (!string.IsNullOrEmpty(userId))
        {
            // Add user to their own group so we can send them direct messages
            await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroupName(userId));
            Console.WriteLine($"[MessageHub] ✅ User {userId} added to group: {GetUserGroupName(userId)}");

            var becameOnline = _userPresenceService.UserConnected(userId);
            if (becameOnline)
            {
                // 🟢 Broadcast user online status to all friends
                try
                {
                    var friends = await _friendshipService.GetAcceptedFriendsAsync(userId);
                    foreach (var friendship in friends)
                    {
                        var friendId = friendship.UserId == userId ? friendship.FriendId : friendship.UserId;
                        if (!string.IsNullOrEmpty(friendId))
                        {
                            await Clients.Group(GetUserGroupName(friendId))
                                .SendAsync("UserOnline", new { UserId = userId });
                            Console.WriteLine($"[MessageHub] 🟢 Broadcasted {userId} is ONLINE to friend {friendId}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MessageHub] ⚠️ Error broadcasting online status: {ex.Message}");
                }
            }
        }
        else
        {
            Console.WriteLine($"[MessageHub] ⚠️ Failed to get userId from claims!");
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when user disconnects
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Console.WriteLine($"[MessageHub] 👋 User disconnected: {userId}, ConnectionId: {Context.ConnectionId}");
        
        if (exception != null)
        {
            Console.WriteLine($"[MessageHub] Error during disconnect: {exception.Message}");
        }
        
        // 🔴 Broadcast user offline status to all friends
        if (!string.IsNullOrEmpty(userId))
        {
            var (becameOffline, lastSeenAtUtc) = _userPresenceService.UserDisconnected(userId);
            if (becameOffline)
            {
                try
                {
                    var friends = await _friendshipService.GetAcceptedFriendsAsync(userId);
                    foreach (var friendship in friends)
                    {
                        var friendId = friendship.UserId == userId ? friendship.FriendId : friendship.UserId;
                        if (!string.IsNullOrEmpty(friendId))
                        {
                            await Clients.Group(GetUserGroupName(friendId))
                                .SendAsync("UserOffline", new { UserId = userId, LastSeenAt = lastSeenAtUtc });
                            Console.WriteLine($"[MessageHub] 🔴 Broadcasted {userId} is OFFLINE to friend {friendId}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[MessageHub] ⚠️ Error broadcasting offline status: {ex.Message}");
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Helper: Generate conversation group name (sorted to ensure consistency)
    /// </summary>
    private static string GetConversationGroupName(string userId1, string userId2)
    {
        var ids = new[] { userId1, userId2 }.OrderBy(x => x).ToArray();
        return $"conversation_{ids[0]}_{ids[1]}";
    }

    /// <summary>
    /// Helper: Generate group conversation group name
    /// </summary>
    private static string GetGroupConversationGroupName(int groupId) => $"group_{groupId}";

    /// <summary>
    /// Helper: Generate user group name for direct messages
    /// </summary>
    private static string GetUserGroupName(string userId) => $"user_{userId}";
}
