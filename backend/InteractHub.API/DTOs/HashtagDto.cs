namespace InteractHub.API.DTOs;

public class CreateHashtagDto
{
    public string Name {get; set;} = string.Empty;
}

public class UpdateHashtagDto
{
    public string Name {get; set;} = string.Empty;
}

public class HashtagResponseDto
{
    public int Id {get; set;}
    public string Name {get; set;} = string.Empty;
}