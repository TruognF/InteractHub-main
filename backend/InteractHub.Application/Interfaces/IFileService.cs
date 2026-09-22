using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace InteractHub.Application.Interfaces;

public interface IFileService
{
    Task<string> UploadImageAsync(IFormFile file, string folderName = "interacthub");
}
