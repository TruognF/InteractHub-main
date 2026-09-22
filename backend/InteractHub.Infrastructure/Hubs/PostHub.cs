using System.Linq;
using System.Security.Claims;
using InteractHub.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class PostHub : Hub
{
    private readonly IGroupService _groupService;

    public PostHub(IGroupService groupService)
    {
        _groupService = groupService;
    }

    public async Task JoinFeed()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "feed");
    }

    public async Task JoinGroup(int groupId)
    {
        var currentUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
            return;

        var group = await _groupService.GetByIdAsync(groupId);
        if (group == null || !group.Memberships.Any(m => m.UserId == currentUserId))
            return;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");
    }

    public async Task LeaveGroup(int groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group_{groupId}");
    }
}