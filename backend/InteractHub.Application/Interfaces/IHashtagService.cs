using InteractHub.Application.Entities;
namespace InteractHub.Application.Interfaces;

public interface IHashtagService
{
    Task<List<Hashtag>> GetAllAsync();
    Task<Hashtag?> GetByIdAsync(int id);
    Task<Hashtag?> GetByNameAsync(string name);
    Task<Hashtag> CreateAsync(Hashtag hashtag);
    Task<bool> DeleteAsync(int id);
}