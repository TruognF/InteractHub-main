namespace InteractHub.Application.Helpers;

/// <summary>
/// Pagination helper class
/// </summary>
public class PaginationHelper
{
    /// <summary>
    /// Tính toán skip count dựa trên page number và page size
    /// </summary>
    public static int GetSkipCount(int pageNumber, int pageSize)
    {
        ValidateParams(pageNumber, pageSize);
        return (pageNumber - 1) * pageSize;
    }

    /// <summary>
    /// Lấy total pages từ total count
    /// </summary>
    public static int GetTotalPages(int totalCount, int pageSize)
    {
        ValidatePageSize(pageSize);
        return (int)Math.Ceiling((double)totalCount / pageSize);
    }

    /// <summary>
    /// Kiểm tra xem page number có hợp lệ không
    /// </summary>
    public static bool IsValidPageNumber(int pageNumber, int totalPages)
    {
        return pageNumber >= 1 && pageNumber <= totalPages;
    }

    /// <summary>
    /// Validate pagination parameters
    /// </summary>
    public static void ValidateParams(int pageNumber, int pageSize)
    {
        if (pageNumber <= 0)
            throw new ArgumentException("Page number must be greater than 0", nameof(pageNumber));

        ValidatePageSize(pageSize);
    }

    /// <summary>
    /// Validate page size
    /// </summary>
    public static void ValidatePageSize(int pageSize)
    {
        if (pageSize <= 0)
            throw new ArgumentException("Page size must be greater than 0", nameof(pageSize));

        if (pageSize > 100)
            throw new ArgumentException("Page size cannot exceed 100", nameof(pageSize));
    }

    /// <summary>
    /// Tạo pagination metadata
    /// </summary>
    public static PaginationMetadata CreateMetadata(int pageNumber, int pageSize, int totalCount)
    {
        ValidateParams(pageNumber, pageSize);

        var totalPages = GetTotalPages(totalCount, pageSize);
        var hasNextPage = pageNumber < totalPages;
        var hasPreviousPage = pageNumber > 1;

        return new PaginationMetadata
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasNextPage = hasNextPage,
            HasPreviousPage = hasPreviousPage
        };
    }
}

/// <summary>
/// Pagination metadata
/// </summary>
public class PaginationMetadata
{
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}
