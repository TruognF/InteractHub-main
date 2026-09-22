namespace InteractHub.Application.Helpers;

/// <summary>
/// Application-level error codes (no API dependencies)
/// </summary>
public static class AppErrorCodes
{
    // Validation errors (4000-4999)
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";
    public const string INVALID_EMAIL = "INVALID_EMAIL";
    public const string INVALID_PASSWORD = "INVALID_PASSWORD";
    public const string INVALID_USERNAME = "INVALID_USERNAME";
    public const string DUPLICATE_EMAIL = "DUPLICATE_EMAIL";
    public const string DUPLICATE_USERNAME = "DUPLICATE_USERNAME";

    // Authentication errors (4010-4019)
    public const string UNAUTHORIZED = "UNAUTHORIZED";
    public const string INVALID_TOKEN = "INVALID_TOKEN";
    public const string TOKEN_EXPIRED = "TOKEN_EXPIRED";
    public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";

    // Authorization errors (4030-4039)
    public const string FORBIDDEN = "FORBIDDEN";
    public const string ACCESS_DENIED = "ACCESS_DENIED";

    // Not found errors (4040-4049)
    public const string NOT_FOUND = "NOT_FOUND";
    public const string USER_NOT_FOUND = "USER_NOT_FOUND";
    public const string POST_NOT_FOUND = "POST_NOT_FOUND";
    public const string COMMENT_NOT_FOUND = "COMMENT_NOT_FOUND";
    public const string FRIENDSHIP_NOT_FOUND = "FRIENDSHIP_NOT_FOUND";

    // Business logic errors (4200-4299)
    public const string ALREADY_EXISTS = "ALREADY_EXISTS";
    public const string INVALID_STATE = "INVALID_STATE";
    public const string OPERATION_NOT_ALLOWED = "OPERATION_NOT_ALLOWED";
    public const string FRIENDSHIP_ALREADY_EXISTS = "FRIENDSHIP_ALREADY_EXISTS";
    public const string CANNOT_FRIEND_YOURSELF = "CANNOT_FRIEND_YOURSELF";

    // Rate limiting errors (4290)
    public const string RATE_LIMIT_EXCEEDED = "RATE_LIMIT_EXCEEDED";

    // Internal server errors (5000-5999)
    public const string INTERNAL_ERROR = "INTERNAL_ERROR";
    public const string DATABASE_ERROR = "DATABASE_ERROR";
    public const string FILE_OPERATION_ERROR = "FILE_OPERATION_ERROR";
}

/// <summary>
/// Get error message từ error code
/// </summary>
public static class ErrorMessageHelper
{
    public static string GetErrorMessage(string? errorCode)
    {
        return errorCode switch
        {
            AppErrorCodes.VALIDATION_ERROR => "Validation failed",
            AppErrorCodes.INVALID_EMAIL => "Invalid email format",
            AppErrorCodes.INVALID_PASSWORD => "Password does not meet requirements",
            AppErrorCodes.INVALID_USERNAME => "Invalid username format",
            AppErrorCodes.DUPLICATE_EMAIL => "Email already exists",
            AppErrorCodes.DUPLICATE_USERNAME => "Username already exists",

            AppErrorCodes.UNAUTHORIZED => "Unauthorized access",
            AppErrorCodes.INVALID_TOKEN => "Invalid token",
            AppErrorCodes.TOKEN_EXPIRED => "Token has expired",
            AppErrorCodes.INVALID_CREDENTIALS => "Invalid credentials",

            AppErrorCodes.FORBIDDEN => "Access forbidden",
            AppErrorCodes.ACCESS_DENIED => "Access denied",

            AppErrorCodes.NOT_FOUND => "Resource not found",
            AppErrorCodes.USER_NOT_FOUND => "User not found",
            AppErrorCodes.POST_NOT_FOUND => "Post not found",
            AppErrorCodes.COMMENT_NOT_FOUND => "Comment not found",
            AppErrorCodes.FRIENDSHIP_NOT_FOUND => "Friendship not found",

            AppErrorCodes.ALREADY_EXISTS => "Resource already exists",
            AppErrorCodes.INVALID_STATE => "Invalid state",
            AppErrorCodes.OPERATION_NOT_ALLOWED => "Operation not allowed",
            AppErrorCodes.FRIENDSHIP_ALREADY_EXISTS => "Friendship already exists",
            AppErrorCodes.CANNOT_FRIEND_YOURSELF => "Cannot send friend request to yourself",

            AppErrorCodes.RATE_LIMIT_EXCEEDED => "Rate limit exceeded",

            _ => "An error occurred"
        };
    }

    /// <summary>
    /// Map error code lên HTTP status code
    /// </summary>
    public static int GetHttpStatusCode(string? errorCode)
    {
        return errorCode switch
        {
            AppErrorCodes.VALIDATION_ERROR => 400,
            AppErrorCodes.INVALID_EMAIL => 400,
            AppErrorCodes.INVALID_PASSWORD => 400,
            AppErrorCodes.INVALID_USERNAME => 400,
            AppErrorCodes.DUPLICATE_EMAIL => 409,
            AppErrorCodes.DUPLICATE_USERNAME => 409,

            AppErrorCodes.UNAUTHORIZED => 401,
            AppErrorCodes.INVALID_TOKEN => 401,
            AppErrorCodes.TOKEN_EXPIRED => 401,
            AppErrorCodes.INVALID_CREDENTIALS => 401,

            AppErrorCodes.FORBIDDEN => 403,
            AppErrorCodes.ACCESS_DENIED => 403,

            AppErrorCodes.NOT_FOUND => 404,
            AppErrorCodes.USER_NOT_FOUND => 404,
            AppErrorCodes.POST_NOT_FOUND => 404,
            AppErrorCodes.COMMENT_NOT_FOUND => 404,
            AppErrorCodes.FRIENDSHIP_NOT_FOUND => 404,

            AppErrorCodes.ALREADY_EXISTS => 409,
            AppErrorCodes.INVALID_STATE => 409,
            AppErrorCodes.OPERATION_NOT_ALLOWED => 409,
            AppErrorCodes.FRIENDSHIP_ALREADY_EXISTS => 409,
            AppErrorCodes.CANNOT_FRIEND_YOURSELF => 400,

            AppErrorCodes.RATE_LIMIT_EXCEEDED => 429,

            _ => 500
        };
    }
}
