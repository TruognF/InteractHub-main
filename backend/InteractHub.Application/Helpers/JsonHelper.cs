namespace InteractHub.Application.Helpers;

/// <summary>
/// JSON conversion helper
/// </summary>
public static class JsonHelper
{
    /// <summary>
    /// Convert object to JSON string
    /// </summary>
    public static string ToJson<T>(T? obj)
    {
        if (obj == null)
            return "null";

        try
        {
            return System.Text.Json.JsonSerializer.Serialize(obj);
        }
        catch
        {
            return obj.ToString() ?? "null";
        }
    }

    /// <summary>
    /// Convert JSON string to object
    /// </summary>
    public static T? FromJson<T>(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return default;

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Clone object via JSON serialization
    /// </summary>
    public static T? DeepClone<T>(T? obj) where T : class
    {
        if (obj == null)
            return null;

        var json = ToJson(obj);
        return FromJson<T>(json);
    }
}
