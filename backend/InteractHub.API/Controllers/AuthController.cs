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
using InteractHub.Application.Helpers;
using InteractHub.API.DTOs;
using InteractHub.API.DTOs.Response;
using InteractHub.API.Extensions;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<User> _userManager;
    private readonly SignInManager<User> _signInManager;
    private readonly IConfiguration _configuration;

    public AuthController(
        UserManager<User> userManager,
        SignInManager<User> signInManager,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _configuration = configuration;
    }

    // ✅ POST /api/auth/register
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
    {
        // ✅ 1️⃣ VALIDATION - Email format
        if (!ValidationHelper.IsValidEmail(registerDto.Email))
            return this.BadRequestResponse(new List<ApiError> 
            { 
                ErrorHelper.CreateValidationError("email", "Invalid email format") 
            });

        // ✅ 2️⃣ VALIDATION - Username format (3-20 chars, alphanumeric + underscore)
        if (!ValidationHelper.IsValidUsername(registerDto.UserName))
            return this.BadRequestResponse(new List<ApiError> 
            { 
                ErrorHelper.CreateValidationError("username", "Username must be 3-20 characters, alphanumeric and underscore only") 
            });

        // ✅ 3️⃣ VALIDATION - FullName length
        if (!ValidationHelper.IsValidFullName(registerDto.FullName))
            return this.BadRequestResponse(new List<ApiError> 
            { 
                ErrorHelper.CreateValidationError("fullName", "Full name must be 2-100 characters") 
            });

        // ✅ 4️⃣ VALIDATION - Password strength
        if (!ValidationHelper.IsStrongPassword(registerDto.Password))
        {
            var passwordStrength = ValidationHelper.GetPasswordStrength(registerDto.Password);
            var message = passwordStrength < 30 ? "Password is too weak (must contain uppercase, lowercase, number, special char)" :
                         passwordStrength < 60 ? "Password needs improvement (at least 8 chars)" :
                         "Password does not meet requirements";
            
            return this.BadRequestResponse(new List<ApiError> 
            { 
                ErrorHelper.CreateValidationError("password", message) 
            });
        }

        // 5️⃣ Check xem username đã tồn tại chưa
        var existingUser = await _userManager.FindByNameAsync(registerDto.UserName);
        if (existingUser != null)
            return this.BadRequestResponse(new List<ApiError> 
            { 
                ErrorHelper.CreateValidationError("username", "Username already exists") 
            });

        // 6️⃣ Check xem email đã tồn tại chưa
        var existingEmail = await _userManager.FindByEmailAsync(registerDto.Email);
        if (existingEmail != null)
            return this.BadRequestResponse(new List<ApiError> 
            { 
                ErrorHelper.CreateValidationError("email", "Email already registered") 
            });

        // 7️⃣ Tạo user mới
        var user = new User
        {
            UserName = registerDto.UserName,
            Email = registerDto.Email,
            FullName = registerDto.FullName
        };

        // 8️⃣ Hash password và lưu vào database
        var result = await _userManager.CreateAsync(user, registerDto.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.Select(e => 
                new ApiError(e.Description, code: AppErrorCodes.VALIDATION_ERROR)
            ).ToList();
            return this.BadRequestResponse(errors);
        }

        // ✅ 9️⃣ Gán role User mặc định cho user mới
        var roleResult = await _userManager.AddToRoleAsync(user, RoleConstants.User);
        if (!roleResult.Succeeded)
        {
            // Xóa user nếu gán role thất bại
            await _userManager.DeleteAsync(user);
            var errors = roleResult.Errors.Select(e => 
                new ApiError(e.Description, code: AppErrorCodes.INVALID_STATE)
            ).ToList();
            return this.BadRequestResponse(errors);
        }

        // 🔟 Tạo JWT token
        var token = await GenerateJwtToken(user);

        var authData = new
        {
            Token = token,
            User = new UserResponseDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                ProfilePictureUrl = user.ProfilePictureUrl
            }
        };

        return this.CreatedResponse(authData, "Registration successful");
    }

    // ✅ POST /api/auth/login (User Login ONLY - Admin must use /api/admin/login)
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        // 1️⃣ Tìm user theo username
        var user = await _userManager.FindByNameAsync(loginDto.UserName);
        if (user == null)
            return this.UnauthorizedResponse("Invalid username or password");

        // 🔒 SECURITY: Prevent admin users from logging in via regular endpoint
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains(RoleConstants.Admin))
            return this.UnauthorizedResponse("Admin users must use admin login endpoint");

        // 2️⃣ Kiểm tra password
        var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);
        if (!result.Succeeded)
            return this.UnauthorizedResponse("Invalid username or password");

        // 3️⃣ Tạo JWT token (async)
        var token = await GenerateJwtToken(user);

        var authData = new
        {
            Token = token,
            User = new UserResponseDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName ?? string.Empty,
                ProfilePictureUrl = user.ProfilePictureUrl
            }
        };

        return this.SuccessResponse(authData, "Login successful", 200);
    }

    // ✅ Hàm helper để tạo JWT token (bao gồm roles)
    private async Task<string> GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("JWT");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        var issuer = jwtSettings["Issuer"] ?? throw new InvalidOperationException("JWT Issuer is not configured");
        var audience = jwtSettings["Audience"] ?? throw new InvalidOperationException("JWT Audience is not configured");
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

        // 1️⃣ Tạo security key từ secret
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 2️⃣ Lấy roles của user từ database
        var roles = await _userManager.GetRolesAsync(user);

        // 3️⃣ Tạo claims (thông tin user sẽ được encode vào token)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
            new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
            new Claim("FullName", user.FullName ?? string.Empty)
        };

        // ✅ 4️⃣ Thêm roles vào claims (quan trọng cho authorization!)
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
