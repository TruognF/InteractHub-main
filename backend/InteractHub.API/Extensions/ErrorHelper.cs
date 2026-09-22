using InteractHub.API.DTOs.Response;
using InteractHub.Application.Helpers;

namespace InteractHub.API.Extensions;

/// <summary>
/// Error helper - Xử lý lỗi và tạo API responses
/// </summary>
public static class ErrorHelper
{
    /// <summary>
    /// Tạo API error
    /// </summary>
    public static ApiError CreateError(string message, string code)
    {
        return new ApiError(message, code: code);
    }

    /// <summary>
    /// Tạo validation error
    /// </summary>
    public static ApiError CreateValidationError(string fieldName, string message)
    {
        return new ApiError(message, fieldName, AppErrorCodes.VALIDATION_ERROR);
    }

    /// <summary>
    /// Tạo validation errors từ exception
    /// </summary>
    public static List<ApiError> GetValidationErrors(Exception? ex)
    {
        var errors = new List<ApiError>();

        if (ex is ArgumentNullException ane)
        {
            errors.Add(CreateValidationError(ane.ParamName ?? "Unknown", "This field is required"));
        }
        else if (ex is ArgumentException ae)
        {
            errors.Add(CreateValidationError("Unknown", ae.Message));
        }
        else if (ex is InvalidOperationException ioe)
        {
            errors.Add(CreateError(ioe.Message, AppErrorCodes.INVALID_STATE));
        }

        return errors;
    }

    /// <summary>
    /// Map exception lên HTTP status code
    /// </summary>
    public static int GetHttpStatusCode(string errorCode)
    {
        return ErrorMessageHelper.GetHttpStatusCode(errorCode);
    }

    /// <summary>
    /// Get error message từ error code
    /// </summary>
    public static string GetErrorMessage(string? errorCode)
    {
        return ErrorMessageHelper.GetErrorMessage(errorCode);
    }

    /// <summary>
    /// Create error response
    /// </summary>
    public static ApiResponse CreateErrorResponse(string? errorCode, string? customMessage = null)
    {
        var message = customMessage ?? GetErrorMessage(errorCode);
        var statusCode = GetHttpStatusCode(errorCode ?? "");
        var error = new ApiError(message, code: errorCode);

        return new ApiResponse(false, message, null, statusCode, new List<ApiError> { error });
    }
}
