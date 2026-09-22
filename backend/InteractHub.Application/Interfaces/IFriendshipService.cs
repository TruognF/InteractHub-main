using InteractHub.Application.Entities;
using InteractHub.Application.Entities.Enums;
using InteractHub.Application.Helpers;

namespace InteractHub.Application.Interfaces;

public interface IFriendshipService
{
    // Cơ bản
    Task<Friendship?> GetByIdAsync(int id);
    Task<List<Friendship>> GetFriendsAsync(string userId);
    Task<Friendship> CreateAsync(Friendship friendship);
    Task<bool> UpdateAsync(Friendship friendship);
    Task<bool> DeleteAsync(int id);
    
    // Business Logic - Friend Requests
    /// <summary>
    /// Gửi lời mời kết bạn
    /// </summary>
    Task<Friendship> SendFriendRequestAsync(string senderId, string receiverId);
    
    /// <summary>
    /// Chấp nhận lời mời kết bạn
    /// </summary>
    Task<Friendship> AcceptFriendRequestAsync(int friendshipId, string currentUserId);
    
    /// <summary>
    /// Từ chối lời mời kết bạn
    /// </summary>
    Task<bool> DeclineFriendRequestAsync(int friendshipId, string currentUserId);
    
    /// <summary>
    /// Chặn người dùng
    /// </summary>
    Task<Friendship> BlockUserAsync(string userId, string blockUserId);
    
    /// <summary>
    /// Lấy danh sách lời mời kết bạn chờ xử lý
    /// </summary>
    Task<List<Friendship>> GetPendingRequestsAsync(string userId);
    
    /// <summary>
    /// Kiểm tra trạng thái kết bạn giữa 2 người
    /// </summary>
    Task<FriendshipStatus?> CheckFriendshipStatusAsync(string userId1, string userId2);
    
    /// <summary>
    /// Lấy danh sách bạn bè (chỉ những người đã chấp nhận)
    /// </summary>
    Task<List<Friendship>> GetAcceptedFriendsAsync(string userId);
    
    /// <summary>
    /// Xóa bạn bè
    /// </summary>
    Task<bool> RemoveFriendAsync(string userId, string friendId);

    // Pagination Support
    /// <summary>
    /// Lấy danh sách bạn bè (chỉ những người đã chấp nhận) - Có phân trang
    /// </summary>
    Task<(List<Friendship> Friends, PaginationMetadata Metadata)> GetAcceptedFriendsPaginatedAsync(
        string userId, int pageNumber = 1, int pageSize = 20);

    /// <summary>
    /// Lấy danh sách lời mời kết bạn chờ xử lý - Có phân trang
    /// </summary>
    Task<(List<Friendship> Requests, PaginationMetadata Metadata)> GetPendingRequestsPaginatedAsync(
        string userId, int pageNumber = 1, int pageSize = 20);
}