namespace InteractHub.API.DTOs;

public class CreateUserDto
{
    public string UserName {get; set;} = string.Empty;
    public string Email {get; set;} = string.Empty;
    public string Password {get; set;} = string.Empty;
    public string FullName {get; set;} = string.Empty;
}

public class UpdateUserDto
{
    public string FullName {get; set;} = string.Empty;
    public string? ProfilePictureUrl {get; set;}
    public string? Bio {get; set;}
}

public class UserResponseDto
{
    public string Id {get; set;} = string.Empty;
    public string UserName {get; set;} = string.Empty;
    public string Email {get; set;} = string.Empty;
    public string FullName {get; set;} = string.Empty;
    public string? ProfilePictureUrl {get; set;}
    public string? Bio {get; set;}
}