using InteractHub.Application.Interfaces;

namespace InteractHub.Infrastructure.Services;

public class ImageStorageService : IImageStorageService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".webp"
    };

    private readonly string _uploadRoot;

    public ImageStorageService(string webRootPath)
    {
        _uploadRoot = Path.Combine(webRootPath, "uploads");
        Directory.CreateDirectory(_uploadRoot);
    }

    public async Task<string?> SaveImageAsync(Stream stream, string folder, string extension, string? existingPath = null)
    {
        string ext = NormalizeExtension(extension);
        if (ext == null)
            ext = ".jpg";

        var folderPath = Path.Combine(_uploadRoot, FolderName(folder));
        Directory.CreateDirectory(folderPath);

        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);

        if (buffer.Length == 0)
            return existingPath;

        var fileName = $"{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(folderPath, fileName);

        await using (var fileStream = new FileStream(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            buffer.Position = 0;
            await buffer.CopyToAsync(fileStream);
        }

        var relativePath = $"/uploads/{FolderName(folder)}/{fileName}";

        if (!string.IsNullOrEmpty(existingPath))
            await DeleteIfStoredAsync(existingPath);

        return relativePath;
    }

    public async Task<string?> SaveDataUriAsync(string? dataUri, string folder, string? existingPath = null)
    {
        if (string.IsNullOrWhiteSpace(dataUri) || !dataUri.Contains(";base64,"))
            return null;

        var splitIndex = dataUri.IndexOf(";base64,", StringComparison.OrdinalIgnoreCase);
        if (splitIndex < 0)
            return null;

        var mimePart = dataUri[..splitIndex];
        var extension = mimePart.Contains('/') ? mimePart[(mimePart.LastIndexOf('/') + 1)..] : string.Empty;
        var ext = NormalizeExtension($".{extension}") ?? ".jpg";

        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(dataUri[(splitIndex + ";base64,".Length)..]);
        }
        catch (FormatException)
        {
            return null;
        }

        using var stream = new MemoryStream(bytes);
        return await SaveImageAsync(stream, folder, ext, existingPath);
    }

    public async Task DeleteIfStoredAsync(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || !relativePath.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
            return;

        var fullPath = Path.Combine(_uploadRoot, relativePath["/uploads/".Length..].Replace('/', Path.DirectorySeparatorChar));

        if (!string.IsNullOrWhiteSpace(fullPath) && File.Exists(fullPath))
        {
            // Guard against path traversal
            var fullDirectory = Path.GetFullPath(_uploadRoot);
            var targetFull = Path.GetFullPath(fullPath);
            if (targetFull.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase))
            {
                await Task.Run(() => File.Delete(targetFull));
            }
        }
    }

    private static string? NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            return null;

        var ext = extension.Trim();
        if (!ext.StartsWith('.'))
            ext = $".{ext}";

        return AllowedExtensions.Contains(ext) ? ext : null;
    }

    private static string FolderName(string folder)
    {
        var cleaned = string.Join("_", folder.Split(Path.GetInvalidFileNameChars()));
        return string.IsNullOrWhiteSpace(cleaned) ? "misc" : cleaned;
    }
}