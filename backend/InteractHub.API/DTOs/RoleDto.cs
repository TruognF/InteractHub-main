namespace InteractHub.API.DTOs;

/// <summary>
/// DTO để gán role cho user
/// </summary>
public class AssignRoleDto
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

/// <summary>
/// DTO để xem thông tin user và roles của họ
/// </summary>
public class UserWithRolesDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// DTO để remove role khỏi user
/// </summary>
public class RemoveRoleDto
{
    public string UserId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
