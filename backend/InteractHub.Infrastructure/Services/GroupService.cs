using Microsoft.EntityFrameworkCore;
using InteractHub.Application.Interfaces;
using InteractHub.Application.Entities;
using InteractHub.Application.Entities.Enums;
using InteractHub.Infrastructure.Data;
using Regex = System.Text.RegularExpressions.Regex;

namespace InteractHub.Infrastructure.Service;

public class GroupService : IGroupService
{
    private readonly AppDbContext _context;
    private readonly IFriendshipService _friendshipService;

    public GroupService(AppDbContext context, IFriendshipService friendshipService)
    {
        _context = context;
        _friendshipService = friendshipService;
    }

    public async Task<Group> CreateAsync(Group group, string creatorId)
    {
        group.CreatorId = creatorId;
        group.Slug = await GenerateUniqueSlugAsync(group.Name);
        group.CreatedAt = DateTime.UtcNow;

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        var membership = new GroupMembership
        {
            GroupId = group.Id,
            UserId = creatorId,
            JoinedAt = DateTime.UtcNow
        };

        _context.GroupMemberships.Add(membership);
        await _context.SaveChangesAsync();

        return group;
    }

    public async Task<Group> CreateWithMembersAsync(Group group, string creatorId, List<string> memberIds)
    {
        memberIds ??= new List<string>();

        // Check if all members are friends with creator
        foreach (var memberId in memberIds)
        {
            if (memberId == creatorId) continue;
            var status = await _friendshipService.CheckFriendshipStatusAsync(creatorId, memberId);
            if (status != FriendshipStatus.Accepted)
            {
                throw new InvalidOperationException($"User {memberId} is not a friend of the creator");
            }
        }

        group.CreatorId = creatorId;
        group.Slug = await GenerateUniqueSlugAsync(group.Name);
        group.CreatedAt = DateTime.UtcNow;

        _context.Groups.Add(group);
        await _context.SaveChangesAsync();

        // Add creator
        var memberships = new List<GroupMembership>
        {
            new GroupMembership
            {
                GroupId = group.Id,
                UserId = creatorId,
                JoinedAt = DateTime.UtcNow
            }
        };

        // Add other members
        foreach (var memberId in memberIds)
        {
            if (memberId != creatorId)
            {
                memberships.Add(new GroupMembership
                {
                    GroupId = group.Id,
                    UserId = memberId,
                    JoinedAt = DateTime.UtcNow
                });
            }
        }

        _context.GroupMemberships.AddRange(memberships);
        await _context.SaveChangesAsync();

        return group;
    }

    public async Task<bool> IsUserFriendOfCreatorAsync(string userId, string creatorId)
    {
        var status = await _friendshipService.CheckFriendshipStatusAsync(creatorId, userId);
        return status == FriendshipStatus.Accepted;
    }

    public async Task<List<Group>> GetAllWithUserMembershipAsync(string userId)
    {
        return await _context.Groups
            .Include(g => g.Memberships)
            .ToListAsync();
    }

    public async Task<Group?> GetBySlugAsync(string slug)
    {
        return await _context.Groups
            .Include(g => g.Memberships)
            .FirstOrDefaultAsync(g => g.Slug == slug);
    }

    public async Task<Group?> GetByIdAsync(int groupId)
    {
        return await _context.Groups
            .Include(g => g.Memberships)
            .FirstOrDefaultAsync(g => g.Id == groupId);
    }

    public async Task<bool> JoinAsync(int groupId, string userId)
    {
        var group = await _context.Groups.FindAsync(groupId);
        if (group == null)
            return false;

        var existingMembership = await _context.GroupMemberships
            .FindAsync(groupId, userId);

        if (existingMembership != null)
            return true;

        _context.GroupMemberships.Add(new GroupMembership
        {
            GroupId = groupId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> LeaveAsync(int groupId, string userId)
    {
        var membership = await _context.GroupMemberships.FindAsync(groupId, userId);
        if (membership == null)
            return false;

        _context.GroupMemberships.Remove(membership);
        await _context.SaveChangesAsync();
        return true;
    }

    private async Task<string> GenerateUniqueSlugAsync(string name)
    {
        var baseSlug = GenerateSlug(name);
        var slug = baseSlug;
        var index = 1;

        while (await _context.Groups.AnyAsync(g => g.Slug == slug))
        {
            slug = $"{baseSlug}-{index++}";
        }

        return slug;
    }

    private static string GenerateSlug(string name)
    {
        var normalized = name.Trim().ToLowerInvariant();
        normalized = Regex.Replace(normalized, @"\p{Mn}", string.Empty);
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s-]", string.Empty);
        normalized = Regex.Replace(normalized, @"[\s-]+", " ").Trim();
        return Regex.Replace(normalized, @"\s", "-");
    }
}
