using System.Net;
using System.Text.Json;
using InteractHub.API.DTOs.Response;
using InteractHub.API.Exceptions;

namespace InteractHub.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ApiResponse(false, "An error occurred processing your request", null, 500);

        switch (exception)
        {
            // Custom API Exceptions
            case NotFoundException notFoundEx:
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                response = new ApiResponse(false, notFoundEx.Message, null, 404, notFoundEx.Errors);
                _logger.LogWarning($"Not found: {notFoundEx.Message}");
                break;

            case UnauthorizedException unauthorizedEx:
                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                response = new ApiResponse(false, unauthorizedEx.Message, null, 401, unauthorizedEx.Errors);
                _logger.LogWarning($"Unauthorized: {unauthorizedEx.Message}");
                break;

            case ForbiddenException forbiddenEx:
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                response = new ApiResponse(false, forbiddenEx.Message, null, 403, forbiddenEx.Errors);
                _logger.LogWarning($"Forbidden: {forbiddenEx.Message}");
                break;

            case BadRequestException badRequestEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ApiResponse(false, badRequestEx.Message, null, 400, badRequestEx.Errors);
                _logger.LogWarning($"Bad request: {badRequestEx.Message}");
                break;

            case ValidationException validationEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ApiResponse(false, validationEx.Message, null, 400, validationEx.Errors);
                _logger.LogWarning($"Validation failed with {validationEx.Errors?.Count} errors");
                break;

            // Database Exceptions
            case InvalidOperationException invalidOpEx:
                context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                response = new ApiResponse(
                    false,
                    "Invalid operation",
                    null,
                    400,
                    new List<ApiError> { new ApiError(invalidOpEx.Message, code: "INVALID_OPERATION") }
                );
                _logger.LogError($"Invalid operation: {invalidOpEx.Message}");
                break;

            // Generic Exception
            default:
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response = new ApiResponse(
                    false,
                    "An unexpected error occurred. Please try again later.",
                    null,
                    500,
                    new List<ApiError> { new ApiError("Internal server error", code: "INTERNAL_ERROR") }
                );
                _logger.LogError(exception, $"Unhandled exception: {exception.Message}\n{exception.StackTrace}");
                break;
        }

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return context.Response.WriteAsync(jsonResponse);
    }
}
