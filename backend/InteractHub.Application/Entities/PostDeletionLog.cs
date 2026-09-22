namespace InteractHub.Application.Entities;

public class PostDeletionLog
{
    public int Id { get; set; }
    public int PostId { get; set; }
    public string UserId { get; set; }
    public string Content { get; set; }
    public string? ImageUrl { get; set; }
    public int? GroupId { get; set; }
    public int ReportId { get; set; }
    public string Reason { get; set; }
    public DateTime DeletedAt { get; set; } = DateTime.UtcNow;
    public string? DeletedByAdminId { get; set; }

    // Navigation properties
    public User? User { get; set; }
    public User? DeletedByAdmin { get; set; }
    public PostReport? Report { get; set; }
}
