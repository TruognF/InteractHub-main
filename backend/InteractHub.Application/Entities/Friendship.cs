using InteractHub.Application.Entities.Enums;

namespace InteractHub.Application.Entities;
public class Friendship
{
    public int Id {get; set;}

    public string UserId {get; set;}
    public User User {get; set;}

    public string FriendId {get; set;}
    public User Friend {get; set;}

    public FriendshipStatus Status {get; set;} = FriendshipStatus.Pending;
    
    public DateTime CreatedAt {get; set;} = DateTime.Now;
    public DateTime? UpdatedAt {get; set;}
}