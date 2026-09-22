namespace InteractHub.API.Extensions;

/// <summary>
/// String extension methods
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Kiểm tra xem string có trống hay không
    /// </summary>
    public static bool IsNullOrEmpty(this string? value)
    {
        return string.IsNullOrEmpty(value);
    }

    /// <summary>
    /// Kiểm tra xem string có chứa chỉ whitespace hay không
    /// </summary>
    public static bool IsNullOrWhiteSpace(this string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }

    /// <summary>
    /// Cắt ngắn string với limit độ dài
    /// </summary>
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return value.Length <= maxLength 
            ? value 
            : value.Substring(0, maxLength) + "...";
    }

    /// <summary>
    /// Chuyển first letter thành uppercase
    /// </summary>
    public static string FirstCharToUpper(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return char.ToUpper(value[0]) + value.Substring(1);
    }

    /// <summary>
    /// Chuyển first letter thành lowercase
    /// </summary>
    public static string FirstCharToLower(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return char.ToLower(value[0]) + value.Substring(1);
    }

    /// <summary>
    /// Convert slug string (for URLs)
    /// </summary>
    public static string ToSlug(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        value = value.Trim().ToLower();
        // Remove special characters, keep only letters, numbers, hyphens
        var result = System.Text.RegularExpressions.Regex.Replace(value, @"[^\w\s-]", "");
        result = System.Text.RegularExpressions.Regex.Replace(result, @"[\s]+", "-");
        return System.Text.RegularExpressions.Regex.Replace(result, @"[-]+", "-");
    }

    /// <summary>
    /// Kiểm tra xem string có phải email hợp lệ không
    /// </summary>
    public static bool IsValidEmail(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(value);
            return addr.Address == value;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Kiểm tra xem string có phải phone number không (cơ bản)
    /// </summary>
    public static bool IsValidPhoneNumber(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return System.Text.RegularExpressions.Regex.IsMatch(value, @"^[0-9\+\-\s\(\)]+$") 
            && value.Length >= 10;
    }

    /// <summary>
    /// Mask email (ví dụ: john****@gmail.com)
    /// </summary>
    public static string MaskEmail(this string email)
    {
        if (!email.Contains("@"))
            return email;

        var parts = email.Split('@');
        var name = parts[0];
        var domain = parts[1];

        if (name.Length <= 2)
            return email;

        var visibleChars = 2;
        var maskedName = name.Substring(0, visibleChars) + new string('*', name.Length - visibleChars);
        return $"{maskedName}@{domain}";
    }
}
