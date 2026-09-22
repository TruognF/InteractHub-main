using Microsoft.AspNetCore.Identity;

namespace InteractHub.Application.Entities;
public class User : IdentityUser
{
    public string FullName {get; set;}
    public string? ProfilePictureUrl {get; set;}
    public string? Bio {get; set;}
    public DateTime? LastActiveAt { get; set; }

    public ICollection<Post> Posts {get; set;}
    public ICollection<Like> Likes {get; set;}
    public ICollection<Comment> Comments {get; set;}
    public ICollection<Friendship> SentFriendships {get; set;}
    public ICollection<Friendship> ReceivedFriendships {get; set;}
    public ICollection<Story> Stories {get; set;}
    public ICollection<Notification> Notifications {get; set;}
    public ICollection<PostReport> Reports {get; set;}
    public ICollection<GroupMembership> GroupMemberships { get; set; }
    public ICollection<Group> CreatedGroups { get; set; }
    public ICollection<Message> SentMessages { get; set; }
    public ICollection<Message> ReceivedMessages { get; set; }
}