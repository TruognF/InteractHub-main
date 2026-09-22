using InteractHub.Application.Entities;
namespace InteractHub.Application.Interfaces;

public interface IStoryService
{
    Task<List<Story>> GetAllAsync();
    Task<Story?> GetByIdAsync(int id);
    Task<List<Story>> GetByUserIdAsync(string userId);
    Task<Story> CreateAsync(Story story);
    Task<bool> DeleteAsync(int id);
}