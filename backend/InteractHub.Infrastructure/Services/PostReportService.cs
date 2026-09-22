using Microsoft.EntityFrameworkCore;
using InteractHub.Application.Interfaces;
using InteractHub.Infrastructure.Data;
using InteractHub.Application.Entities;

namespace InteractHub.Infrastructure.Service;

public class PostReportService : IPostReportService
{
    private readonly AppDbContext _context;

    public PostReportService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<PostReport>> GetAllAsync()
    {
        return await _context.PostReports
            .Include(pr => pr.Post)
            .Include(pr => pr.ReporterUser)
            .Include(pr => pr.ReviewedByAdmin)
            .ToListAsync();
    }

    public async Task<PostReport?> GetByIdAsync(int id)
    {
        return await _context.PostReports
            .Include(pr => pr.Post)
            .Include(pr => pr.ReporterUser)
            .Include(pr => pr.ReviewedByAdmin)
            .FirstOrDefaultAsync(pr => pr.Id == id);
    }

    public async Task<PostReport> CreateAsync(PostReport report)
    {
        _context.PostReports.Add(report);
        await _context.SaveChangesAsync();
        return report;
    }

    public async Task<bool> UpdateAsync(PostReport report)
    {
        _context.PostReports.Update(report);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var report = await _context.PostReports.FindAsync(id);
        if (report == null)
            return false;

        _context.PostReports.Remove(report);
        await _context.SaveChangesAsync();
        return true;
    }
}
