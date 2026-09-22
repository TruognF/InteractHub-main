using Microsoft.EntityFrameworkCore;
using InteractHub.Application.Interfaces;
using InteractHub.Infrastructure.Data;
using InteractHub.Application.Entities;

namespace InteractHub.Infrastructure.Service;

public class HashtagService : IHashtagService
{
    private readonly AppDbContext _context;

    public HashtagService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Hashtag>> GetAllAsync()
    {
        return await _context.Hashtags.ToListAsync();
    }

    public async Task<Hashtag?> GetByIdAsync(int id)
    {
        return await _context.Hashtags.FindAsync(id);
    }

    public async Task<Hashtag?> GetByNameAsync(string name)
    {
        return await _context.Hashtags.FirstOrDefaultAsync(h => h.Name == name);
    }

    public async Task<Hashtag> CreateAsync(Hashtag hashtag)
    {
        _context.Hashtags.Add(hashtag);
        await _context.SaveChangesAsync();
        return hashtag;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var hashtag = await _context.Hashtags.FindAsync(id);
        if (hashtag == null)
            return false;

        _context.Hashtags.Remove(hashtag);
        await _context.SaveChangesAsync();
        return true;
    }
}
