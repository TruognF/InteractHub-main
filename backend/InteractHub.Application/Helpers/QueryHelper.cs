namespace InteractHub.Application.Helpers;

/// <summary>
/// Query helper - Filter, Sort, Search utils
/// </summary>
public static class QueryHelper
{
    /// <summary>
    /// Tạo LIKE query parameter cho SQL
    /// </summary>
    public static string GetSearchPattern(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return "%";

        return $"%{searchTerm.Trim()}%";
    }

    /// <summary>
    /// Parse sort string (ví dụ: "name:asc,createdAt:desc")
    /// </summary>
    public static Dictionary<string, SortDirection> ParseSortString(string? sortString)
    {
        var result = new Dictionary<string, SortDirection>();

        if (string.IsNullOrWhiteSpace(sortString))
            return result;

        var parts = sortString.Split(',');
        foreach (var part in parts)
        {
            var sortParts = part.Trim().Split(':');
            if (sortParts.Length == 2)
            {
                var fieldName = sortParts[0].Trim();
                var direction = sortParts[1].Trim().ToLower() == "desc" 
                    ? SortDirection.Descending 
                    : SortDirection.Ascending;

                if (!result.ContainsKey(fieldName))
                    result[fieldName] = direction;
            }
        }

        return result;
    }

    /// <summary>
    /// Build filter expression (cơ bản)
    /// </summary>
    public static bool IsFilterMatch(string filterValue, string textToSearch)
    {
        if (string.IsNullOrWhiteSpace(filterValue))
            return true;

        return textToSearch.Contains(filterValue, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Validate search term
    /// </summary>
    public static string? ValidateAndSanitizeSearchTerm(string? searchTerm, int maxLength = 100)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return null;

        var sanitized = searchTerm.Trim();

        if (sanitized.Length > maxLength)
            sanitized = sanitized.Substring(0, maxLength);

        // Remove dangerous characters
        sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, @"[;'""\\]", "");

        return string.IsNullOrEmpty(sanitized) ? null : sanitized;
    }

    /// <summary>
    /// Get allowed sort fields (security check)
    /// </summary>
    public static bool IsAllowedSortField(string fieldName, string[] allowedFields)
    {
        return allowedFields.Any(f => f.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Sort direction enum
/// </summary>
public enum SortDirection
{
    Ascending = 0,
    Descending = 1
}
