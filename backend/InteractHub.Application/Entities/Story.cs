namespace InteractHub.Application.Entities;
public class Story
{
    public int Id {get; set;}
    public string? ImageUrl {get; set;}
    public string? Content {get; set;}

    public DateTime CreatedAt {get; set;} = DateTime.UtcNow;
    public DateTime ExpireAt {get; set;}

    public string UserId {get; set;}
    public User User {get; set;}
}