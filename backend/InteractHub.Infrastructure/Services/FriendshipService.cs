using Microsoft.EntityFrameworkCore;
using InteractHub.Application.Interfaces;
using InteractHub.Infrastructure.Data;
using InteractHub.Application.Entities;
using InteractHub.Application.Entities.Enums;
using InteractHub.Application.Helpers;

namespace InteractHub.Infrastructure.Service;

public class FriendshipService : IFriendshipService
{
    private readonly AppDbContext _context;
    private readonly INotificationService _notificationService;

    public FriendshipService(AppDbContext context, INotificationService notificationService)
    {
        _context = context;
        _notificationService = notificationService;
    }

    // ==================== CƠSS BẢN ====================
    
    public async Task<Friendship?> GetByIdAsync(int id)
    {
        return await _context.Friendships
            .Include(f => f.User)
            .Include(f => f.Friend)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<List<Friendship>> GetFriendsAsync(string userId)
    {
        return await _context.Friendships.AsNoTracking()
            .Where(f => (f.UserId == userId || f.FriendId == userId) && f.Status == FriendshipStatus.Accepted)
            .Include(f => f.User)
            .Include(f => f.Friend)
            .ToListAsync();
    }

    public async Task<Friendship> CreateAsync(Friendship friendship)
    {
        _context.Friendships.Add(friendship);
        await _context.SaveChangesAsync();
        return friendship;
    }

    public async Task<bool> UpdateAsync(Friendship friendship)
    {
        friendship.UpdatedAt = DateTime.Now;
        _context.Friendships.Update(friendship);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var friendship = await _context.Friendships.FindAsync(id);
        if (friendship == null)
            return false;

        _context.Friendships.Remove(friendship);
        await _context.SaveChangesAsync();
        return true;
    }

    // ==================== BUSINESS LOGIC ====================

    /// <summary>
    /// Gửi lời mời kết bạn
    /// </summary>
    public async Task<Friendship> SendFriendRequestAsync(string senderId, string receiverId)
    {
        // Kiểm tra người gửi và người nhận
        if (senderId == receiverId)
            throw new InvalidOperationException("Không thể gửi lời mời kết bạn cho chính mình");

        // Kiểm tra xem đã có request hay không
        var existingRequest = await _context.Friendships
            .FirstOrDefaultAsync(f =>
                (f.UserId == senderId && f.FriendId == receiverId) ||
                (f.UserId == receiverId && f.FriendId == senderId));

        if (existingRequest != null)
            throw new InvalidOperationException("Lời mời kết bạn đã tồn tại hoặc đã là bạn bè");

        var friendship = new Friendship
        {
            UserId = senderId,
            FriendId = receiverId,
            Status = FriendshipStatus.Pending
        };

        await CreateAsync(friendship);

        // Tạo notification cho người nhận
        await _notificationService.NotifyFriendRequestAsync(receiverId, senderId);

        return friendship;
    }

    /// <summary>
    /// Chấp nhận lời mời kết bạn
    /// </summary>
    public async Task<Friendship> AcceptFriendRequestAsync(int friendshipId, string currentUserId)
    {
        var friendship = await GetByIdAsync(friendshipId);
        if (friendship == null)
            throw new InvalidOperationException("Lời mời kết bạn không tồn tại");

        if (friendship.Status != FriendshipStatus.Pending)
            throw new InvalidOperationException("Lời mời kết bạn không ở trạng thái chờ xử lý");

        if (friendship.FriendId != currentUserId)
            throw new InvalidOperationException("Bạn không có quyền chấp nhận lời mời này");

        friendship.Status = FriendshipStatus.Accepted;
        friendship.UpdatedAt = DateTime.Now;

        await UpdateAsync(friendship);

        // Tạo notification cho người gửi lời mời
        await _notificationService.NotifyFriendRequestAcceptedAsync(friendship.UserId, friendship.FriendId);

        return friendship;
    }

    /// <summary>
    /// Từ chối lời mời kết bạn
    /// </summary>
    public async Task<bool> DeclineFriendRequestAsync(int friendshipId, string currentUserId)
    {
        var friendship = await GetByIdAsync(friendshipId);
        if (friendship == null)
            return false;

        if (friendship.Status != FriendshipStatus.Pending)
            return false;

        if (friendship.FriendId != currentUserId)
            return false;

        friendship.Status = FriendshipStatus.Declined;
        friendship.UpdatedAt = DateTime.Now;

        await UpdateAsync(friendship);
        return true;
    }

    /// <summary>
    /// Chặn người dùng
    /// </summary>
    public async Task<Friendship> BlockUserAsync(string userId, string blockUserId)
    {
        var existingFriendship = await _context.Friendships
            .FirstOrDefaultAsync(f =>
                (f.UserId == userId && f.FriendId == blockUserId) ||
                (f.UserId == blockUserId && f.FriendId == userId));

        if (existingFriendship != null)
        {
            existingFriendship.Status = FriendshipStatus.Blocked;
            await UpdateAsync(existingFriendship);
            return existingFriendship;
        }

        var friendship = new Friendship
        {
            UserId = userId,
            FriendId = blockUserId,
            Status = FriendshipStatus.Blocked
        };

        await CreateAsync(friendship);
        return friendship;
    }

    /// <summary>
    /// Lấy danh sách lời mời kết bạn chờ xử lý
    /// </summary>
    public async Task<List<Friendship>> GetPendingRequestsAsync(string userId)
    {
        return await _context.Friendships.AsNoTracking()
            .Where(f => f.FriendId == userId && f.Status == FriendshipStatus.Pending)
            .Include(f => f.User)
            .Include(f => f.Friend)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Kiểm tra trạng thái kết bạn giữa 2 người
    /// </summary>
    public async Task<FriendshipStatus?> CheckFriendshipStatusAsync(string userId1, string userId2)
    {
        var friendship = await _context.Friendships
            .FirstOrDefaultAsync(f =>
                (f.UserId == userId1 && f.FriendId == userId2) ||
                (f.UserId == userId2 && f.FriendId == userId1));

        return friendship?.Status;
    }

    /// <summary>
    /// Lấy danh sách bạn bè (chỉ những người đã chấp nhận)
    /// </summary>
    public async Task<List<Friendship>> GetAcceptedFriendsAsync(string userId)
    {
        return await _context.Friendships.AsNoTracking()
            .Where(f => (f.UserId == userId || f.FriendId == userId) 
                && f.Status == FriendshipStatus.Accepted)
            .Include(f => f.User)
            .Include(f => f.Friend)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Xóa bạn bè
    /// </summary>
    public async Task<bool> RemoveFriendAsync(string userId, string friendId)
    {
        var friendship = await _context.Friendships
            .FirstOrDefaultAsync(f =>
                (f.UserId == userId && f.FriendId == friendId) ||
                (f.UserId == friendId && f.FriendId == userId));

        if (friendship == null)
            return false;

        if (friendship.Status != FriendshipStatus.Accepted)
            return false;

        _context.Friendships.Remove(friendship);
        await _context.SaveChangesAsync();
        return true;
    }

    // ==================== PAGINATION SUPPORT ====================

    /// <summary>
    /// Lấy danh sách bạn bè (chỉ những người đã chấp nhận) - Có phân trang
    /// </summary>
    public async Task<(List<Friendship> Friends, PaginationMetadata Metadata)> GetAcceptedFriendsPaginatedAsync(
        string userId, int pageNumber = 1, int pageSize = 20)
    {
        // ✅ Validate pagination parameters
        PaginationHelper.ValidateParams(pageNumber, pageSize);

        // 1️⃣ Lấy total count
        var totalCount = await _context.Friendships
            .Where(f => (f.UserId == userId || f.FriendId == userId) 
                && f.Status == FriendshipStatus.Accepted)
            .CountAsync();

        // 2️⃣ Tính skip count
        var skipCount = PaginationHelper.GetSkipCount(pageNumber, pageSize);

        // 3️⃣ Lấy dữ liệu
        var friends = await _context.Friendships.AsNoTracking()
            .Where(f => (f.UserId == userId || f.FriendId == userId) 
                && f.Status == FriendshipStatus.Accepted)
            .Include(f => f.User)
            .Include(f => f.Friend)
            .OrderByDescending(f => f.CreatedAt)
            .Skip(skipCount)
            .Take(pageSize)
            .ToListAsync();

        // 4️⃣ Tạo pagination metadata
        var metadata = PaginationHelper.CreateMetadata(pageNumber, pageSize, totalCount);

        return (friends, metadata);
    }

    /// <summary>
    /// Lấy danh sách lời mời kết bạn chờ xử lý - Có phân trang
    /// </summary>
    public async Task<(List<Friendship> Requests, PaginationMetadata Metadata)> GetPendingRequestsPaginatedAsync(
        string userId, int pageNumber = 1, int pageSize = 20)
    {
        // ✅ Validate pagination parameters
        PaginationHelper.ValidateParams(pageNumber, pageSize);

        // 1️⃣ Lấy total count
        var totalCount = await _context.Friendships
            .Where(f => f.FriendId == userId && f.Status == FriendshipStatus.Pending)
            .CountAsync();

        // 2️⃣ Tính skip count
        var skipCount = PaginationHelper.GetSkipCount(pageNumber, pageSize);

        // 3️⃣ Lấy dữ liệu
        var requests = await _context.Friendships.AsNoTracking()
            .Where(f => f.FriendId == userId && f.Status == FriendshipStatus.Pending)
            .Include(f => f.User)
            .Include(f => f.Friend)
            .OrderByDescending(f => f.CreatedAt)
            .Skip(skipCount)
            .Take(pageSize)
            .ToListAsync();

        // 4️⃣ Tạo pagination metadata
        var metadata = PaginationHelper.CreateMetadata(pageNumber, pageSize, totalCount);

        return (requests, metadata);
    }
}
