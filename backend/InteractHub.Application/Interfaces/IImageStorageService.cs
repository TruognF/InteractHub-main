namespace InteractHub.Application.Interfaces;

public interface IImageStorageService
{
    /// <summary>
    /// Saves an uploaded image stream to disk under {webRoot}/uploads/{folder}.
    /// Returns a relative URL path (e.g. /uploads/posts/xxx.jpg) or null if the stream is empty.
    /// </summary>
    Task<string?> SaveImageAsync(Stream stream, string folder, string extension, string? existingPath = null);

    /// <summary>
    /// Saves a base64 data-URI (e.g. data:image/jpeg;base64,....) to disk under {webRoot}/uploads/{folder}.
    /// Returns a relative URL path or null when the value is null/empty.
    /// </summary>
    Task<string?> SaveDataUriAsync(string? dataUri, string folder, string? existingPath = null);

    /// <summary>
    /// Deletes the stored file when the value is a relative /uploads/... path.
    /// </summary>
    Task DeleteIfStoredAsync(string? relativePath);
}