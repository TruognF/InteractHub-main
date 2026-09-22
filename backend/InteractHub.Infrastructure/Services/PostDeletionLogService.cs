using Microsoft.EntityFrameworkCore;
using InteractHub.Application.Interfaces;
using InteractHub.Infrastructure.Data;
using InteractHub.Application.Entities;

namespace InteractHub.Infrastructure.Service;

public class PostDeletionLogService : IPostDeletionLogService
{
    private readonly AppDbContext _context;

    public PostDeletionLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PostDeletionLog> CreateAsync(PostDeletionLog log)
    {
        _context.PostDeletionLogs.Add(log);
        await _context.SaveChangesAsync();
        return log;
    }

    public async Task<List<PostDeletionLog>> GetAllAsync()
    {
        return await _context.PostDeletionLogs
            .Include(l => l.User)
            .Include(l => l.DeletedByAdmin)
            .Include(l => l.Report)
            .OrderByDescending(l => l.DeletedAt)
            .ToListAsync();
    }

    public async Task<PostDeletionLog?> GetByIdAsync(int id)
    {
        return await _context.PostDeletionLogs
            .Include(l => l.User)
            .Include(l => l.DeletedByAdmin)
            .Include(l => l.Report)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<List<PostDeletionLog>> GetByPostIdAsync(int postId)
    {
        return await _context.PostDeletionLogs
            .Include(l => l.User)
            .Include(l => l.DeletedByAdmin)
            .Include(l => l.Report)
            .Where(l => l.PostId == postId)
            .OrderByDescending(l => l.DeletedAt)
            .ToListAsync();
    }
}
