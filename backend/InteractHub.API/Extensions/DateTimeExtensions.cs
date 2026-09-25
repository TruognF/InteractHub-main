namespace InteractHub.API.Extensions;

/// <summary>
/// DateTime extension methods
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Tính khoảng thời gian từ bây giờ (ví dụ: "vừa xong", "2 giờ trước")
    /// </summary>
    public static string GetTimeAgo(this DateTime dateTime)
    {
        var utcDateTime = dateTime.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            : dateTime.ToUniversalTime();

        var timeSpan = DateTime.UtcNow - utcDateTime;

        if (timeSpan.TotalSeconds < 60)
            return "vừa xong";

        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} phút trước";

        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} giờ trước";

        if (timeSpan.TotalDays < 30)
            return $"{(int)timeSpan.TotalDays} ngày trước";

        if (timeSpan.TotalDays < 365)
        {
            var months = (int)(timeSpan.TotalDays / 30);
            return $"{months} tháng trước";
        }

        var years = (int)(timeSpan.TotalDays / 365);
        return $"{years} năm trước";
    }

    /// <summary>
    /// Format DateTime thành Vietnamese format
    /// </summary>
    public static string ToVietnameseFormat(this DateTime dateTime)
    {
        return dateTime.ToString("dd/MM/yyyy HH:mm:ss");
    }

    /// <summary>
    /// Format DateTime thành Vietnamese date only
    /// </summary>
    public static string ToVietnameseDateOnly(this DateTime dateTime)
    {
        return dateTime.ToString("dd/MM/yyyy");
    }

    /// <summary>
    /// Format DateTime thành Vietnamese time only
    /// </summary>
    public static string ToVietnameseTimeOnly(this DateTime dateTime)
    {
        return dateTime.ToString("HH:mm:ss");
    }

    /// <summary>
    /// Kiểm tra xem ngày có phải hôm nay không
    /// </summary>
    public static bool IsToday(this DateTime dateTime)
    {
        return dateTime.Date == DateTime.Today;
    }

    /// <summary>
    /// Kiểm tra xem ngày có phải hôm qua không
    /// </summary>
    public static bool IsYesterday(this DateTime dateTime)
    {
        return dateTime.Date == DateTime.Today.AddDays(-1);
    }

    /// <summary>
    /// Kiểm tra xem ngày có phải trong tuần này không
    /// </summary>
    public static bool IsThisWeek(this DateTime dateTime)
    {
        var today = DateTime.Today;
        var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
        var endOfWeek = startOfWeek.AddDays(6);

        return dateTime.Date >= startOfWeek.Date && dateTime.Date <= endOfWeek.Date;
    }

    /// <summary>
    /// Kiểm tra xem ngày có phải trong tháng này không
    /// </summary>
    public static bool IsThisMonth(this DateTime dateTime)
    {
        var today = DateTime.Today;
        return dateTime.Year == today.Year && dateTime.Month == today.Month;
    }

    /// <summary>
    /// Kiểm tra xem ngày có phải trong năm này không
    /// </summary>
    public static bool IsThisYear(this DateTime dateTime)
    {
        return dateTime.Year == DateTime.Today.Year;
    }

    /// <summary>
    /// Lấy start of day
    /// </summary>
    public static DateTime StartOfDay(this DateTime dateTime)
    {
        return dateTime.Date;
    }

    /// <summary>
    /// Lấy end of day
    /// </summary>
    public static DateTime EndOfDay(this DateTime dateTime)
    {
        return dateTime.Date.AddDays(1).AddTicks(-1);
    }

    /// <summary>
    /// Lấy start of month
    /// </summary>
    public static DateTime StartOfMonth(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, 1);
    }

    /// <summary>
    /// Lấy end of month
    /// </summary>
    public static DateTime EndOfMonth(this DateTime dateTime)
    {
        return dateTime.StartOfMonth().AddMonths(1).AddTicks(-1);
    }

    /// <summary>
    /// Lấy tuần số trong năm
    /// </summary>
    public static int GetWeekOfYear(this DateTime dateTime)
    {
        var culture = System.Globalization.CultureInfo.CurrentCulture;
        var calendar = culture.Calendar;
        return calendar.GetWeekOfYear(dateTime, culture.DateTimeFormat.CalendarWeekRule, culture.DateTimeFormat.FirstDayOfWeek);
    }
}
