using InteractHub.Application.Entities.Enums;

namespace InteractHub.Application.Entities;
public class Notification
{
    public int Id {get; set;}
    public string Content {get; set;}
    public bool IsRead {get; set;} = false;

    public string UserId {get; set;}
    public User User {get; set;}
    
    // Người dùng tạo ra notification (nếu có)
    public string? RelatedUserId {get; set;}
    public User? RelatedUser {get; set;}
    
    public NotificationType Type {get; set;} = NotificationType.System;
    
    // Để link tới entity liên quan (Post, Comment, etc.)
    public int? RelatedEntityId {get; set;}
    
    public DateTime CreatedAt {get; set;} = DateTime.Now;
}