using System.ComponentModel.DataAnnotations;

namespace InteractHub.API.DTOs;

public class CreateMessageDto
{
    [MaxLength(2000, ErrorMessage = "Nội dung tin nhắn không được vượt quá 2000 ký tự")]
    public string? Content { get; set; }
    public string? ImageUrl { get; set; }
    public string? ReceiverId { get; set; }
    public int? GroupId { get; set; }
}

public class UpdateMessageDto
{
    [Required(ErrorMessage = "Nội dung tin nhắn không được để trống")]
    [MaxLength(2000, ErrorMessage = "Nội dung tin nhắn không được vượt quá 2000 ký tự")]
    public string Content { get; set; } = string.Empty;
}

public class MessageResponseDto
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string SenderId { get; set; } = string.Empty;
    public string SenderName { get; set; } = string.Empty;
    public string? ReceiverId { get; set; }
    public string? ReceiverName { get; set; }
    public int? GroupId { get; set; }
    public string? GroupName { get; set; }
    public bool IsRead { get; set; }
    public bool IsEdited { get; set; }
    public bool IsDeleted { get; set; }
    public string? ImageUrl { get; set; }
}