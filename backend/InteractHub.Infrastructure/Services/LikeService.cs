using Microsoft.EntityFrameworkCore;
using InteractHub.Application.Interfaces;
using InteractHub.Infrastructure.Data;
using InteractHub.Application.Entities;

namespace InteractHub.Infrastructure.Service;

public class LikeService : ILikeService
{
    private readonly AppDbContext _context;

    public LikeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Like>> GetByPostIdAsync(int postId)
    {
        return await _context.Likes.AsNoTracking().Where(l => l.PostId == postId).ToListAsync();
    }

    public async Task<Like?> GetByIdAsync(int id)
    {
        return await _context.Likes.AsNoTracking().FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<Like?> GetByPostAndUserIdAsync(int postId, string userId)
    {
        return await _context.Likes.AsNoTracking()
            .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);
    }

    public async Task<Like> CreateAsync(Like like)
    {
        _context.Likes.Add(like);
        await _context.SaveChangesAsync();
        return like;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var like = await _context.Likes.FindAsync(id);
        if (like == null)
            return false;

        _context.Likes.Remove(like);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetLikeCountAsync(int postId)
    {
        return await _context.Likes.CountAsync(l => l.PostId == postId);
    }
}