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
    private readonly IPostDeletionLogService _postDeletionLogService;
    private readonly IMapper _mapper;

    public PostReportsController(
        IPostReportService postReportService,
        IPostService postService,
        IUserService userService,
        INotificationService notificationService,
        IPostDeletionLogService postDeletionLogService,
        IMapper mapper)
    {
        _postReportService = postReportService;
        _postService = postService;
        _userService = userService;
        _notificationService = notificationService;
        _postDeletionLogService = postDeletionLogService;
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
        var existingReport = (await _postReportService.GetAllAsync())
            .FirstOrDefault(r => r.PostId == dto.PostId && r.ReporterUserId == currentUserId && r.Status == ReportStatus.Pending);

        if (existingReport != null)
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
        
        // Manual mapping to avoid AutoMapper issues
        var reportDto = new PostReportResponseDto
        {
            Id = createdReport.Id,
            Reason = createdReport.Reason,
            Detail = createdReport.Detail,
            Status = createdReport.Status,
            PostId = createdReport.PostId,
            ReporterUserId = createdReport.ReporterUserId,
            CreatedAt = createdReport.CreatedAt,
            ReviewedAt = createdReport.ReviewedAt,
            ReviewedByAdminId = createdReport.ReviewedByAdminId
        };

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

        // Manual mapping to avoid AutoMapper issues
        var reportDtos = reports.Select(r => new PostReportResponseDto
        {
            Id = r.Id,
            Reason = r.Reason,
            Detail = r.Detail,
            Status = r.Status,
            PostId = r.PostId,
            ReporterUserId = r.ReporterUserId,
            CreatedAt = r.CreatedAt,
            ReviewedAt = r.ReviewedAt,
            ReviewedByAdminId = r.ReviewedByAdminId
        }).ToList();

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

        // Manual mapping to avoid AutoMapper issues
        var reportDtos = reports.Select(r => new PostReportResponseDto
        {
            Id = r.Id,
            Reason = r.Reason,
            Detail = r.Detail,
            Status = r.Status,
            PostId = r.PostId,
            ReporterUserId = r.ReporterUserId,
            CreatedAt = r.CreatedAt,
            ReviewedAt = r.ReviewedAt,
            ReviewedByAdminId = r.ReviewedByAdminId
        }).ToList();
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

        // Manual mapping to avoid AutoMapper issues
        var reportDto = new PostReportResponseDto
        {
            Id = report.Id,
            Reason = report.Reason,
            Detail = report.Detail,
            Status = report.Status,
            PostId = report.PostId,
            ReporterUserId = report.ReporterUserId,
            CreatedAt = report.CreatedAt,
            ReviewedAt = report.ReviewedAt,
            ReviewedByAdminId = report.ReviewedByAdminId
        };
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
        try
        {
            Console.WriteLine($"[PostReportsController] 🔍 Starting ApproveReport for report {id}");
            
            var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(adminId))
                return this.UnauthorizedResponse("Admin not authenticated");

            var report = await _postReportService.GetByIdAsync(id);
            if (report == null)
                return this.NotFoundResponse("Report not found");

            Console.WriteLine($"[PostReportsController] 📋 Report found: Id={report.Id}, Status={report.Status}, PostId={report.PostId}");

            if (report.Status != ReportStatus.Pending)
                return this.ErrorResponse("Report đã được xử lý rồi");

            // Step 1: Update report status FIRST
            report.Status = ReportStatus.ApprovedViolation;
            report.ReviewedByAdminId = adminId;
            report.ReviewedAt = DateTime.UtcNow;
            await _postReportService.UpdateAsync(report);
            Console.WriteLine($"[PostReportsController] ✅ Report {id} status updated to ApprovedViolation");

            // Step 2: Capture post info BEFORE any deletion
            string postOwnerId = report.Post?.UserId ?? "";
            int? postId = report.Post?.Id;
            string? postContent = report.Post?.Content;
            string? postImageUrl = report.Post?.ImageUrl;
            int? postGroupId = report.Post?.GroupId;
            
            Console.WriteLine($"[PostReportsController] 👤 Post owner ID: {postOwnerId}, Post ID: {postId}");

            // Step 3: Save post deletion log
            if (postId.HasValue && !string.IsNullOrEmpty(postOwnerId))
            {
                try
                {
                    var deletionLog = new PostDeletionLog
                    {
                        PostId = postId.Value,
                        UserId = postOwnerId,
                        Content = postContent ?? "",
                        ImageUrl = postImageUrl,
                        GroupId = postGroupId,
                        ReportId = report.Id,
                        Reason = report.Reason.ToString(),
                        DeletedAt = DateTime.UtcNow,
                        DeletedByAdminId = adminId
                    };
                    await _postDeletionLogService.CreateAsync(deletionLog);
                    Console.WriteLine($"[PostReportsController] 📝 Post deletion log saved for post {postId}");
                }
                catch (Exception logEx)
                {
                    Console.WriteLine($"[PostReportsController] ⚠️ Failed to save deletion log: {logEx.Message}");
                    Console.WriteLine($"[PostReportsController] ⚠️ Log error stack: {logEx.StackTrace}");
                    // Don't fail the whole process if logging fails
                }
            }

            // Step 4: Send notification to post owner
            if (!string.IsNullOrEmpty(postOwnerId))
            {
                try
                {
                    Console.WriteLine($"[PostReportsController] 📧 Fetching post owner user {postOwnerId}");
                    var postOwner = await _userService.GetByIdAsync(postOwnerId);
                    if (postOwner != null)
                    {
                        Console.WriteLine($"[PostReportsController] 📧 Post owner found: {postOwner.UserName}");
                        var reasonText = report.Reason.ToString();
                        Console.WriteLine($"[PostReportsController] 📧 Report reason: {reasonText}");
                        
                        var notificationContent = $"Bài viết của bạn đã bị gỡ xuống vì: {reasonText}";
                        Console.WriteLine($"[PostReportsController] 📧 Creating notification with content: {notificationContent}");
                        
                        if (postOwner.Id == null)
                        {
                            Console.WriteLine($"[PostReportsController] ⚠️ Post owner ID is null!");
                            throw new InvalidOperationException("Post owner ID is null");
                        }
                        
                        await _notificationService.CreateAsync(new Notification
                        {
                            UserId = postOwner.Id,
                            Content = notificationContent,
                            Type = NotificationType.System,
                            CreatedAt = DateTime.UtcNow
                        });
                        Console.WriteLine($"[PostReportsController] 📬 Notification sent to {postOwner.Id}");
                    }
                    else
                    {
                        Console.WriteLine($"[PostReportsController] ⚠️ Post owner user not found: {postOwnerId}");
                    }
                }
                catch (Exception notifEx)
                {
                    Console.WriteLine($"[PostReportsController] ⚠️ Failed to send notification: {notifEx.Message}");
                    Console.WriteLine($"[PostReportsController] ⚠️ Notification error stack trace: {notifEx.StackTrace}");
                    // Don't fail the whole process if notification fails
                }
            }
            else
            {
                Console.WriteLine($"[PostReportsController] ⚠️ Post owner ID is empty, skipping notification");
            }

            // Step 5: DELETE THE POST LAST (after all other operations succeed)
            if (postId.HasValue && report.Post != null)
            {
                try
                {
                    Console.WriteLine($"[PostReportsController] 🗑️ Deleting post {postId}");
                    await _postService.DeleteAsync(postId.Value);
                    Console.WriteLine($"[PostReportsController] ✅ Post {postId} deleted due to violation report");
                }
                catch (Exception deleteEx)
                {
                    Console.WriteLine($"[PostReportsController] ❌ Failed to delete post: {deleteEx.Message}");
                    Console.WriteLine($"[PostReportsController] ❌ Delete error stack: {deleteEx.StackTrace}");
                    throw; // Rethrow to be caught by outer try-catch
                }
            }
            else
            {
                Console.WriteLine($"[PostReportsController] ⚠️ Post ID is null or Report.Post is null, skipping post deletion");
            }

            // Step 6: Return success response
            var reportDto = new PostReportResponseDto
            {
                Id = report.Id,
                Reason = report.Reason,
                Detail = report.Detail,
                Status = report.Status,
                PostId = report.PostId,
                ReporterUserId = report.ReporterUserId,
                CreatedAt = report.CreatedAt,
                ReviewedAt = report.ReviewedAt,
                ReviewedByAdminId = report.ReviewedByAdminId
            };
            Console.WriteLine($"[PostReportsController] ✅ Report {id} approval completed successfully");
            return this.SuccessResponse(reportDto, "Báo cáo được duyệt và bài viết đã bị gỡ");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[PostReportsController] ❌ Error approving report {id}: {ex.Message}");
            Console.WriteLine($"[PostReportsController] ❌ Stack trace: {ex.StackTrace}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"[PostReportsController] ❌ Inner exception: {ex.InnerException.Message}");
                Console.WriteLine($"[PostReportsController] ❌ Inner stack trace: {ex.InnerException.StackTrace}");
            }
            return this.ErrorResponse($"Lỗi khi duyệt báo cáo: {ex.Message}");
        }
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

        // Manual mapping to avoid AutoMapper issues
        var reportDto = new PostReportResponseDto
        {
            Id = report.Id,
            Reason = report.Reason,
            Detail = report.Detail,
            Status = report.Status,
            PostId = report.PostId,
            ReporterUserId = report.ReporterUserId,
            CreatedAt = report.CreatedAt,
            ReviewedAt = report.ReviewedAt,
            ReviewedByAdminId = report.ReviewedByAdminId
        };
        return this.SuccessResponse(reportDto, "Báo cáo bị từ chối");
    }
}
