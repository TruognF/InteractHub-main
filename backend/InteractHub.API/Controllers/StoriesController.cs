using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using InteractHub.Application.Interfaces;
using InteractHub.Application.Entities;
using InteractHub.API.DTOs;
using InteractHub.API.DTOs.Response;
using InteractHub.API.Extensions;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using InteractHub.Infrastructure.Hubs;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StoriesController : ControllerBase
{
    private readonly IStoryService _storyService;
    private readonly IFriendshipService _friendshipService;
    private readonly IHubContext<StoryHub> _storyHub;
    private readonly IImageStorageService _imageStorage;

    public StoriesController(IStoryService storyService, IFriendshipService friendshipService, IHubContext<StoryHub> storyHub, IImageStorageService imageStorage)
    {
        _storyService = storyService;
        _friendshipService = friendshipService;
        _storyHub = storyHub;
        _imageStorage = imageStorage;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<StoryResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return this.UnauthorizedResponse("User not authenticated");

        var friends = await _friendshipService.GetAcceptedFriendsAsync(userId);
        var friendIds = friends
            .Select(f => f.UserId == userId ? f.Friend?.Id : f.User?.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .ToList();

        var allStories = await _storyService.GetAllAsync();
        var stories = allStories.Where(s => friendIds.Contains(s.UserId) || s.UserId == userId).ToList();

        var storyDtos = stories.Select(s => new StoryResponseDto
        {
            Id = s.Id,
            ImageUrl = s.ImageUrl,
            Content = s.Content,
            CreatedAt = s.CreatedAt,
            ExpireAt = s.ExpireAt,
            UserId = s.UserId,
            UserName = s.User?.FullName ?? s.User?.UserName,
            UserProfilePictureUrl = s.User?.ProfilePictureUrl
        }).ToList();

        return this.SuccessResponse(storyDtos);
    }

    /// <summary>Nhóm realtime story: chủ tin + bạn bè đã kết nối (cùng logic feed).</summary>
    private async Task<List<string>> GetStoryRealtimeRecipientGroupsAsync(string storyAuthorUserId)
    {
        var friends = await _friendshipService.GetAcceptedFriendsAsync(storyAuthorUserId);
        var ids = friends
            .Select(f => storyAuthorUserId == f.UserId ? f.Friend?.Id : f.User?.Id)
            .Where(id => !string.IsNullOrEmpty(id))
            .Select(id => id!)
            .Distinct()
            .ToList();
        ids.Add(storyAuthorUserId);
        return ids
            .Distinct()
            .Select(id => $"story-user-{id}")
            .ToList();
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<StoryResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(int id)
    {
        var story = await _storyService.GetByIdAsync(id);
        if (story == null)
            return this.NotFoundResponse("Story not found");

        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
            return this.UnauthorizedResponse("User not authenticated");

        // Allow if viewing own story
        if (currentUserId != story.UserId)
        {
            var status = await _friendshipService.CheckFriendshipStatusAsync(currentUserId, story.UserId);
            if (status != InteractHub.Application.Entities.Enums.FriendshipStatus.Accepted)
                return this.ForbiddenResponse("You can only view stories from friends");
        }

        var storyDto = new StoryResponseDto
        {
            Id = story.Id,
            ImageUrl = story.ImageUrl,
            Content = story.Content,
            CreatedAt = story.CreatedAt,
            ExpireAt = story.ExpireAt,
            UserId = story.UserId,
            UserName = story.User?.FullName ?? story.User?.UserName,
            UserProfilePictureUrl = story.User?.ProfilePictureUrl
        };

        return this.SuccessResponse(storyDto);
    }

    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<List<StoryResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByUserId(string userId)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(currentUserId))
            return this.UnauthorizedResponse("User not authenticated");

        // Allow if viewing own stories
        if (currentUserId != userId)
        {
            var status = await _friendshipService.CheckFriendshipStatusAsync(currentUserId, userId);
            if (status != InteractHub.Application.Entities.Enums.FriendshipStatus.Accepted)
                return this.ForbiddenResponse("You can only view stories from friends");
        }

        var stories = await _storyService.GetByUserIdAsync(userId);
        var storyDtos = stories.Select(s => new StoryResponseDto
        {
            Id = s.Id,
            ImageUrl = s.ImageUrl,
            Content = s.Content,
            CreatedAt = s.CreatedAt,
            ExpireAt = s.ExpireAt,
            UserId = s.UserId,
            UserName = s.User?.FullName ?? s.User?.UserName,
            UserProfilePictureUrl = s.User?.ProfilePictureUrl
        }).ToList();

        return this.SuccessResponse(storyDtos);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<StoryResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateStoryDto createStoryDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return this.UnauthorizedResponse("User not authenticated");

        var story = new Story
        {
            ImageUrl = await _imageStorage.SaveDataUriAsync(createStoryDto.ImageUrl, "stories"),
            Content = createStoryDto.Content,
            CreatedAt = DateTime.UtcNow,
            ExpireAt = createStoryDto.ExpireAt,
            UserId = userId
        };

        var created = await _storyService.CreateAsync(story);
        var createdWithUser = await _storyService.GetByIdAsync(created.Id);

        var storyDto = new StoryResponseDto
        {
            Id = createdWithUser!.Id,
            ImageUrl = createdWithUser.ImageUrl,
            Content = createdWithUser.Content,
            CreatedAt = createdWithUser.CreatedAt,
            ExpireAt = createdWithUser.ExpireAt,
            UserId = createdWithUser.UserId,
            UserName = createdWithUser.User?.FullName ?? createdWithUser.User?.UserName,
            UserProfilePictureUrl = createdWithUser.User?.ProfilePictureUrl
        };

        try
        {
            var recipientGroups = await GetStoryRealtimeRecipientGroupsAsync(userId);
            await _storyHub.Clients.Groups(recipientGroups)
                .SendAsync("StoryCreated", new
                {
                    storyDto.Id,
                    storyDto.ImageUrl,
                    storyDto.Content,
                    storyDto.CreatedAt,
                    storyDto.ExpireAt,
                    storyDto.UserId,
                    storyDto.UserName,
                    storyDto.UserProfilePictureUrl
                });
        }
        catch
        {
            // Ignore realtime failures
        }

        return this.CreatedResponse(storyDto);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var story = await _storyService.GetByIdAsync(id);
        if (story == null)
            return this.NotFoundResponse("Story not found");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (story.UserId != userId)
            return this.ForbiddenResponse();

        var result = await _storyService.DeleteAsync(id);
        if (!result)
            return this.NotFoundResponse("Story not found");

        await _imageStorage.DeleteIfStoredAsync(story.ImageUrl);

        try
        {
            var recipientGroups = await GetStoryRealtimeRecipientGroupsAsync(story.UserId);
            await _storyHub.Clients.Groups(recipientGroups)
                .SendAsync("StoryDeleted", new { storyId = id, userId = story.UserId });
        }
        catch
        {
            // Ignore realtime failures
        }

        return this.SuccessResponse(statusCode: 204);
    }
}
