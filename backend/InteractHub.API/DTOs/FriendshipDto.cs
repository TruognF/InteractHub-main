namespace InteractHub.API.DTOs;

public class CreateFriendshipDto
{
    public string FriendId {get; set;} = string.Empty;
}

public class SendFriendRequestDto
{
    public string FriendId {get; set;} = string.Empty;
}

public class FriendshipResponseDto
{
    public int Id {get; set;}
    public string UserId {get; set;} = string.Empty;
    public string FriendId {get; set;} = string.Empty;
    public string FriendName { get; set; } = string.Empty;
    public string? FriendProfilePictureUrl { get; set; }
    public string Status {get; set;} = "Pending";
    public DateTime CreatedAt {get; set;}
    public DateTime? UpdatedAt {get; set;}
}

/// <summary>
/// DTO for conversation with latest message info
/// </summary>
public class ConversationDto
{
    public string? Id { get; set; } // Conversation identifier (FriendId or GroupId)
    public string? FriendId { get; set; } = string.Empty; // For private chats only
    public string ConversationName { get; set; } = string.Empty; // Friend or group name
    public string? ConversationAvatarUrl { get; set; } // Friend or group avatar
    public string? LastMessage { get; set; }
    public DateTime? LastMessageTime { get; set; }
    public string? LastMessageSenderId { get; set; }
    public bool IsGroup { get; set; } = false; // True if group conversation
    public int ParticipantCount { get; set; } = 2; // Number of participants in conversation
    public bool IsOnline { get; set; } = false;
    public DateTime? LastSeenAt { get; set; }
}