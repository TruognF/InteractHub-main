using InteractHub.Application.Entities;
namespace InteractHub.Application.Interfaces;

public interface ICommentService
{
    Task<List<Comment>> GetAllAsync();
    Task<Comment?> GetByIdAsync(int id);
    Task<List<Comment>> GetByPostIdAsync(int postId);
    Task<int> GetCountByPostIdAsync(int postId);
    Task<Comment> CreateAsync(Comment comment);
    Task<bool> UpdateAsync(Comment comment);
    Task<bool> DeleteAsync(int id);
}