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
                .ThenInclude(p => p.User)
            .Include(pr => pr.Post)
                .ThenInclude(p => p.Likes)
            .Include(pr => pr.Post)
                .ThenInclude(p => p.Comments)
            .Include(pr => pr.ReporterUser)
            .Include(pr => pr.ReviewedByAdmin)
            .ToListAsync();
    }

    public async Task<PostReport?> GetByIdAsync(int id)
    {
        return await _context.PostReports
            .Include(pr => pr.Post)
                .ThenInclude(p => p.User)
            .Include(pr => pr.Post)
                .ThenInclude(p => p.Likes)
            .Include(pr => pr.Post)
                .ThenInclude(p => p.Comments)
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

        var postId = report.PostId;

        // Xóa logs xóa bài liên quan trước để tránh lỗi FK constraint
        var logs = await _context.PostDeletionLogs.Where(l => l.ReportId == id).ToListAsync();
        if (logs.Any())
        {
            _context.PostDeletionLogs.RemoveRange(logs);
        }

        _context.PostReports.Remove(report);
        await _context.SaveChangesAsync();

        // Nếu bài viết đã bị soft-delete và không còn report nào khác tham chiếu, dọn dẹp bài viết
        var hasOtherReports = await _context.PostReports.AnyAsync(r => r.PostId == postId);
        if (!hasOtherReports)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post != null && post.IsDeleted)
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
        }

        return true;
    }
}
