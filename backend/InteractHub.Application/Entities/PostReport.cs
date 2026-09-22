using InteractHub.Application.Entities.Enums;

namespace InteractHub.Application.Entities;
public class PostReport
{
    public int Id {get; set;}
    public ReportReason Reason {get; set;}
    public string? Detail {get; set;}
    public ReportStatus Status {get; set;} = ReportStatus.Pending;

    public int PostId {get; set;}
    public Post Post {get; set;}

    public string ReporterUserId {get; set;}
    public User ReporterUser {get; set;}

    public string? ReviewedByAdminId {get; set;}
    public User? ReviewedByAdmin {get; set;}

    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
    public DateTime? ReviewedAt {get; set;}
}