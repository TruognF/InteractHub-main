namespace InteractHub.API.DTOs;

public class CreateMessageDto
{
    public string Content { get; set; } = string.Empty;
    public string? ReceiverId { get; set; }
    public int? GroupId { get; set; }
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
}