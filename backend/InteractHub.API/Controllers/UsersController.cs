using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using InteractHub.Application.Interfaces;
using InteractHub.Application.Entities;
using InteractHub.Application.Helpers;
using InteractHub.API.DTOs;
using InteractHub.API.DTOs.Response;
using InteractHub.API.Extensions;
using InteractHub.Infrastructure.Hubs;
using System.Security.Claims;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IHubContext<UserHub> _userHub;
    private readonly IImageStorageService _imageStorage;

    public UsersController(IUserService userService, IHubContext<UserHub> userHub, IImageStorageService imageStorage)
    {
        _userService = userService;
        _userHub = userHub;
        _imageStorage = imageStorage;
    }

    /// <summary>
    /// Get all users with optional search filter
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] string? search = null)
    {
        try
        {
            var sanitizedSearch = string.IsNullOrWhiteSpace(search)
                ? null
                : QueryHelper.ValidateAndSanitizeSearchTerm(search);

            var users = await _userService.GetUsersAsync(sanitizedSearch, 50);

            var userDtos = users.Select(u => new UserResponseDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                FullName = u.FullName,
                ProfilePictureUrl = u.ProfilePictureUrl,
                Bio = u.Bio
            }).ToList();

            // Return just the array, not wrapped in metadata object
            return this.SuccessResponse(userDtos, "Users retrieved successfully", 200);
        }
        catch (Exception ex)
        {
            return this.BadRequestResponse(new List<ApiError> 
            { 
                ErrorHelper.CreateValidationError("search", ex.Message) 
            });
        }
    }

    /// <summary>
    /// Batch fetch users by ids (avoids N+1 requests)
    /// </summary>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBatch([FromBody] BatchUserIdsDto dto)
    {
        var ids = dto?.UserIds ?? new List<string>();
        var users = await _userService.GetByIdsAsync(ids);

        var userDtos = users.Select(u => new UserResponseDto
        {
            Id = u.Id,
            UserName = u.UserName,
            Email = u.Email,
            FullName = u.FullName,
            ProfilePictureUrl = u.ProfilePictureUrl,
            Bio = u.Bio
        }).ToList();

        return this.SuccessResponse(userDtos, "Users retrieved successfully", 200);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(string id)
    {
        var user = await _userService.GetByIdAsync(id);
        if (user == null)
            return this.NotFoundResponse("User not found");

        var userDto = new UserResponseDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FullName = user.FullName,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Bio = user.Bio
        };

        return this.SuccessResponse(userDto, "User retrieved successfully", 200);
    }

    [HttpGet("email/{email}")]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByEmail(string email)
    {
        var user = await _userService.GetByEmailAsync(email);
        if (user == null)
            return this.NotFoundResponse("User not found");

        var userDto = new UserResponseDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FullName = user.FullName,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Bio = user.Bio
        };

        return this.SuccessResponse(userDto, "User retrieved successfully", 200);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(string id, [FromBody] UpdateUserDto updateUserDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (id != userId)
            return this.ForbiddenResponse("You cannot update this user");

        var user = await _userService.GetByIdAsync(id);
        if (user == null)
            return this.NotFoundResponse("User not found");

        user.FullName = updateUserDto.FullName;
        user.ProfilePictureUrl = updateUserDto.ProfilePictureUrl;
        user.Bio = updateUserDto.Bio;

        await _userService.UpdateAsync(user);

        // 🔔 Emit profile update event via SignalR
        // This will notify all clients about this user's profile change
        var userDto = new UserResponseDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FullName = user.FullName,
            ProfilePictureUrl = user.ProfilePictureUrl,
            Bio = user.Bio
        };

        await _userHub.Clients.All.SendAsync("UserProfileUpdated", userDto);
        Console.WriteLine($"[UsersController] 📡 Emitted UserProfileUpdated for user {id}");

        return this.SuccessResponse(message: "User updated successfully", statusCode: 200);
    }

    [HttpPost("{id}/upload-profile-picture")]
    [ProducesResponseType(typeof(ApiResponse<UserResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadProfilePicture([FromRoute] string id, [FromForm] IFormFile? file)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (id != userId)
            return this.ForbiddenResponse("You cannot update this user");

        // Validate file
        if (file == null || file.Length == 0)
            return this.BadRequestResponse(new List<ApiError> 
            { 
                ErrorHelper.CreateValidationError("file", "Please select a file to upload") 
            });

        // Check file size (max 5MB)
        const long maxFileSize = 5 * 1024 * 1024; // 5MB
        if (file.Length > maxFileSize)
            return this.BadRequestResponse(new List<ApiError> 
            { 
                ErrorHelper.CreateValidationError("file", "File size must not exceed 5MB") 
            });

        // Check file type
        var allowedMimeTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
        if (!allowedMimeTypes.Contains(file.ContentType.ToLower()))
            return this.BadRequestResponse(new List<ApiError> 
            { 
                ErrorHelper.CreateValidationError("file", "Only image files (JPEG, PNG, GIF, WebP) are allowed") 
            });

        try
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return this.NotFoundResponse("User not found");

            var extension = Path.GetExtension(file.FileName);
            user.ProfilePictureUrl = await _imageStorage.SaveImageAsync(
                file.OpenReadStream(),
                "avatars",
                extension,
                user.ProfilePictureUrl);

            await _userService.UpdateAsync(user);

                var userDto = new UserResponseDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    FullName = user.FullName,
                    ProfilePictureUrl = user.ProfilePictureUrl,
                    Bio = user.Bio
                };

                // 🔔 Emit profile update event via SignalR
                await _userHub.Clients.All.SendAsync("UserProfileUpdated", userDto);
                Console.WriteLine($"[UsersController] 📡 Emitted UserProfileUpdated for user {id} (profile picture)");

                return this.SuccessResponse(userDto, "Profile picture uploaded successfully", 200);
        }
        catch (Exception ex)
        {
            return this.BadRequestResponse(new List<ApiError> 
            { 
                ErrorHelper.CreateValidationError("file", $"Error uploading file: {ex.Message}") 
            });
        }
    }
}