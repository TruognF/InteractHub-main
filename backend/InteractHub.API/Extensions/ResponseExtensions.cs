using InteractHub.API.DTOs.Response;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.API.Extensions;

public static class ResponseExtensions
{
    // Success responses
    public static IActionResult SuccessResponse<T>(this ControllerBase controller, T? data, string message = "Success", int statusCode = 200)
    {
        var response = new ApiResponse<T>(true, message, data, statusCode);
        return new ObjectResult(response) { StatusCode = statusCode };
    }

    public static IActionResult SuccessResponse(this ControllerBase controller, string message = "Success", int statusCode = 200)
    {
        var response = new ApiResponse(true, message, null, statusCode);
        return new ObjectResult(response) { StatusCode = statusCode };
    }

    public static IActionResult CreatedResponse<T>(this ControllerBase controller, T? data, string message = "Resource created successfully")
    {
        var response = new ApiResponse<T>(true, message, data, 201);
        return new ObjectResult(response) { StatusCode = 201 };
    }

    // Error responses
    public static IActionResult ErrorResponse(this ControllerBase controller, string message, List<ApiError>? errors = null, int statusCode = 400)
    {
        var response = new ApiResponse(false, message, null, statusCode, errors);
        return new ObjectResult(response) { StatusCode = statusCode };
    }

    public static IActionResult ErrorResponse<T>(this ControllerBase controller, string message, List<ApiError>? errors = null, int statusCode = 400)
    {
        var response = new ApiResponse<T>(false, message, default, statusCode, errors);
        return new ObjectResult(response) { StatusCode = statusCode };
    }

    public static IActionResult NotFoundResponse(this ControllerBase controller, string message = "Resource not found")
    {
        var errors = new List<ApiError> { new ApiError(message, code: "NOT_FOUND") };
        var response = new ApiResponse(false, message, null, 404, errors);
        return new ObjectResult(response) { StatusCode = 404 };
    }

    public static IActionResult UnauthorizedResponse(this ControllerBase controller, string message = "Unauthorized")
    {
        var errors = new List<ApiError> { new ApiError(message, code: "UNAUTHORIZED") };
        var response = new ApiResponse(false, message, null, 401, errors);
        return new ObjectResult(response) { StatusCode = 401 };
    }

    public static IActionResult ForbiddenResponse(this ControllerBase controller, string message = "Access forbidden")
    {
        var errors = new List<ApiError> { new ApiError(message, code: "FORBIDDEN") };
        var response = new ApiResponse(false, message, null, 403, errors);
        return new ObjectResult(response) { StatusCode = 403 };
    }

    public static IActionResult BadRequestResponse(this ControllerBase controller, List<ApiError> errors)
    {
        var response = new ApiResponse(false, "Validation failed", null, 400, errors);
        return new ObjectResult(response) { StatusCode = 400 };
    }

    public static IActionResult ServerErrorResponse(this ControllerBase controller, string message = "An error occurred processing your request")
    {
        var errors = new List<ApiError> { new ApiError(message, code: "INTERNAL_ERROR") };
        var response = new ApiResponse(false, message, null, 500, errors);
        return new ObjectResult(response) { StatusCode = 500 };
    }
}
