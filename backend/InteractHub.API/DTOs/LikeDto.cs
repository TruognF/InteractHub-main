namespace InteractHub.API.DTOs;

public class CreateLikeDto
{
    public int PostId {get; set;}
}

public class LikeResponseDto
{
    public int Id {get; set;}
    public int PostId {get; set;}
    public string UserId {get; set;} = string.Empty;
}