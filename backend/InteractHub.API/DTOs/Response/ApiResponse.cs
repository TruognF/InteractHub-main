namespace InteractHub.API.DTOs.Response;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public int StatusCode { get; set; }
    public List<ApiError>? Errors { get; set; }

    public ApiResponse(bool success, string? message, T? data, int statusCode, List<ApiError>? errors = null)
    {
        Success = success;
        Message = message;
        Data = data;
        StatusCode = statusCode;
        Errors = errors;
    }
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public object? Data { get; set; }
    public int StatusCode { get; set; }
    public List<ApiError>? Errors { get; set; }

    public ApiResponse(bool success, string? message, object? data, int statusCode, List<ApiError>? errors = null)
    {
        Success = success;
        Message = message;
        Data = data;
        StatusCode = statusCode;
        Errors = errors;
    }
}

public class ApiError
{
    public string? Code { get; set; }
    public string? Field { get; set; }
    public string? Message { get; set; }

    public ApiError(string? message, string? field = null, string? code = null)
    {
        Message = message;
        Field = field;
        Code = code;
    }
}
