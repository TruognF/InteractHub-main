using InteractHub.Application.Entities;

namespace InteractHub.Application.Interfaces;

public interface IGroupService
{
    Task<Group> CreateAsync(Group group, string creatorId);
    Task<Group> CreateWithMembersAsync(Group group, string creatorId, List<string> memberIds);
    Task<List<Group>> GetAllWithUserMembershipAsync(string userId);
    Task<Group?> GetBySlugAsync(string slug);
    Task<Group?> GetByIdAsync(int groupId);
    Task<bool> JoinAsync(int groupId, string userId);
    Task<bool> LeaveAsync(int groupId, string userId);
    Task<bool> IsUserFriendOfCreatorAsync(string userId, string creatorId);
}
