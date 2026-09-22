using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using InteractHub.Application.Interfaces;
using InteractHub.Application.Entities;
using InteractHub.Application.Entities.Enums;
using InteractHub.API.DTOs;
using InteractHub.API.DTOs.Response;
using InteractHub.API.Extensions;
using System.Security.Claims;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    /// <summary>
    /// Lấy chi tiết một notification
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<NotificationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var notification = await _notificationService.GetByIdAsync(id);
        if (notification == null)
            return this.NotFoundResponse("Notification not found");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (notification.UserId != userId)
            return this.ForbiddenResponse();

        var notificationDto = MapToNotificationResponseDto(notification);
        return this.SuccessResponse(notificationDto);
    }

    /// <summary>
    /// Lấy tất cả notifications của người dùng
    /// </summary>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<List<NotificationResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByUserId(string userId)
    {
        var userId_current = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != userId_current)
            return this.ForbiddenResponse();

        var notifications = await _notificationService.GetByUserIdAsync(userId);
        var notificationDtos = notifications.Select(MapToNotificationResponseDto).ToList();
        return this.SuccessResponse(notificationDtos);
    }

    /// <summary>
    /// Lấy notifications chưa đọc
    /// </summary>
    [HttpGet("user/{userId}/unread")]
    [ProducesResponseType(typeof(ApiResponse<List<NotificationResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadNotifications(string userId)
    {
        var userId_current = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != userId_current)
            return this.ForbiddenResponse();

        var unreadNotifications = await _notificationService.GetUnreadNotificationsAsync(userId);
        var notificationDtos = unreadNotifications.Select(MapToNotificationResponseDto).ToList();
        return this.SuccessResponse(notificationDtos);
    }

    /// <summary>
    /// Lấy số lượng notifications chưa đọc
    /// </summary>
    [HttpGet("user/{userId}/unread-count")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUnreadCount(string userId)
    {
        var userId_current = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != userId_current)
            return this.ForbiddenResponse();

        var count = await _notificationService.GetUnreadCountAsync(userId);
        return this.SuccessResponse(new { UnreadCount = count });
    }

    /// <summary>
    /// Đánh dấu notification là đã đọc
    /// </summary>
    [HttpPut("{id}/read")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var notification = await _notificationService.GetByIdAsync(id);
        if (notification == null)
            return this.NotFoundResponse("Notification not found");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (notification.UserId != userId)
            return this.ForbiddenResponse();

        var result = await _notificationService.MarkAsReadAsync(id);
        if (!result)
            return this.NotFoundResponse("Notification not found");

        return this.SuccessResponse(message: "Notification marked as read");
    }

    /// <summary>
    /// Đánh dấu tất cả notifications là đã đọc
    /// </summary>
    [HttpPut("user/{userId}/mark-all-read")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkAllAsRead(string userId)
    {
        var userId_current = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != userId_current)
            return this.ForbiddenResponse();

        await _notificationService.MarkAllAsReadAsync(userId);
        return this.SuccessResponse(message: "All notifications marked as read");
    }

    /// <summary>
    /// Đánh dấu đã đọc thông báo feed (trừ tin nhắn), giữ nguyên bản ghi lịch sử.
    /// </summary>
    [HttpPut("user/{userId}/mark-feed-read")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> MarkFeedNotificationsAsRead(string userId)
    {
        var userIdCurrent = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != userIdCurrent)
            return this.ForbiddenResponse();

        await _notificationService.MarkFeedNotificationsAsReadAsync(userId);
        return this.SuccessResponse(message: "Feed notifications marked as read");
    }

    /// <summary>
    /// Xóa notification
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var notification = await _notificationService.GetByIdAsync(id);
        if (notification == null)
            return this.NotFoundResponse("Notification not found");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (notification.UserId != userId)
            return this.ForbiddenResponse();

        var result = await _notificationService.DeleteAsync(id);
        if (!result)
            return this.NotFoundResponse("Notification not found");

        return this.SuccessResponse(message: "Notification deleted");
    }

    /// <summary>
    /// Xóa notifications theo type
    /// </summary>
    [HttpDelete("user/{userId}/by-type/{type}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteByType(string userId, string type)
    {
        var userId_current = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != userId_current)
            return this.ForbiddenResponse();

        if (!Enum.TryParse<NotificationType>(type, true, out var notificationType))
            return this.BadRequestResponse(new List<ApiError> 
            { 
                ErrorHelper.CreateValidationError("type", $"Invalid notification type: {type}") 
            });

        await _notificationService.DeleteNotificationsByTypeAsync(userId, notificationType);
        return this.SuccessResponse(message: $"Notifications of type '{type}' deleted");
    }

    // ==================== HELPERS ====================

    private NotificationResponseDto MapToNotificationResponseDto(Notification notification)
    {
        return new NotificationResponseDto
        {
            Id = notification.Id,
            Content = notification.Content,
            IsRead = notification.IsRead,
            Type = notification.Type.ToString(),
            UserId = notification.UserId,
            RelatedUserId = notification.RelatedUserId,
            RelatedEntityId = notification.RelatedEntityId,
            CreatedAt = notification.CreatedAt,
            // ✅ Use DateTimeExtensions to calculate user-friendly time
            TimeAgo = notification.CreatedAt.GetTimeAgo()
        };
    }
}
