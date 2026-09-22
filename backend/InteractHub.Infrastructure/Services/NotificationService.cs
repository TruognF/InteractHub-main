using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using InteractHub.Application.Interfaces;
using InteractHub.Infrastructure.Data;
using InteractHub.Application.Entities;
using InteractHub.Application.Entities.Enums;
using InteractHub.Infrastructure.Hubs;

namespace InteractHub.Infrastructure.Service;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationService(AppDbContext context, IHubContext<NotificationHub> hubContext)
    {
        _context = context;
        _hubContext = hubContext;
    }

    private static string GetGroupName(string userId) => $"notifications-{userId}";

    // ==================== CƠ BẢN ====================

    public async Task<List<Notification>> GetByUserIdAsync(string userId)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .Include(n => n.RelatedUser)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();
    }

    public async Task<Notification?> GetByIdAsync(int id)
    {
        return await _context.Notifications
            .Include(n => n.RelatedUser)
            .FirstOrDefaultAsync(n => n.Id == id);
    }

    public async Task<Notification> CreateAsync(Notification notification)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        return notification;
    }

    public async Task<bool> MarkAsReadAsync(int id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null)
            return false;

        notification.IsRead = true;
        _context.Notifications.Update(notification);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var notification = await _context.Notifications.FindAsync(id);
        if (notification == null)
            return false;

        _context.Notifications.Remove(notification);
        await _context.SaveChangesAsync();
        return true;
    }

    // ==================== BUSINESS LOGIC ====================

    /// <summary>
    /// Tạo notification và tự động lưu vào DB
    /// </summary>
    public async Task<Notification> CreateNotificationAsync(
        string userId,
        string content,
        NotificationType type,
        string? relatedUserId = null,
        int? relatedEntityId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Content = content,
            Type = type,
            RelatedUserId = relatedUserId,
            RelatedEntityId = relatedEntityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        var createdNotification = await CreateAsync(notification);

        try
        {
            await _hubContext.Clients
                .Group(GetGroupName(userId))
                .SendAsync("ReceiveNotification", new
                {
                    Id = createdNotification.Id,
                    Content = createdNotification.Content,
                    IsRead = createdNotification.IsRead,
                    Type = createdNotification.Type.ToString(),
                    UserId = createdNotification.UserId,
                    RelatedUserId = createdNotification.RelatedUserId,
                    RelatedEntityId = createdNotification.RelatedEntityId,
                    CreatedAt = createdNotification.CreatedAt
                });
        }
        catch
        {
            // Ignore SignalR delivery failures so notification creation still succeeds.
        }

        return createdNotification;
    }

    /// <summary>
    /// Lấy notifications chưa đọc
    /// </summary>
    public async Task<List<Notification>> GetUnreadNotificationsAsync(string userId)
    {
        return await _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId && !n.IsRead)
            .Include(n => n.RelatedUser)
            .OrderByDescending(n => n.CreatedAt)
            .Take(100)
            .ToListAsync();
    }

    /// <summary>
    /// Đánh dấu tất cả là đã đọc
    /// </summary>
    public async Task<bool> MarkAllAsReadAsync(string userId)
    {
        var unreadNotifications = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        if (unreadNotifications.Count == 0)
            return true;

        foreach (var notification in unreadNotifications)
        {
            notification.IsRead = true;
        }

        _context.Notifications.UpdateRange(unreadNotifications);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> MarkFeedNotificationsAsReadAsync(string userId)
    {
        var unreadFeed = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead && n.Type != NotificationType.Message)
            .ToListAsync();

        if (unreadFeed.Count == 0)
            return true;

        foreach (var n in unreadFeed)
            n.IsRead = true;

        _context.Notifications.UpdateRange(unreadFeed);
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Lấy số lượng notifications chưa đọc
    /// </summary>
    public async Task<int> GetUnreadCountAsync(string userId)
    {
        return await _context.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    /// <summary>
    /// Tạo notification khi có friend request
    /// </summary>
    private string GetDisplayName(User? user)
    {
        if (user == null)
            return "Một người dùng";

        return !string.IsNullOrWhiteSpace(user.FullName)
            ? user.FullName
            : !string.IsNullOrWhiteSpace(user.UserName)
                ? user.UserName
                : "Một người dùng";
    }

    private string GetPreview(string text, int maxLength = 50)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.Length > maxLength ? text.Substring(0, maxLength) + "..." : text;
    }

    public async Task<Notification> NotifyFriendRequestAsync(string receiverId, string senderId)
    {
        var sender = await _context.Users.FindAsync(senderId);
        var displayName = GetDisplayName(sender);
        var content = $"{displayName} vừa gửi lời mời kết bạn";

        return await CreateNotificationAsync(
            receiverId,
            content,
            NotificationType.FriendRequest,
            senderId
        );
    }

    /// <summary>
    /// Tạo notification khi friend request được chấp nhận
    /// </summary>
    public async Task<Notification> NotifyFriendRequestAcceptedAsync(string receiverId, string accepterId)
    {
        var accepter = await _context.Users.FindAsync(accepterId);
        var displayName = GetDisplayName(accepter);
        var content = $"{displayName} vừa chấp nhận lời mời kết bạn của bạn";

        return await CreateNotificationAsync(
            receiverId,
            content,
            NotificationType.FriendRequestAccepted,
            accepterId
        );
    }

    /// <summary>
    /// Tạo notification khi có like
    /// </summary>
    public async Task<Notification> NotifyLikeAsync(string userId, string likerUserId, int postId)
    {
        var liker = await _context.Users.FindAsync(likerUserId);
        var displayName = GetDisplayName(liker);
        var content = $"{displayName} vừa thích bài đăng của bạn";

        return await CreateNotificationAsync(
            userId,
            content,
            NotificationType.Like,
            likerUserId,
            postId
        );
    }

    /// <summary>
    /// Tạo notification khi có comment
    /// </summary>
    public async Task<Notification> NotifyCommentAsync(string userId, string commenterUserId, int postId, string commentContent)
    {
        var commenter = await _context.Users.FindAsync(commenterUserId);
        var displayName = GetDisplayName(commenter);
        var preview = GetPreview(commentContent);
        var content = $"{displayName} vừa bình luận trên bài đăng của bạn: \"{preview}\"";

        return await CreateNotificationAsync(
            userId,
            content,
            NotificationType.Comment,
            commenterUserId,
            postId
        );
    }

    /// <summary>
    /// Gửi thông báo tới bạn bè khi có bài đăng mới trên feed cá nhân.
    /// </summary>
    public async Task NotifyFriendsAboutNewPostAsync(string authorUserId, int postId)
    {
        var friendIds = await _context.Friendships
            .AsNoTracking()
            .Where(f =>
                f.Status == FriendshipStatus.Accepted &&
                (f.UserId == authorUserId || f.FriendId == authorUserId))
            .Select(f => f.UserId == authorUserId ? f.FriendId! : f.UserId!)
            .Where(id => !string.IsNullOrEmpty(id) && id != authorUserId)
            .Distinct()
            .ToListAsync();

        if (friendIds.Count == 0)
            return;

        var author = await _context.Users.FindAsync(authorUserId);
        var displayName = GetDisplayName(author);
        var content = $"{displayName} vừa đăng bài viết mới";
        var now = DateTime.UtcNow;

        var notifications = friendIds.Select(friendId => new Notification
        {
            UserId = friendId,
            Content = content,
            Type = NotificationType.FriendPublishedPost,
            RelatedUserId = authorUserId,
            RelatedEntityId = postId,
            IsRead = false,
            CreatedAt = now
        }).ToList();

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        foreach (var notification in notifications)
        {
            try
            {
                await _hubContext.Clients
                    .Group(GetGroupName(notification.UserId))
                    .SendAsync("ReceiveNotification", new
                    {
                        Id = notification.Id,
                        Content = notification.Content,
                        IsRead = notification.IsRead,
                        Type = notification.Type.ToString(),
                        UserId = notification.UserId,
                        RelatedUserId = notification.RelatedUserId,
                        RelatedEntityId = notification.RelatedEntityId,
                        CreatedAt = notification.CreatedAt
                    });
            }
            catch
            {
                // Ignore SignalR delivery failures so notification creation still succeeds.
            }
        }
    }

    /// <summary>
    /// Thông báo chủ bài gốc khi được chia sẻ.
    /// </summary>
    public async Task NotifyOriginalAuthorPostSharedAsync(string originalAuthorUserId, string sharerUserId, int sharedPostId)
    {
        if (string.IsNullOrEmpty(originalAuthorUserId) || originalAuthorUserId == sharerUserId)
            return;

        var sharer = await _context.Users.FindAsync(sharerUserId);
        var displayName = GetDisplayName(sharer);
        var content = $"{displayName} đã chia sẻ bài viết của bạn";

        await CreateNotificationAsync(
            originalAuthorUserId,
            content,
            NotificationType.PostShared,
            sharerUserId,
            sharedPostId
        );
    }

    /// <summary>
    /// Tạo notification khi có tin nhắn
    /// </summary>
    public async Task<Notification> NotifyMessageAsync(string userId, string senderId, int messageId, string messageContent)
    {
        var sender = await _context.Users.FindAsync(senderId);
        var displayName = GetDisplayName(sender);
        var preview = GetPreview(messageContent);
        var content = $"{displayName} vừa gửi tin nhắn cho bạn: \"{preview}\"";

        return await CreateNotificationAsync(
            userId,
            content,
            NotificationType.Message,
            senderId,
            messageId
        );
    }

    /// <summary>
    /// Xóa notifications theo type
    /// </summary>
    public async Task<bool> DeleteNotificationsByTypeAsync(string userId, NotificationType type)
    {
        var notifications = await _context.Notifications
            .Where(n => n.UserId == userId && n.Type == type)
            .ToListAsync();

        if (notifications.Count == 0)
            return true;

        _context.Notifications.RemoveRange(notifications);
        await _context.SaveChangesAsync();
        return true;
    }
}
