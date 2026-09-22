using System.Security.Cryptography;
using System.Text;

namespace InteractHub.Application.Helpers;

/// <summary>
/// Security helper - Hash, encrypt, etc.
/// </summary>
public static class SecurityHelper
{
    /// <summary>
    /// Tạo hash từ string (SHA256)
    /// </summary>
    public static string HashString(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        using (var sha256 = SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hashedBytes);
        }
    }

    /// <summary>
    /// Tạo random string
    /// </summary>
    public static string GenerateRandomString(int length = 32)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var sb = new StringBuilder(length);

        for (int i = 0; i < length; i++)
        {
            sb.Append(chars[random.Next(chars.Length)]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Tạo secure random string (cryptographically secure)
    /// </summary>
    public static string GenerateSecureRandomString(int length = 32)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        using (var rng = new RNGCryptoServiceProvider())
        {
            var data = new byte[length];
            rng.GetBytes(data);

            var result = new StringBuilder(length);
            foreach (byte b in data)
            {
                result.Append(chars[b % chars.Length]);
            }

            return result.ToString();
        }
    }

    /// <summary>
    /// Tạo MD5 hash
    /// </summary>
    public static string GenerateMd5Hash(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        using (var md5 = System.Security.Cryptography.MD5.Create())
        {
            var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash);
        }
    }

    /// <summary>
    /// Mask sensitive data
    /// </summary>
    public static string MaskSensitiveData(string data, int visibleChars = 2)
    {
        if (string.IsNullOrWhiteSpace(data) || data.Length <= visibleChars)
            return data;

        return data.Substring(0, visibleChars) + new string('*', data.Length - visibleChars);
    }
}
