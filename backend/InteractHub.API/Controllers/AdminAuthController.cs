using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using InteractHub.Application.Entities;
using InteractHub.Application.Constants;
using InteractHub.API.DTOs;
using InteractHub.API.DTOs.Response;
using InteractHub.API.Extensions;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminAuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;

    public AdminAuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    /// <summary>
    /// Admin login endpoint - Only Admin users can login here
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AdminLogin([FromBody] LoginDto loginDto)
    {
        // 1️⃣ Tìm user theo username
        var user = await _userManager.FindByNameAsync(loginDto.UserName);
        if (user == null)
            return this.UnauthorizedResponse("Invalid username or password");

        // 2️⃣ Kiểm tra password
        var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
        if (!result.Succeeded)
            return this.UnauthorizedResponse("Invalid username or password");

        // 🔒 SECURITY: Only allow users with Admin role
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains(RoleConstants.Admin))
        {
            Console.WriteLine($"[AdminAuthController] ⚠️ Non-admin user '{user.UserName}' attempted admin login");
            return this.ForbiddenResponse("Only admin users can access this endpoint");
        }

        // 3️⃣ Tạo JWT token
        var token = await GenerateAdminJwtToken(user);

        var authData = new
        {
            Token = token,
            Admin = new
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                ProfilePictureUrl = user.ProfilePictureUrl,
                IsAdmin = true
            }
        };

        Console.WriteLine($"[AdminAuthController] ✅ Admin user '{user.UserName}' successfully logged in");
        return this.SuccessResponse(authData, "Admin login successful", 200);
    }

    /// <summary>
    /// Get current admin info (requires admin authentication)
    /// </summary>
    [HttpGet("me")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAdminInfo()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return this.UnauthorizedResponse("Admin not authenticated");

        var admin = await _userManager.FindByIdAsync(userId);
        if (admin == null)
            return this.NotFoundResponse("Admin not found");

        var adminData = new
        {
            Id = admin.Id,
            UserName = admin.UserName,
            Email = admin.Email,
            FullName = admin.FullName,
            ProfilePictureUrl = admin.ProfilePictureUrl,
            IsAdmin = true
        };

        return this.SuccessResponse(adminData);
    }

    /// <summary>
    /// Refresh admin token
    /// </summary>
    [HttpPost("refresh-token")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RefreshAdminToken()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return this.UnauthorizedResponse("Admin not authenticated");

        var admin = await _userManager.FindByIdAsync(userId);
        if (admin == null)
            return this.NotFoundResponse("Admin not found");

        var roles = await _userManager.GetRolesAsync(admin);
        if (!roles.Contains(RoleConstants.Admin))
            return this.ForbiddenResponse("User is not an admin");

        var newToken = await GenerateAdminJwtToken(admin);

        return this.SuccessResponse(new { Token = newToken }, "Token refreshed successfully");
    }

    /// <summary>
    /// Generate JWT token for admin user
    /// </summary>
    private async Task<string> GenerateAdminJwtToken(User admin)
    {
        var jwtSettings = _configuration.GetSection("JWT");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured");
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "120"); // Longer expiration for admins

        // 1️⃣ Tạo security key từ secret
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 2️⃣ Lấy roles của user từ database
        var roles = await _userManager.GetRolesAsync(admin);

        // 3️⃣ Tạo claims
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, admin.Id),
            new Claim(ClaimTypes.Name, admin.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, admin.Email ?? string.Empty),
            new Claim("FullName", admin.FullName ?? string.Empty),
            new Claim("IsAdmin", "true")
        };

        // ✅ 4️⃣ Thêm roles vào claims (Admin role)
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        // 5️⃣ Tạo JWT token
        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        // 6️⃣ Convert token thành string
        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }
}
