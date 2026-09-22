using InteractHub.Application.DTOs;
using InteractHub.Application.Entities.Enums;

namespace InteractHub.API.DTOs;

public class CreatePostReportDto
{
    public ReportReason Reason { get; set; }
    public string? Detail { get; set; }
    public int PostId { get; set; }
}

public class PostReportResponseDto
{
    public int Id { get; set; }
    public ReportReason Reason { get; set; }
    public string? Detail { get; set; }
    public ReportStatus Status { get; set; }
    public int PostId { get; set; }
    public PostResponseDto? Post { get; set; }
    public string ReporterUserId { get; set; } = string.Empty;
    public UserResponseDto? ReporterUser { get; set; }
    public string? ReviewedByAdminId { get; set; }
    public UserResponseDto? ReviewedByAdmin { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}

public class ApproveReportDto
{
    public int ReportId { get; set; }
}

public class RejectReportDto
{
    public int ReportId { get; set; }
}
