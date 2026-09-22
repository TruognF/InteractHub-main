namespace InteractHub.API.DTOs;

public class CreatePostDto
{
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int? GroupId { get; set; }
    public int? SharedPostId { get; set; }
    // public string UserId { get; set; } = string.Empty;
}

public class UpdatePostDto{
    public string Content {get; set;} = string.Empty;
    public string? ImageUrl {get; set;}
}