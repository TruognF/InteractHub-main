using InteractHub.Application.Entities;

namespace InteractHub.Application.Interfaces;

public interface IPostDeletionLogService
{
    Task<PostDeletionLog> CreateAsync(PostDeletionLog log);
    Task<List<PostDeletionLog>> GetAllAsync();
    Task<PostDeletionLog?> GetByIdAsync(int id);
    Task<List<PostDeletionLog>> GetByPostIdAsync(int postId);
}
