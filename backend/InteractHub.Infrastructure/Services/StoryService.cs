using Microsoft.EntityFrameworkCore;
using InteractHub.Application.Interfaces;
using InteractHub.Infrastructure.Data;
using InteractHub.Application.Entities;

namespace InteractHub.Infrastructure.Service;

public class StoryService : IStoryService
{
    private readonly AppDbContext _context;

    public StoryService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Story>> GetAllAsync()
    {
        return await _context.Stories.AsNoTracking().Include(s => s.User).ToListAsync();
    }

    public async Task<Story?> GetByIdAsync(int id)
    {
        return await _context.Stories.AsNoTracking().Include(s => s.User).FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Story>> GetByUserIdAsync(string userId)
    {
        return await _context.Stories.AsNoTracking()
            .Where(s => s.UserId == userId)
            .Include(s => s.User)
            .ToListAsync();
    }

    public async Task<Story> CreateAsync(Story story)
    {
        _context.Stories.Add(story);
        await _context.SaveChangesAsync();
        return story;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var story = await _context.Stories.FindAsync(id);
        if (story == null)
            return false;

        _context.Stories.Remove(story);
        await _context.SaveChangesAsync();
        return true;
    }
}
