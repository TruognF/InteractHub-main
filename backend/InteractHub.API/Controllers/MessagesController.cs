using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using InteractHub.Application.Interfaces;
using InteractHub.Application.Entities;
using InteractHub.API.DTOs;
using InteractHub.API.DTOs.Response;
using InteractHub.API.Extensions;
using System.Security.Claims;
using InteractHub.Infrastructure.Hubs;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;
    private readonly INotificationService _notificationService;
    private readonly IGroupService _groupService;
    private readonly IHubContext<MessageHub> _messageHubContext;

    public MessagesController(
        IMessageService messageService, 
        INotificationService notificationService,
        IGroupService groupService,
        IHubContext<MessageHub> messageHubContext)
    {
        _messageService = messageService;
        _notificationService = notificationService;
        _groupService = groupService;
        _messageHubContext = messageHubContext;
    }

    [HttpGet("conversation/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversationMessages(string userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        if (currentUserId == userId)
            return BadRequest("Cannot view conversation with yourself");

        // Ensure valid pagination
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(pageSize, 100));

        // Get total count of messages
        var totalCount = await _messageService.GetConversationMessagesCountAsync(currentUserId, userId);

        // Get paginated messages (already sorted ASC in service)
        var messages = await _messageService.GetMessagesBetweenUsersAsync(currentUserId, userId, page, pageSize);

        var messageDtos = messages.Select(m => new MessageResponseDto
        {
            Id = m.Id,
            Content = m.Content,
            CreatedAt = m.CreatedAt,
            SenderId = m.SenderId,
            SenderName = m.Sender?.UserName ?? "Unknown",
            ReceiverId = m.ReceiverId,
            ReceiverName = m.Receiver?.UserName ?? "Unknown",
            IsRead = m.IsRead
        }).ToList();

        return this.SuccessResponse(new
        {
            messages = messageDtos,
            pagination = new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (totalCount + pageSize - 1) / pageSize,
                hasMore = page * pageSize < totalCount
            }
        });
    }

    [HttpGet("group/{groupId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetGroupMessages(int groupId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
            return Unauthorized();

        // Check if user is member of the group
        var group = await _groupService.GetByIdAsync(groupId);
        if (group == null)
            return NotFound("Group not found");

        if (!group.Memberships.Any(m => m.UserId == currentUserId))
            return BadRequest("You are not a member of this group");

        // Ensure valid pagination
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(pageSize, 100)); // Cap pageSize at 100

        // Get total count of messages
        var totalCount = await _messageService.GetGroupMessagesCountAsync(groupId);

        // Get paginated messages (already sorted ASC in service)
        var messages = await _messageService.GetGroupMessagesAsync(groupId, page, pageSize);

        var messageDtos = messages.Select(m => new MessageResponseDto
        {
            Id = m.Id,
            Content = m.Content,
            CreatedAt = m.CreatedAt,
            SenderId = m.SenderId,
            SenderName = m.Sender?.UserName ?? "Unknown",
            GroupId = m.GroupId,
            GroupName = m.Group?.Name,
            IsRead = m.IsRead
        }).ToList();

        return this.SuccessResponse(new
        {
            messages = messageDtos,
            pagination = new
            {
                page,
                pageSize,
                totalCount,
                totalPages = (totalCount + pageSize - 1) / pageSize,
                hasMore = page * pageSize < totalCount
            }
        });
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<MessageResponseDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> SendMessage([FromBody] CreateMessageDto dto)
    {
        var senderId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(senderId))
            return Unauthorized();

        if (string.IsNullOrWhiteSpace(dto.Content))
            return BadRequest("Message content is required");

        Message message;
        if (dto.GroupId.HasValue)
        {
            // Group message
            var group = await _groupService.GetByIdAsync(dto.GroupId.Value);
            if (group == null)
                return NotFound("Group not found");

            // Check if sender is member of the group
            if (!group.Memberships.Any(m => m.UserId == senderId))
                return BadRequest("You are not a member of this group");

            message = await _messageService.SendGroupMessageAsync(senderId, dto.GroupId.Value, dto.Content);
        }
        else
        {
            // Personal message
            if (string.IsNullOrEmpty(dto.ReceiverId))
                return BadRequest("ReceiverId is required for personal messages");

            message = await _messageService.SendMessageAsync(senderId, dto.ReceiverId, dto.Content);
        }

        if (message.ReceiverId != null && message.ReceiverId != message.SenderId)
        {
            await _notificationService.NotifyMessageAsync(message.ReceiverId, message.SenderId, message.Id, message.Content);
        }

        var messageDto = new MessageResponseDto
        {
            Id = message.Id,
            Content = message.Content,
            CreatedAt = message.CreatedAt,
            SenderId = message.SenderId,
            SenderName = message.Sender?.UserName ?? "Unknown",
            ReceiverId = message.ReceiverId,
            ReceiverName = message.Receiver?.UserName ?? "Unknown",
            GroupId = message.GroupId,
            GroupName = message.Group?.Name,
            IsRead = message.IsRead
        };

        // 🔄 Send message to both users via SignalR for real-time update
        if (dto.GroupId.HasValue)
        {
            // Group message - broadcast to all group members
            var groupMembers = message.Group?.Memberships.Select(m => m.UserId).ToList() ?? new List<string>();
            foreach (var memberId in groupMembers)
            {
                await _messageHubContext.Clients.Group($"user_{memberId}")
                    .SendAsync("ReceiveMessage", messageDto);
            }
        }
        else
        {
            // Personal message
            var conversationGroup = GetConversationGroupName(senderId, dto.ReceiverId);
            Console.WriteLine($"[MessagesController] 📡 Broadcasting message to group: {conversationGroup}");
            Console.WriteLine($"[MessagesController] 📨 Message content: {messageDto.Content}");

            await _messageHubContext.Clients.Group(conversationGroup).SendAsync("ReceiveMessage", messageDto);

            await _messageHubContext.Clients.Group($"user_{dto.ReceiverId}")
                .SendAsync("ReceiveMessage", messageDto);

            await _messageHubContext.Clients.Group($"user_{senderId}")
                .SendAsync("ReceiveMessage", messageDto);
        }
        Console.WriteLine($"[MessagesController] ✅ Message broadcasted successfully");

        return this.CreatedResponse(messageDto);
    }

    [HttpPut("{messageId}/read")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAsRead(int messageId)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        await _messageService.MarkAsReadAsync(messageId, userId);
        return this.SuccessResponse(message: "Message marked as read");
    }

    [HttpGet("unread")]
    [ProducesResponseType(typeof(ApiResponse<List<MessageResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadMessages()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        var messages = await _messageService.GetUnreadMessagesAsync(userId);
        var messageDtos = messages.Select(m => new MessageResponseDto
        {
            Id = m.Id,
            Content = m.Content,
            CreatedAt = m.CreatedAt,
            SenderId = m.SenderId,
            SenderName = m.Sender?.UserName ?? "Unknown",
            ReceiverId = m.ReceiverId,
            ReceiverName = m.Receiver?.UserName ?? "Unknown",
            IsRead = m.IsRead
        }).ToList();

        return this.SuccessResponse(messageDtos);
    }

    /// <summary>
    /// Helper: Generate conversation group name (sorted to ensure consistency with MessageHub)
    /// </summary>
    private static string GetConversationGroupName(string userId1, string userId2)
    {
        var ids = new[] { userId1, userId2 }.OrderBy(x => x).ToArray();
        return $"conversation_{ids[0]}_{ids[1]}";
    }
}