using Microsoft.EntityFrameworkCore;
using InteractHub.Application.Interfaces;
using InteractHub.Infrastructure.Data;
using InteractHub.Application.Entities;

namespace InteractHub.Infrastructure.Service;

public class MessageService : IMessageService
{
    private readonly AppDbContext _context;

    public MessageService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Message>> GetMessagesBetweenUsersAsync(string userId1, string userId2)
    {
        return await _context.Messages
            .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                        (m.SenderId == userId2 && m.ReceiverId == userId1))
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Message>> GetMessagesBetweenUsersAsync(string userId1, string userId2, int page = 1, int pageSize = 50)
    {
        return await _context.Messages
            .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                        (m.SenderId == userId2 && m.ReceiverId == userId1))
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .OrderBy(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetConversationMessagesCountAsync(string userId1, string userId2)
    {
        return await _context.Messages
            .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                        (m.SenderId == userId2 && m.ReceiverId == userId1))
            .CountAsync();
    }

    public async Task<Message> SendMessageAsync(string senderId, string receiverId, string content)
    {
        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        await _context.Entry(message).Reference(m => m.Sender).LoadAsync();
        if (message.ReceiverId != null)
        {
            await _context.Entry(message).Reference(m => m.Receiver).LoadAsync();
        }

        return message;
    }

    public async Task MarkAsReadAsync(int messageId, string userId)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message != null && message.ReceiverId == userId)
        {
            message.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<Message>> GetUnreadMessagesAsync(string userId)
    {
        return await _context.Messages
            .Where(m => m.ReceiverId == userId && !m.IsRead)
            .Include(m => m.Sender)
            .ToListAsync();
    }

    public async Task<Message> SendGroupMessageAsync(string senderId, int groupId, string content)
    {
        var message = new Message
        {
            SenderId = senderId,
            GroupId = groupId,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        await _context.Entry(message).Reference(m => m.Sender).LoadAsync();
        await _context.Entry(message).Reference(m => m.Group).LoadAsync();

        return message;
    }

    public async Task<IEnumerable<Message>> GetGroupMessagesAsync(int groupId, int page = 1, int pageSize = 50)
    {
        return await _context.Messages
            .Where(m => m.GroupId == groupId)
            .Include(m => m.Sender)
            .Include(m => m.Group)
            .OrderBy(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetGroupMessagesCountAsync(int groupId)
    {
        return await _context.Messages
            .Where(m => m.GroupId == groupId)
            .CountAsync();
    }

    public async Task<Message?> GetLatestMessageAsync(string userId1, string userId2)
    {
        return await _context.Messages.AsNoTracking()
            .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                        (m.SenderId == userId2 && m.ReceiverId == userId1))
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task<Dictionary<string, Message>> GetLatestMessagesForFriendsAsync(string userId, List<string> friendIds)
    {
        if (friendIds == null || friendIds.Count == 0)
            return new Dictionary<string, Message>();

        var latestMessages = await _context.Messages.AsNoTracking()
            .Where(m => m.GroupId == null && (friendIds.Contains(m.SenderId) || friendIds.Contains(m.ReceiverId)))
            .GroupBy(m => m.SenderId == userId ? m.ReceiverId! : m.SenderId!)
            .Select(g => g.OrderByDescending(m => m.CreatedAt).First())
            .ToListAsync();

        return latestMessages.ToDictionary(
            m => m.SenderId == userId ? m.ReceiverId! : m.SenderId!,
            m => m);
    }
}