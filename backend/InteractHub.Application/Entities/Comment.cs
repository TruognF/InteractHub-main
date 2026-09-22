namespace InteractHub.Application.Entities;
public class Comment
{
    public int Id { get; set; }
    public string Content { get; set; }

    public int PostId { get; set; }
    public Post Post { get; set; }

    public string UserId { get; set; }
    public User User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}




























































