
namespace InteractHub.Application.Entities;
public class Post
{
    public int Id {get; set;}
    public string Content {get; set;}
    public string? ImageUrl {get; set;}
    public DateTime CreatedAt {get; set;} = DateTime.Now;
    public DateTime? UpdatedAt {get; set;}

    public string UserId {get; set;}
    public User User {get; set;}

    public int? GroupId {get; set;}
    public Group? Group {get; set;}

    // Share Post feature
    public int? SharedPostId {get; set;}
    public Post? SharedPost {get; set;}

    public ICollection <Comment> Comments {get; set;}
    public ICollection <Like> Likes {get; set;}
    public ICollection<PostHashtag> PostHashtags {get; set;}
    public ICollection<PostReport> Reports {get; set;}

}