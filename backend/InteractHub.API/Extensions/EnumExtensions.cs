using System.ComponentModel;
using System.Reflection;

namespace InteractHub.API.Extensions;

/// <summary>
/// Enum extension methods
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Lấy Description của enum value
    /// </summary>
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }

    /// <summary>
    /// Lấy Display Name của enum value
    /// </summary>
    public static string GetDisplayName(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<System.ComponentModel.DataAnnotations.DisplayAttribute>();
        return attribute?.Name ?? value.ToString();
    }

    /// <summary>
    /// Chuyển enum thành danh sách key-value
    /// </summary>
    public static List<KeyValuePair<string, string>> GetEnumList<T>() where T : struct, Enum
    {
        var result = new List<KeyValuePair<string, string>>();
        var values = Enum.GetValues(typeof(T));

        foreach (var value in values)
        {
            if (value is T enumValue)
            {
                result.Add(new KeyValuePair<string, string>(
                    enumValue.ToString(),
                    enumValue.GetDescription()
                ));
            }
        }

        return result;
    }

    /// <summary>
    /// Kiểm tra xem enum value có tồn tại không
    /// </summary>
    public static bool IsValidEnum<T>(string value) where T : struct, Enum
    {
        return Enum.TryParse<T>(value, true, out _);
    }
}
