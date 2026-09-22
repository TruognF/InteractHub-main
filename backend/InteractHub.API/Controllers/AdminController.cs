using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using InteractHub.Application.Entities;
using InteractHub.Application.Constants;
using InteractHub.API.DTOs;
using InteractHub.API.DTOs.Response;
using InteractHub.API.Extensions;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "AdminOnly")] // ✅ Chỉ Admin có quyền truy cập
public class AdminController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    public AdminController(
        UserManager<User> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    // ✅ GET /api/admin/users-with-roles
    [HttpGet("users-with-roles")]
    [ProducesResponseType(typeof(ApiResponse<List<UserWithRolesDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllUsersWithRoles()
    {
        var users = _userManager.Users.ToList();
        var usersWithRoles = new List<UserWithRolesDto>();

        foreach (var user in users)
        {
            var roles = await _userManager.GetRolesAsync(user);
            usersWithRoles.Add(new UserWithRolesDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                FullName = user.FullName,
                Roles = roles.ToList()
            });
        }

        return this.SuccessResponse(usersWithRoles, "Users retrieved successfully");
    }

    // ✅ POST /api/admin/assign-role
    [HttpPost("assign-role")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleDto dto)
    {
        // 1️⃣ Kiểm tra user tồn tại
        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null)
            return this.ErrorResponse("User not found", statusCode: 404);

        // 2️⃣ Kiểm tra role tồn tại
        var roleExists = await _roleManager.RoleExistsAsync(dto.Role);
        if (!roleExists)
            return this.ErrorResponse($"Role '{dto.Role}' does not exist", statusCode: 400);

        // 3️⃣ Kiểm tra user đã có role này chưa
        var userHasRole = await _userManager.IsInRoleAsync(user, dto.Role);
        if (userHasRole)
            return this.ErrorResponse($"User already has role '{dto.Role}'", statusCode: 400);

        // 4️⃣ Gán role
        var result = await _userManager.AddToRoleAsync(user, dto.Role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return this.ErrorResponse($"Failed to assign role: {errors}", statusCode: 400);
        }

        return this.SuccessResponse(new { message = $"Role '{dto.Role}' assigned to {user.UserName}" });
    }

    // ✅ DELETE /api/admin/remove-role
    [HttpDelete("remove-role")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveRoleFromUser([FromBody] RemoveRoleDto dto)
    {
        // 1️⃣ Kiểm tra user tồn tại
        var user = await _userManager.FindByIdAsync(dto.UserId);
        if (user == null)
            return this.ErrorResponse("User not found", statusCode: 404);

        // 2️⃣ Kiểm tra user có role này không
        var userHasRole = await _userManager.IsInRoleAsync(user, dto.Role);
        if (!userHasRole)
            return this.ErrorResponse($"User does not have role '{dto.Role}'", statusCode: 400);

        // ✅ 3️⃣ Không cho phép remove Admin role từ chính user hiện tại
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId == user.Id && dto.Role == RoleConstants.Admin)
            return this.ErrorResponse("Cannot remove Admin role from yourself", statusCode: 400);

        // 4️⃣ Remove role
        var result = await _userManager.RemoveFromRoleAsync(user, dto.Role);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return this.ErrorResponse($"Failed to remove role: {errors}", statusCode: 400);
        }

        return this.SuccessResponse(new { message = $"Role '{dto.Role}' removed from {user.UserName}" });
    }

    // ✅ GET /api/admin/user/{userId}
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<UserWithRolesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUserWithRoles(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return this.ErrorResponse("User not found", statusCode: 404);

        var roles = await _userManager.GetRolesAsync(user);
        var userWithRoles = new UserWithRolesDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            FullName = user.FullName,
            Roles = roles.ToList()
        };

        return this.SuccessResponse(userWithRoles);
    }

    // ✅ DELETE /api/admin/delete-user/{userId}
    [HttpDelete("delete-user/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return this.ErrorResponse("User not found", statusCode: 404);

        // ✅ Không cho phép xóa chính mình
        var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId == user.Id)
            return this.ErrorResponse("Cannot delete your own account", statusCode: 400);

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return this.ErrorResponse($"Failed to delete user: {errors}", statusCode: 400);
        }

        return this.SuccessResponse(new { message = $"User '{user.UserName}' deleted successfully" });
    }
}
