namespace InteractHub.API.Exceptions;

using InteractHub.API.DTOs.Response;

public class ApiException : Exception
{
    public int StatusCode { get; set; }
    public List<ApiError>? Errors { get; set; }

    public ApiException(string message, int statusCode = 500, List<ApiError>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}

public class NotFoundException : ApiException
{
    public NotFoundException(string message)
        : base(message, 404, new List<ApiError> 
        { 
            new ApiError(message, code: "NOT_FOUND") 
        })
    {
    }
}

public class UnauthorizedException : ApiException
{
    public UnauthorizedException(string message = "Unauthorized")
        : base(message, 401, new List<ApiError> 
        { 
            new ApiError(message, code: "UNAUTHORIZED") 
        })
    {
    }
}

public class ForbiddenException : ApiException
{
    public ForbiddenException(string message = "Access forbidden")
        : base(message, 403, new List<ApiError> 
        { 
            new ApiError(message, code: "FORBIDDEN") 
        })
    {
    }
}

public class BadRequestException : ApiException
{
    public BadRequestException(string message, List<ApiError>? errors = null)
        : base(message, 400, errors ?? new List<ApiError> 
        { 
            new ApiError(message, code: "BAD_REQUEST") 
        })
    {
    }
}

public class ValidationException : ApiException
{
    public ValidationException(List<ApiError> errors)
        : base("Validation failed", 400, errors)
    {
    }
}
