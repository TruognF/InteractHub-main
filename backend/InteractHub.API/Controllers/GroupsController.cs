using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InteractHub.Application.Interfaces;
using InteractHub.API.DTOs;
using InteractHub.API.Extensions;
using InteractHub.Application.Entities;
using InteractHub.API.DTOs.Response;
using Microsoft.AspNetCore.SignalR;
using InteractHub.Infrastructure.Hubs;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly IGroupService _groupService;
    private readonly IHubContext<GroupHub> _groupHub;

    public GroupsController(IGroupService groupService, IHubContext<GroupHub> groupHub)
    {
        _groupService = groupService;
        _groupHub = groupHub;
    }

    private string GetCurrentUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<GroupResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroups()
    {
        var userId = GetCurrentUserId();
        var groups = await _groupService.GetAllWithUserMembershipAsync(userId);

        var groupDtos = groups.Select(g => new GroupResponseDto
        {
            Id = g.Id,
            Name = g.Name,
            Slug = g.Slug,
            Description = g.Description,
            ImageUrl = g.ImageUrl,
            CreatorId = g.CreatorId,
            IsJoined = g.Memberships.Any(m => m.UserId == userId),
            MemberCount = g.Memberships.Count,
            CreatedAt = g.CreatedAt
        }).ToList();

        return this.SuccessResponse(groupDtos, "Groups retrieved successfully", 200);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<GroupResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return this.BadRequestResponse(new List<ApiError> { new ApiError("Group name is required", "Name") });

        dto.MemberIds ??= new List<string>();

        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return this.UnauthorizedResponse("Unable to determine current user");

        var newGroup = new Group
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            ImageUrl = dto.ImageUrl
        };

        var createdGroup = await _groupService.CreateWithMembersAsync(newGroup, userId, dto.MemberIds);

        var groupDto = new GroupResponseDto
        {
            Id = createdGroup.Id,
            Name = createdGroup.Name,
            Slug = createdGroup.Slug,
            Description = createdGroup.Description,
            ImageUrl = createdGroup.ImageUrl,
            CreatorId = createdGroup.CreatorId,
            IsJoined = true,
            MemberCount = createdGroup.Memberships.Count,
            CreatedAt = createdGroup.CreatedAt
        };

        // Notify all clients about new group
        await _groupHub.Clients.All.SendAsync("GroupCreated", groupDto);

        return this.CreatedResponse(groupDto, "Group created successfully");
    }

    [HttpPost("{id}/join")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> JoinGroup(int id)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return this.UnauthorizedResponse("Unable to determine current user");

        var joined = await _groupService.JoinAsync(id, userId);
        if (!joined)
            return this.NotFoundResponse("Group not found");

        // Get updated group to get new member count
        var group = await _groupService.GetByIdAsync(id);
        var memberCount = group?.Memberships.Count ?? 0;

        // 🔔 Emit member count updated event via SignalR
        await _groupHub.Clients.All.SendAsync("GroupMemberCountUpdated", new
        {
            groupId = id,
            memberCount = memberCount
        });

        Console.WriteLine($"[GroupsController] 📡 Emitted GroupMemberCountUpdated for group {id}, members: {memberCount}");

        return this.SuccessResponse(message: "Joined group successfully");
    }

    [HttpDelete("{id}/leave")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LeaveGroup(int id)
    {
        var userId = GetCurrentUserId();
        if (string.IsNullOrEmpty(userId))
            return this.UnauthorizedResponse("Unable to determine current user");

        var left = await _groupService.LeaveAsync(id, userId);
        if (!left)
            return this.NotFoundResponse("Group membership not found");

        // Get updated group to get new member count
        var group = await _groupService.GetByIdAsync(id);
        var memberCount = group?.Memberships.Count ?? 0;

        // 🔔 Emit member count updated event via SignalR
        await _groupHub.Clients.All.SendAsync("GroupMemberCountUpdated", new
        {
            groupId = id,
            memberCount = memberCount
        });

        Console.WriteLine($"[GroupsController] 📡 Emitted GroupMemberCountUpdated for group {id}, members: {memberCount}");

        return this.SuccessResponse(message: "Left group successfully");
    }
}
