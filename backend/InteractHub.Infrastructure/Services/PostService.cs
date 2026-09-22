using Microsoft.EntityFrameworkCore;
using InteractHub.Application.DTOs;
using InteractHub.Application.Interfaces;
using InteractHub.Infrastructure.Data;
using InteractHub.Application.Entities;

namespace InteractHub.Infrastructure.Service;

public class PostService : IPostService
{
    private readonly AppDbContext _context;

    public PostService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Post> CreateAsync(Post post)
    {
        _context.Posts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var post = await _context.Posts.FindAsync(id);
        if (post == null)
        {
            return false;
        }

        _context.Posts.Remove(post);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<Post>> GetAllAsync()
    {
        return await _context.Posts.AsNoTracking().ToListAsync();
    }

    public async Task<Post?> GetByIdAsync(int id)
    {
        return await _context.Posts.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
    }

    private static readonly System.Linq.Expressions.Expression<System.Func<Post, PostResponseDto>> ToDto =
        p => new PostResponseDto
        {
            Id = p.Id,
            GroupId = p.GroupId,
            Content = p.Content,
            ImageUrl = p.ImageUrl,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            UserId = p.UserId,
            UserName = p.User!.UserName,
            UserFullName = p.User!.FullName,
            UserProfilePictureUrl = p.User!.ProfilePictureUrl,
            LikesCount = p.Likes.Count,
            CommentsCount = p.Comments.Count,
            LikedByUserIds = p.Likes.Select(l => l.UserId).ToList(),
            IsShared = p.SharedPostId.HasValue,
            SharedPostId = p.SharedPostId,
            SharedPost = p.SharedPostId.HasValue
                ? new SharedPostDto
                {
                    Id = p.SharedPost!.Id,
                    Content = p.SharedPost.Content,
                    ImageUrl = p.SharedPost.ImageUrl,
                    CreatedAt = p.SharedPost.CreatedAt,
                    UpdatedAt = p.SharedPost.UpdatedAt,
                    UserId = p.SharedPost.UserId,
                    UserName = p.SharedPost.User!.UserName,
                    UserFullName = p.SharedPost.User!.FullName,
                    UserProfilePictureUrl = p.SharedPost.User!.ProfilePictureUrl,
                    LikesCount = p.SharedPost.Likes.Count,
                    CommentsCount = p.SharedPost.Comments.Count,
                    LikedByUserIds = p.SharedPost.Likes.Select(l => l.UserId).ToList()
                }
                : null
        };

    public async Task<(List<PostResponseDto> Posts, int TotalCount)> GetFeedAsync(int page, int pageSize)
    {
        var query = _context.Posts.AsNoTracking().Where(p => p.GroupId == null);
        var totalCount = await query.CountAsync();
        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync();
        return (posts, totalCount);
    }

    public async Task<(List<PostResponseDto> Posts, int TotalCount)> GetUserPostsAsync(string userId, int page, int pageSize)
    {
        var query = _context.Posts.AsNoTracking().Where(p => p.UserId == userId && p.GroupId == null);
        var totalCount = await query.CountAsync();
        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync();
        return (posts, totalCount);
    }

    public async Task<(List<PostResponseDto> Posts, int TotalCount)> GetGroupPostsAsync(int groupId, int page, int pageSize)
    {
        var query = _context.Posts.AsNoTracking().Where(p => p.GroupId == groupId);
        var totalCount = await query.CountAsync();
        var posts = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(ToDto)
            .ToListAsync();
        return (posts, totalCount);
    }

    public async Task<PostResponseDto?> GetByIdDtoAsync(int id)
    {
        return await _context.Posts.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(ToDto)
            .FirstOrDefaultAsync();
    }

    public async Task<PostLiteDto?> GetLiteAsync(int id)
    {
        return await _context.Posts.AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PostLiteDto { UserId = p.UserId, GroupId = p.GroupId })
            .FirstOrDefaultAsync();
    }
}