using InteractHub.Application.Entities;
namespace InteractHub.Application.Interfaces;

public interface IPostReportService
{
    Task<List<PostReport>> GetAllAsync();
    Task<PostReport?> GetByIdAsync(int id);
    Task<PostReport> CreateAsync(PostReport report);
    Task<bool> UpdateAsync(PostReport report);
    Task<bool> DeleteAsync(int id);
}