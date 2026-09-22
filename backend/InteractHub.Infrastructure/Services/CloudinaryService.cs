using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using InteractHub.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;

namespace InteractHub.Infrastructure.Service;

public class CloudinaryService : IFileService
{
    private readonly Cloudinary _cloudinary;

    public CloudinaryService(IConfiguration config)
    {
        var cloudinaryConfig = config.GetSection("Cloudinary");
        var account = new Account(
            cloudinaryConfig["CloudName"] ?? Environment.GetEnvironmentVariable("Cloudinary_CloudName"),
            cloudinaryConfig["ApiKey"] ?? Environment.GetEnvironmentVariable("Cloudinary_ApiKey"),
            cloudinaryConfig["ApiSecret"] ?? Environment.GetEnvironmentVariable("Cloudinary_ApiSecret")
        );

        _cloudinary = new Cloudinary(account);
        _cloudinary.Api.Secure = true;
    }

    public async Task<string> UploadImageAsync(IFormFile file, string folderName = "interacthub")
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("Tệp tải lên không hợp lệ hoặc trống rỗng.");
        }

        using var stream = file.OpenReadStream();
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = folderName,
            Transformation = new Transformation().Quality("auto").FetchFormat("auto")
        };

        var uploadResult = await _cloudinary.UploadAsync(uploadParams);

        if (uploadResult.Error != null)
        {
            throw new Exception($"Lỗi khi tải ảnh lên Cloudinary: {uploadResult.Error.Message}");
        }

        // Trả về URI bảo mật (https)
        return uploadResult.SecureUrl.ToString();
    }
}
