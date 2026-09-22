using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using InteractHub.Application.Interfaces;
using InteractHub.Application.DTOs;
using InteractHub.Application.Entities;
using InteractHub.API.DTOs;
using InteractHub.API.DTOs.Response;
using InteractHub.API.Extensions;
using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PostsController : ControllerBase
{
    private readonly IPostService _postService;
    private readonly IFriendshipService _friendshipService;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<PostHub> _postHub;
    private readonly IImageStorageService _imageStorage;

    public PostsController(
        IPostService postService,
        IFriendshipService friendshipService,
        INotificationService notificationService,
        IHubContext<PostHub> postHub,
        IImageStorageService imageStorage)
    {
        _postService = postService;
        _friendshipService = friendshipService;
        _notificationService = notificationService;
        _postHub = postHub;
        _imageStorage = imageStorage;
    }

    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(ApiResponse<List<PostResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByUserId(string userId)
    {
        var (posts, _) = await _postService.GetUserPostsAsync(userId, 1, 100);

        return this.SuccessResponse(posts, "User posts retrieved successfully", 200);
    }

    [HttpGet("group/{groupId}")]
    [ProducesResponseType(typeof(ApiResponse<List<PostResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByGroupId(int groupId)
    {
        var (posts, _) = await _postService.GetGroupPostsAsync(groupId, 1, 100);

        return this.SuccessResponse(posts, "Group posts retrieved successfully", 200);
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<PostResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        // Ensure valid pagination
        page = Math.Max(1, page);
        pageSize = Math.Max(1, Math.Min(pageSize, 100)); // Cap pageSize at 100

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return this.UnauthorizedResponse("User not authenticated");

        var (postDtos, totalCount) = await _postService.GetFeedAsync(page, pageSize);

        var pagination = new
        {
            page,
            pageSize,
            totalCount,
            totalPages = (totalCount + pageSize - 1) / pageSize,
            hasMore = page * pageSize < totalCount
        };

        return Ok(new { success = true, message = "Posts retrieved successfully", data = postDtos, pagination });
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PostResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(int id)
    {
        var postDto = await _postService.GetByIdDtoAsync(id);
        if (postDto == null)
            return this.NotFoundResponse("Post not found");

        return this.SuccessResponse(postDto, "Post retrieved successfully", 200);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PostResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreatePostDto createPostDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return this.UnauthorizedResponse("User not authenticated");

        if (string.IsNullOrWhiteSpace(createPostDto.Content))
            return this.BadRequestResponse(new List<ApiError> { new ApiError("Content is required and cannot be empty", code: "EMPTY_CONTENT") });

        var post = new Post
        {
            Content = createPostDto.Content,
            ImageUrl = await _imageStorage.SaveDataUriAsync(createPostDto.ImageUrl, "posts"),
            UserId = userId,
            GroupId = createPostDto.GroupId,
            SharedPostId = createPostDto.SharedPostId
        };

        var created = await _postService.CreateAsync(post);

        // Reload post with User data
        var createdWithUser = await _postService.GetByIdDtoAsync(created.Id);

        if (createdWithUser == null)
            return this.ErrorResponse("Failed to retrieve created post", statusCode: 500);

        if (createdWithUser.GroupId.HasValue)
        {
            await _postHub.Clients.Group($"group_{createdWithUser.GroupId.Value}")
                .SendAsync("GroupPostCreated", createdWithUser);
        }
        else
        {
            await _postHub.Clients.Group("feed")
                .SendAsync("PostCreated", createdWithUser);
        }

        if (createdWithUser.GroupId == null && createdWithUser.SharedPostId == null)
        {
            await _notificationService.NotifyFriendsAboutNewPostAsync(userId, createdWithUser.Id);
        }

        return this.CreatedResponse(createdWithUser, "Post created successfully");
    }

    [HttpPost("{postId}/share")]
    [ProducesResponseType(typeof(ApiResponse<PostResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SharePost(int postId, [FromBody] CreatePostDto? shareDto = null)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return this.UnauthorizedResponse("User not authenticated");

        // Check if original post exists
        var originalPost = await _postService.GetByIdAsync(postId);
        if (originalPost == null)
            return this.NotFoundResponse("Original post not found");

        // Create a new post that references the original
        var sharedPost = new Post
        {
            Content = shareDto?.Content ?? string.Empty, // Optional caption
            ImageUrl = null, // Shared posts don't have their own image
            UserId = userId,
            GroupId = shareDto?.GroupId,
            SharedPostId = postId // Reference to original post
        };

        var created = await _postService.CreateAsync(sharedPost);

        // Reload post with related data
        var createdWithData = await _postService.GetByIdDtoAsync(created.Id);

        if (createdWithData == null)
            return this.ErrorResponse("Failed to retrieve created shared post", statusCode: 500);

        if (createdWithData.GroupId.HasValue)
        {
            await _postHub.Clients.Group($"group_{createdWithData.GroupId.Value}")
                .SendAsync("GroupPostCreated", createdWithData);
        }
        else
        {
            await _postHub.Clients.Group("feed")
                .SendAsync("PostCreated", createdWithData);
        }

        await _notificationService.NotifyOriginalAuthorPostSharedAsync(originalPost.UserId, userId, createdWithData.Id);

        return this.CreatedResponse(createdWithData, "Post shared successfully");
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Delete(int id)
    {
        var post = await _postService.GetByIdAsync(id);
        if (post == null)
            return this.NotFoundResponse("Post not found");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (post.UserId != userId)
            return this.ForbiddenResponse("You cannot delete this post");

        var result = await _postService.DeleteAsync(id);
        if (!result)
            return this.NotFoundResponse("Post not found");

        await _imageStorage.DeleteIfStoredAsync(post.ImageUrl);

        if (post.GroupId.HasValue)
        {
            await _postHub.Clients.Group($"group_{post.GroupId.Value}")
                .SendAsync("GroupPostDeleted", new
                {
                    postId = id,
                    groupId = post.GroupId.Value
                });
        }

        return this.SuccessResponse(message: "Post deleted successfully", statusCode: 200);
    }
}