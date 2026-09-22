using InteractHub.Application.Entities;

namespace InteractHub.Application.Interfaces;

public interface IMessageService
{
    Task<IEnumerable<Message>> GetMessagesBetweenUsersAsync(string userId1, string userId2);
    Task<IEnumerable<Message>> GetMessagesBetweenUsersAsync(string userId1, string userId2, int page = 1, int pageSize = 50);
    Task<int> GetConversationMessagesCountAsync(string userId1, string userId2);
    Task<Message> SendMessageAsync(string senderId, string receiverId, string content);
    Task<Message> SendGroupMessageAsync(string senderId, int groupId, string content);
    Task<IEnumerable<Message>> GetGroupMessagesAsync(int groupId, int page = 1, int pageSize = 50);
    Task<int> GetGroupMessagesCountAsync(int groupId);
    Task MarkAsReadAsync(int messageId, string userId);
    Task<IEnumerable<Message>> GetUnreadMessagesAsync(string userId);
    
    /// <summary>
    /// Get latest message between two users
    /// </summary>
    Task<Message?> GetLatestMessageAsync(string userId1, string userId2);

    /// <summary>
    /// Get the latest message for each friend pair in one query (avoids N+1).
    /// Key = friend user id.
    /// </summary>
    Task<Dictionary<string, Message>> GetLatestMessagesForFriendsAsync(string userId, List<string> friendIds);
}