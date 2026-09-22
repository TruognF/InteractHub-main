namespace InteractHub.API.DTOs;

public class CreateCommentDto
{
    public string Content {get; set;} = string.Empty;
    public int PostId {get; set;}
}

public class UpdateCommentDto
{
    public string Content {get; set;} = string.Empty;
}

public class CommentResponseDto
{
    public int Id {get; set;}
    public string Content {get; set;} = string.Empty;
    public int PostId {get; set;}
    public string UserId {get; set;} = string.Empty;
    public DateTime CreatedAt {get; set;}
}