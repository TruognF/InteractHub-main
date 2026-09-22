namespace InteractHub.Application.Entities;

public class GroupMembership
{
    public int GroupId { get; set; }
    public Group? Group { get; set; }

    public string UserId { get; set; } = string.Empty;
    public User? User { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
