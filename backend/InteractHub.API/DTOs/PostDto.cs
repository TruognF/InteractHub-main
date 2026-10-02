using System.ComponentModel.DataAnnotations;

namespace InteractHub.API.DTOs;

public class CreatePostDto
{
    [Required(ErrorMessage = "Content không được để trống")]
    [MaxLength(5000, ErrorMessage = "Content không được vượt quá 5000 ký tự")]
    public string Content { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int? GroupId { get; set; }
    public int? SharedPostId { get; set; }
    // public string UserId { get; set; } = string.Empty;
}

public class UpdatePostDto{
    [Required(ErrorMessage = "Content không được để trống")]
    [MaxLength(5000, ErrorMessage = "Content không được vượt quá 5000 ký tự")]
    public string Content {get; set;} = string.Empty;
    public string? ImageUrl {get; set;}
}