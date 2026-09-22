using System.Text.RegularExpressions;

namespace InteractHub.Application.Helpers;

/// <summary>
/// Validation helper - Common validations
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validate email format
    /// </summary>
    public static bool IsValidEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validate password strength
    /// </summary>
    public static bool IsStrongPassword(string? password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return false;

        // Phải có ít nhất 1 chữ hoa, 1 chữ thường, 1 số, 1 ký tự đặc biệt
        var hasUpper = Regex.IsMatch(password, "[A-Z]");
        var hasLower = Regex.IsMatch(password, "[a-z]");
        var hasNumber = Regex.IsMatch(password, "[0-9]");
        var hasSpecial = Regex.IsMatch(password, "[!@#$%^&*(),.?\":{}|<>]");

        return hasUpper && hasLower && hasNumber && hasSpecial;
    }

    /// <summary>
    /// Get password strength score (0-100)
    /// </summary>
    public static int GetPasswordStrength(string? password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return 0;

        int strength = 0;

        // Length
        if (password.Length >= 8) strength += 20;
        if (password.Length >= 12) strength += 10;

        // Uppercase
        if (Regex.IsMatch(password, "[A-Z]")) strength += 20;

        // Lowercase
        if (Regex.IsMatch(password, "[a-z]")) strength += 20;

        // Numbers
        if (Regex.IsMatch(password, "[0-9]")) strength += 15;

        // Special characters
        if (Regex.IsMatch(password, "[!@#$%^&*(),.?\":{}|<>]")) strength += 15;

        return Math.Min(strength, 100);
    }

    /// <summary>
    /// Validate phone number
    /// </summary>
    public static bool IsValidPhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
            return false;

        return Regex.IsMatch(phoneNumber, @"^[0-9\+\-\s\(\)]{10,}$");
    }

    /// <summary>
    /// Validate URL
    /// </summary>
    public static bool IsValidUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return Uri.TryCreate(url, UriKind.Absolute, out var uriResult) 
            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Validate UUID
    /// </summary>
    public static bool IsValidUuid(string? uuid)
    {
        if (string.IsNullOrWhiteSpace(uuid))
            return false;

        return Guid.TryParse(uuid, out _);
    }

    /// <summary>
    /// Validate username (alphanumeric + underscore, 3-20 chars)
    /// </summary>
    public static bool IsValidUsername(string? username)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        return Regex.IsMatch(username, @"^[a-zA-Z0-9_]{3,20}$");
    }

    /// <summary>
    /// Validate full name
    /// </summary>
    public static bool IsValidFullName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return false;

        var trimmed = fullName.Trim();
        return trimmed.Length >= 2 && trimmed.Length <= 100;
    }

    /// <summary>
    /// Validate file extension
    /// </summary>
    public static bool IsAllowedFileExtension(string? fileName, string[] allowedExtensions)
    {
        if (string.IsNullOrWhiteSpace(fileName) || allowedExtensions == null || allowedExtensions.Length == 0)
            return false;

        var extension = Path.GetExtension(fileName).ToLower();
        return allowedExtensions.Any(ext => extension == ext.ToLower());
    }

    /// <summary>
    /// Get allowed extensions message
    /// </summary>
    public static string GetAllowedExtensionsMessage(string[] allowedExtensions)
    {
        return $"Allowed extensions: {string.Join(", ", allowedExtensions)}";
    }

    /// <summary>
    /// Validate file size (bytes)
    /// </summary>
    public static bool IsValidFileSize(long fileSize, long maxSizeBytes)
    {
        return fileSize > 0 && fileSize <= maxSizeBytes;
    }

    /// <summary>
    /// Convert file size to readable format
    /// </summary>
    public static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Validate range (between min and max)
    /// </summary>
    public static bool IsInRange<T>(T value, T min, T max) where T : IComparable
    {
        return value.CompareTo(min) >= 0 && value.CompareTo(max) <= 0;
    }

    /// <summary>
    /// Validate string length
    /// </summary>
    public static bool IsValidLength(string? value, int minLength, int maxLength)
    {
        if (value == null)
            return minLength == 0;

        return value.Length >= minLength && value.Length <= maxLength;
    }
}
