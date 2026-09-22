namespace InteractHub.API.DTOs;

public class CreateStoryDto
{
    public string? ImageUrl {get; set;}
    public string? Content {get; set;}
    public DateTime ExpireAt {get; set;}
}

public class StoryResponseDto
{
    public int Id {get; set;}
    public string? ImageUrl {get; set;}
    public string? Content {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime ExpireAt {get; set;}
    public string UserId {get; set;} = string.Empty;
    public string? UserName {get; set;}
    public string? UserProfilePictureUrl {get; set;}
}