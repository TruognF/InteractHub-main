# Admin + Report System - Complete Implementation Guide

## BACKEND IMPLEMENTATION

### 1. Fix PostReportsController - Corrected Version

Replace your current PostReportsController.cs with this corrected implementation that matches your existing architecture:

```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InteractHub.Application.Interfaces;
using InteractHub.Application.Entities;
using InteractHub.Application.Entities.Enums;
using InteractHub.API.DTOs;
using InteractHub.API.DTOs.Response;
using InteractHub.API.Extensions;
using AutoMapper;
using System.Security.Claims;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostReportsController : ControllerBase
{
    private readonly IPostReportService _postReportService;
    private readonly IPostService _postService;
    private readonly IUserService _userService;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;

    public PostReportsController(
        IPostReportService postReportService,
        IPostService postService,
        IUserService userService,
        INotificationService notificationService,
        IMapper mapper)
    {
        _postReportService = postReportService;
        _postService = postService;
        _userService = userService;
        _notificationService = notificationService;
        _mapper = mapper;
    }

    /// <summary>
    /// Create a report for a post (user side)
    /// </summary>
    [HttpPost("create")]
    [ProducesResponseType(typeof(ApiResponse<PostReportResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CreateReport([FromBody] CreatePostReportDto dto)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
            return this.UnauthorizedResponse("User not authenticated");

        // 🔒 Security: User cannot report their own post
        var post = await _postService.GetByIdAsync(dto.PostId);
        if (post == null)
            return this.NotFoundResponse("Post not found");

        if (post.UserId == currentUserId)
            return this.ErrorResponse("Không thể báo cáo bài viết của chính mình");

        // 🔒 Security: Check if user already reported this post
        var existingReports = (await _postReportService.GetAllAsync())
            .Where(r => r.PostId == dto.PostId && r.ReporterUserId == currentUserId && r.Status == ReportStatus.Pending)
            .ToList();

        if (existingReports.Any())
            return this.ErrorResponse("Bạn đã báo cáo bài viết này rồi");

        // Create report
        var report = new PostReport
        {
            Reason = dto.Reason,
            Detail = dto.Detail,
            PostId = dto.PostId,
            ReporterUserId = currentUserId,
            Status = ReportStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        var createdReport = await _postReportService.CreateAsync(report);
        var reportDto = _mapper.Map<PostReportResponseDto>(createdReport);

        Console.WriteLine($"[PostReportsController] 📝 Report created: {createdReport.Id} for post {dto.PostId} by user {currentUserId}");

        return this.CreatedResponse(reportDto, "Báo cáo thành công");
    }

    /// <summary>
    /// Get pending reports (admin only)
    /// </summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<PostReportResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetPendingReports()
    {
        var reports = (await _postReportService.GetAllAsync())
            .Where(r => r.Status == ReportStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

        var reportDtos = _mapper.Map<List<PostReportResponseDto>>(reports);
        return this.SuccessResponse(reportDtos);
    }

    /// <summary>
    /// Get all reports with pagination (admin only)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<List<PostReportResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var allReports = await _postReportService.GetAllAsync();
        var reports = allReports
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var reportDtos = _mapper.Map<List<PostReportResponseDto>>(reports);
        return this.SuccessResponse(reportDtos);
    }

    /// <summary>
    /// Get report by ID (admin only)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PostReportResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id)
    {
        var report = await _postReportService.GetByIdAsync(id);
        if (report == null)
            return this.NotFoundResponse("Report not found");

        var reportDto = _mapper.Map<PostReportResponseDto>(report);
        return this.SuccessResponse(reportDto);
    }

    /// <summary>
    /// Approve report and delete post (admin moderation)
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PostReportResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveReport(int id)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return this.UnauthorizedResponse("Admin not authenticated");

        var report = await _postReportService.GetByIdAsync(id);
        if (report == null)
            return this.NotFoundResponse("Report not found");

        if (report.Status != ReportStatus.Pending)
            return this.ErrorResponse("Report đã được xử lý rồi");

        // Update report status
        report.Status = ReportStatus.ApprovedViolation;
        report.ReviewedByAdminId = adminId;
        report.ReviewedAt = DateTime.UtcNow;
        await _postReportService.UpdateAsync(report);

        // Delete the post
        if (report.Post != null)
        {
            await _postService.DeleteAsync(report.Post.Id);
            Console.WriteLine($"[PostReportsController] 🗑️ Post {report.Post.Id} deleted due to violation report");
        }

        // 📢 Send notification to post owner
        var postOwner = await _userService.GetByIdAsync(report.Post?.UserId ?? "");
        if (postOwner != null)
        {
            var notificationContent = $"Bài viết của bạn đã bị gỡ xuống vì: {report.Reason}";
            await _notificationService.CreateAsync(new Notification
            {
                UserId = postOwner.Id,
                Content = notificationContent,
                Type = NotificationType.System,
                CreatedAt = DateTime.UtcNow
            });

            Console.WriteLine($"[PostReportsController] 📬 Notification sent to {postOwner.Id}");
        }

        var reportDto = _mapper.Map<PostReportResponseDto>(report);
        return this.SuccessResponse(reportDto, "Báo cáo được duyệt và bài viết đã bị gỡ");
    }

    /// <summary>
    /// Reject report (admin moderation)
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<PostReportResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectReport(int id)
    {
        var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(adminId))
            return this.UnauthorizedResponse("Admin not authenticated");

        var report = await _postReportService.GetByIdAsync(id);
        if (report == null)
            return this.NotFoundResponse("Report not found");

        if (report.Status != ReportStatus.Pending)
            return this.ErrorResponse("Report đã được xử lý rồi");

        // Update report status
        report.Status = ReportStatus.Rejected;
        report.ReviewedByAdminId = adminId;
        report.ReviewedAt = DateTime.UtcNow;
        await _postReportService.UpdateAsync(report);

        Console.WriteLine($"[PostReportsController] ✅ Report {id} rejected by admin {adminId}");

        var reportDto = _mapper.Map<PostReportResponseDto>(report);
        return this.SuccessResponse(reportDto, "Báo cáo bị từ chối");
    }
}
```

### 2. Update IPostReportService - Add UpdateAsync

```csharp
// In InteractHub.Application/Interfaces/IPostReportService.cs
// Add this method to the interface:
Task<bool> UpdateAsync(PostReport report);
```

### 3. Update PostReportService - Implement UpdateAsync

In PostReportService.cs, add:

```csharp
public async Task<bool> UpdateAsync(PostReport report)
{
    _context.PostReports.Update(report);
    await _context.SaveChangesAsync();
    return true;
}
```

### 4. Create Migration

```bash
# Run from backend folder
dotnet ef migrations add EnhancePostReportWithStatusAndDetails -p InteractHub.Infrastructure -s InteractHub.API

# Apply migration
dotnet ef database update -p InteractHub.Infrastructure -s InteractHub.API
```

### 5. Setup AutoMapper Profile

Add this to your AutoMapper profile (usually in Program.cs or a MappingProfile class):

```csharp
// In AutoMapper configuration
CreateMap<PostReport, PostReportResponseDto>()
    .ForMember(dest => dest.Post, opt => opt.MapFrom(src => src.Post))
    .ForMember(dest => dest.ReporterUser, opt => opt.MapFrom(src => src.ReporterUser))
    .ForMember(dest => dest.ReviewedByAdmin, opt => opt.MapFrom(src => src.ReviewedByAdmin));

CreateMap<Post, PostResponseDto>();
CreateMap<User, UserResponseDto>();
```

---

## FRONTEND IMPLEMENTATION - COMING NEXT

Due to token limits, I'll provide the complete frontend implementation in the next message, including:

1. **AdminLoginPage.jsx** - Admin login form
2. **AdminLayout.jsx** - Admin dashboard layout
3. **AdminDashboard.jsx** - Admin home page
4. **AdminReportsPage.jsx** - Report management interface
5. **ReportPostModal.jsx** - Report submission form
6. **Update HomePage.jsx** - Add report button for others' posts
7. **Protected routes** for admin access

---

## KEY ARCHITECTURE DECISIONS

**Backend:**
- PostReport entity: Enhanced with Status, Detail, ReviewedAt, ReviewedByAdminId fields
- Role-based authorization: `[Authorize(Roles = "Admin")]` on admin endpoints
- Security checks: User can't report own post, duplicate report prevention
- Soft delete via DeleteAsync (existing implementation)

**Frontend:**
- Admin login separate from user login at `/admin/login`
- Report modal component for submitting reports
- Admin dashboard to review and moderate pending reports
- Real-time notifications when posts are removed

---

## NEXT STEPS

1. ✅ Backend report system API
2. ⏳ Frontend admin & report UI (coming next)
3. ⏳ Admin role seed data
4. ⏳ Frontend auth token management for admin
5. ⏳ Integration & testing

Would you like me to continue with the frontend implementation?
