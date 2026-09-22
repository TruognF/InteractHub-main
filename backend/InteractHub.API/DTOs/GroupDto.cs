namespace InteractHub.API.DTOs;

public class CreateGroupDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> MemberIds { get; set; } = new List<string>();
}

public class GroupResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string CreatorId { get; set; } = string.Empty;
    public bool IsJoined { get; set; }
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
