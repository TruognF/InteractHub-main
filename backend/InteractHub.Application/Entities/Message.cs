namespace InteractHub.Application.Entities;

public class Message
{
    public int Id { get; set; }
    public string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string SenderId { get; set; }
    public User Sender { get; set; }

    public string? ReceiverId { get; set; }
    public User? Receiver { get; set; }

    public int? GroupId { get; set; }
    public Group? Group { get; set; }

    public bool IsRead { get; set; } = false;
}