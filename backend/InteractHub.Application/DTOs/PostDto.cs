namespace InteractHub.Application.DTOs;

public class SharedPostDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? UserFullName { get; set; }
    public string? UserProfilePictureUrl { get; set; }
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public List<string> LikedByUserIds { get; set; } = new();
}

public class PostResponseDto
{
    public int Id { get; set; }
    public int? GroupId { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? UserName { get; set; }
    public string? UserFullName { get; set; }
    public string? UserProfilePictureUrl { get; set; }
    public int LikesCount { get; set; }
    public int CommentsCount { get; set; }
    public List<string> LikedByUserIds { get; set; } = new();

    // Share Post feature
    public bool IsShared { get; set; }
    public int? SharedPostId { get; set; }
    public SharedPostDto? SharedPost { get; set; }
}

public class PostLiteDto
{
    public string UserId { get; set; } = string.Empty;
    public int? GroupId { get; set; }
}