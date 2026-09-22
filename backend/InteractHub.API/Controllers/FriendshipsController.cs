using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using InteractHub.Application.Interfaces;
using InteractHub.Application.Entities;
using InteractHub.Application.Entities.Enums;
using InteractHub.Application.Helpers;
using InteractHub.API.DTOs;
using InteractHub.API.DTOs.Response;
using InteractHub.API.Extensions;
using InteractHub.Infrastructure.Hubs;
using System.Security.Claims;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FriendshipsController : ControllerBase
{
    private readonly IFriendshipService _friendshipService;
    private readonly IMessageService _messageService;
    private readonly IUserPresenceService _userPresenceService;
    private readonly IHubContext<NotificationHub> _notificationHub;

    public FriendshipsController(
        IFriendshipService friendshipService,
        IMessageService messageService,
        IUserPresenceService userPresenceService,
        IHubContext<NotificationHub> notificationHub)
    {
        _friendshipService = friendshipService;
        _messageService = messageService;
        _userPresenceService = userPresenceService;
        _notificationHub = notificationHub;
    }

    /// <summary>
    /// Lấy chi tiết kết bạn
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<FriendshipResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var friendship = await _friendshipService.GetByIdAsync(id);
        if (friendship == null)
            return this.NotFoundResponse("Friendship not found");

        var friendshipDto = MapToFriendshipResponseDto(friendship);
        return this.SuccessResponse(friendshipDto);
    }

    /// <summary>
    /// Lấy danh sách bạn bè (chỉ những người đã chấp nhận) - Có phân trang
    /// </summary>
    [HttpGet("user/{userId}/accepted")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAcceptedFriends(string userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
            return this.UnauthorizedResponse("User not authenticated");
        if (currentUserId != userId)
            return this.ForbiddenResponse();

        try
        {
            // ✅ Validate pagination parameters using PaginationHelper
            PaginationHelper.ValidateParams(pageNumber, pageSize);

            var (friends, metadata) = await _friendshipService.GetAcceptedFriendsPaginatedAsync(userId, pageNumber, pageSize);
            
            // Map Friendship to FriendshipResponseDto, ensuring we get the "other friend" for two-way relationships
            var friendshipDtos = friends.Select(f => {
                var friend = f.UserId == userId ? f.Friend : f.User;
                return new FriendshipResponseDto
                {
                    Id = f.Id,
                    UserId = userId,
                    FriendId = friend?.Id ?? string.Empty,
                    FriendName = friend?.UserName ?? string.Empty,
                    FriendProfilePictureUrl = friend?.ProfilePictureUrl,
                    Status = f.Status.ToString(),
                    CreatedAt = f.CreatedAt,
                    UpdatedAt = f.UpdatedAt
                };
            }).ToList();

            // Return just the array, pagination info can be added later if needed
            return this.SuccessResponse(friendshipDtos);
        }
        catch (ArgumentException ex)
        {
            return this.BadRequestResponse(new List<ApiError> 
            { 
                ErrorHelper.CreateValidationError("pagination", ex.Message) 
            });
        }
    }

    /// <summary>
    /// Lấy danh sách lời mời kết bạn chờ xử lý - Có phân trang
    /// </summary>
    [HttpGet("user/{userId}/pending")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetPendingRequests(string userId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        var userId_current = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != userId_current)
            return this.ForbiddenResponse();

        try
        {
            // ✅ Validate pagination parameters using PaginationHelper
            PaginationHelper.ValidateParams(pageNumber, pageSize);

            var (requests, metadata) = await _friendshipService.GetPendingRequestsPaginatedAsync(userId, pageNumber, pageSize);
            var friendshipDtos = requests.Select(MapToFriendshipResponseDto).ToList();

            // Return just the array, pagination info can be added later if needed
            return this.SuccessResponse(friendshipDtos);
        }
        catch (ArgumentException ex)
        {
            return this.BadRequestResponse(new List<ApiError> 
            { 
                ErrorHelper.CreateValidationError("pagination", ex.Message) 
            });
        }
    }

    /// <summary>
    /// Gửi lời mời kết bạn
    /// </summary>
    [HttpPost("send-request")]
    [ProducesResponseType(typeof(ApiResponse<FriendshipResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendFriendRequest([FromBody] SendFriendRequestDto requestDto)
    {
        try
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return this.UnauthorizedResponse("User not authenticated");

            var friendship = await _friendshipService.SendFriendRequestAsync(userId, requestDto.FriendId);
            var friendshipDto = MapToFriendshipResponseDto(friendship);

            // 🔔 Emit friend request received event via SignalR to the receiver
            // Group format: "notifications-{userId}"
            await _notificationHub.Clients.Group($"notifications-{requestDto.FriendId}")
                .SendAsync("FriendRequestReceived", new
                {
                    Id = friendship.Id,
                    UserId = userId,
                    FriendId = requestDto.FriendId,
                    Status = friendship.Status.ToString(),
                    CreatedAt = friendship.CreatedAt
                });
            
            Console.WriteLine($"[FriendshipsController] 📡 Emitted FriendRequestReceived to {requestDto.FriendId}");

            return this.CreatedResponse(friendshipDto);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestResponse(new List<ApiError> 
            { 
                ErrorHelper.CreateValidationError("friendship", ex.Message) 
            });
        }
    }

    /// <summary>
    /// Chấp nhận lời mời kết bạn
    /// </summary>
    [HttpPost("{id}/accept")]
    [ProducesResponseType(typeof(ApiResponse<FriendshipResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptFriendRequest(int id)
    {
        try
        {
            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserId))
                return this.UnauthorizedResponse("User not authenticated");

            var friendship = await _friendshipService.AcceptFriendRequestAsync(id, currentUserId);
            var friendshipDto = MapToFriendshipResponseDto(friendship);
            
            // 🔔 Emit friend request accepted event to the requester
            await _notificationHub.Clients.Group($"notifications-{friendship.UserId}")
                .SendAsync("FriendRequestAccepted", new
                {
                    Id = friendship.Id,
                    UserId = friendship.UserId,
                    FriendId = friendship.FriendId,
                    Status = friendship.Status.ToString(),
                    UpdatedAt = friendship.UpdatedAt
                });
            
            Console.WriteLine($"[FriendshipsController] 📡 Emitted FriendRequestAccepted to {friendship.UserId}");
            
            return this.SuccessResponse(friendshipDto);
        }
        catch (InvalidOperationException ex)
        {
            return this.BadRequestResponse(new List<ApiError> 
            { 
                ErrorHelper.CreateValidationError("friendship", ex.Message) 
            });
        }
    }

    /// <summary>
    /// Từ chối lời mời kết bạn
    /// </summary>
    [HttpPost("{id}/decline")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeclineFriendRequest(int id)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
            return this.UnauthorizedResponse("User not authenticated");

        var result = await _friendshipService.DeclineFriendRequestAsync(id, currentUserId);
        if (!result)
            return this.BadRequestResponse(new List<ApiError> 
            { 
                ErrorHelper.CreateValidationError("friendship", "Cannot decline this request") 
            });

        // 🔔 Emit friend request declined event via SignalR to the requester
        // Get friendship details to find the requester
        var friendship = await _friendshipService.GetByIdAsync(id);
        if (friendship != null)
        {
            await _notificationHub.Clients.Group($"notifications-{friendship.UserId}")
                .SendAsync("FriendRequestDeclined", new
                {
                    Id = friendship.Id,
                    UserId = friendship.UserId,
                    FriendId = friendship.FriendId,
                    Status = friendship.Status.ToString()
                });
            
            Console.WriteLine($"[FriendshipsController] 📡 Emitted FriendRequestDeclined to {friendship.UserId}");
        }

        return this.SuccessResponse(message: "Friend request declined");
    }

    /// <summary>
    /// Xóa bạn bè
    /// </summary>
    [HttpDelete("remove/{friendId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveFriend(string friendId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return this.UnauthorizedResponse("User not authenticated");

        var result = await _friendshipService.RemoveFriendAsync(userId, friendId);
        if (!result)
            return this.NotFoundResponse("Friend not found or not in accepted state");

        return this.SuccessResponse(message: "Friend removed successfully");
    }

    /// <summary>
    /// Chặn người dùng
    /// </summary>
    [HttpPost("block/{blockUserId}")]
    [ProducesResponseType(typeof(ApiResponse<FriendshipResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> BlockUser(string blockUserId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return this.UnauthorizedResponse("User not authenticated");

        var friendship = await _friendshipService.BlockUserAsync(userId, blockUserId);
        var friendshipDto = MapToFriendshipResponseDto(friendship);

        return this.SuccessResponse(friendshipDto);
    }

    /// <summary>
    /// Get conversations sorted by latest message (newest first), then by name
    /// </summary>
    [HttpGet("user/{userId}/conversations")]
    [ProducesResponseType(typeof(ApiResponse<List<ConversationDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversationsSorted(string userId)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
            return this.UnauthorizedResponse("User not authenticated");
        if (currentUserId != userId)
            return this.ForbiddenResponse();

        try
        {
            // Get accepted friends
            var friends = await _friendshipService.GetAcceptedFriendsAsync(userId);

            // Resolve friend objects (handle both directions of friendship)
            var friendObjs = friends
                .Select(f => f.UserId == userId ? f.Friend : f.User)
                .Where(f => f != null)
                .Distinct()
                .ToList();

            var friendIds = friendObjs.Select(f => f!.Id).ToList();

            // Get latest message per friend in a single query (avoids N+1)
            var latestByFriend = await _messageService.GetLatestMessagesForFriendsAsync(userId, friendIds);

            var conversations = friendObjs.Select(friendObj => new ConversationDto
            {
                Id = friendObj!.Id,
                FriendId = friendObj.Id,
                ConversationName = friendObj.FullName ?? friendObj.UserName ?? "Unknown",
                ConversationAvatarUrl = friendObj.ProfilePictureUrl,
                LastMessage = latestByFriend.GetValueOrDefault(friendObj.Id)?.Content,
                LastMessageTime = latestByFriend.GetValueOrDefault(friendObj.Id)?.CreatedAt,
                LastMessageSenderId = latestByFriend.GetValueOrDefault(friendObj.Id)?.SenderId,
                IsGroup = false, // Private 1-1 conversation
                ParticipantCount = 2, // Current user + friend
                IsOnline = _userPresenceService.IsOnline(friendObj.Id),
                LastSeenAt = _userPresenceService.GetLastSeenAtUtc(friendObj.Id)
            }).ToList();

            // Sort by latest message time (newest first), then by name
            var sorted = conversations
                .OrderByDescending(c => c.LastMessageTime ?? DateTime.MinValue)
                .ThenBy(c => c.ConversationName)
                .ToList();

            return this.SuccessResponse(sorted);
        }
        catch (Exception ex)
        {
            return this.BadRequestResponse(new List<ApiError>
            {
                ErrorHelper.CreateValidationError("conversations", ex.Message)
            });
        }
    }

    /// <summary>
    /// Kiểm tra trạng thái kết bạn
    /// </summary>
    [HttpGet("status/{friendId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CheckFriendshipStatus(string friendId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return this.UnauthorizedResponse("User not authenticated");

        var status = await _friendshipService.CheckFriendshipStatusAsync(userId, friendId);

        var result = new
        {
            UserId = userId,
            FriendId = friendId,
            Status = status?.ToString() ?? "None"
        };

        return this.SuccessResponse(result);
    }

    // ==================== HELPERS ====================

    private FriendshipResponseDto MapToFriendshipResponseDto(Friendship friendship)
    {
        return new FriendshipResponseDto
        {
            Id = friendship.Id,
            UserId = friendship.UserId,
            FriendId = friendship.FriendId,
            Status = friendship.Status.ToString(),
            CreatedAt = friendship.CreatedAt,
            UpdatedAt = friendship.UpdatedAt
        };
    }
}
