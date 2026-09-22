using InteractHub.Application.DTOs;
using InteractHub.Application.Entities;
namespace InteractHub.Application.Interfaces;

public interface IPostService
{
    Task<List<Post>> GetAllAsync();
    Task<Post?> GetByIdAsync(int id);
    Task<Post> CreateAsync(Post post);
    Task<bool> DeleteAsync(int id);

    Task<(List<PostResponseDto> Posts, int TotalCount)> GetFeedAsync(int page, int pageSize);
    Task<(List<PostResponseDto> Posts, int TotalCount)> GetUserPostsAsync(string userId, int page, int pageSize);
    Task<(List<PostResponseDto> Posts, int TotalCount)> GetGroupPostsAsync(int groupId, int page, int pageSize);
    Task<PostResponseDto?> GetByIdDtoAsync(int id);
    Task<PostLiteDto?> GetLiteAsync(int id);
}