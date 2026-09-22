using InteractHub.Application.Entities;
namespace InteractHub.Application.Interfaces;

public interface ILikeService
{
    Task<List<Like>> GetByPostIdAsync(int postId);
    Task<Like?> GetByIdAsync(int id);
    Task<Like?> GetByPostAndUserIdAsync(int postId, string userId);
    Task<Like> CreateAsync(Like like);
    Task<bool> DeleteAsync(int id);
    Task<int> GetLikeCountAsync(int postId);

}