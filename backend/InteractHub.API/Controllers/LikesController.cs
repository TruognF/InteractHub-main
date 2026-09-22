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

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LikesController : ControllerBase
{
    private readonly ILikeService _likeService;
    private readonly IPostService _postService;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<PostHub> _postHub;

    public LikesController(ILikeService likeService, IPostService postService, INotificationService notificationService, IHubContext<PostHub> postHub)
    {
        _likeService = likeService;
        _postService = postService;
        _notificationService = notificationService;
        _postHub = postHub;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<LikeResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(int id)
    {
        var like = await _likeService.GetByIdAsync(id);
        if (like == null)
            return this.NotFoundResponse("Like not found");

        var likeDto = new LikeResponseDto
        {
            Id = like.Id,
            PostId = like.PostId,
            UserId = like.UserId
        };

        return this.SuccessResponse(likeDto, "Like retrieved successfully", 200);
    }

    [HttpGet("post/{postId}")]
    [ProducesResponseType(typeof(ApiResponse<List<LikeResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByPostId(int postId)
    {
        var likes = await _likeService.GetByPostIdAsync(postId);
        var likeDtos = likes.Select(l => new LikeResponseDto
        {
            Id = l.Id,
            PostId = l.PostId,
            UserId = l.UserId
        }).ToList();

        return this.SuccessResponse(likeDtos, "Likes retrieved successfully", 200);
    }

    [HttpGet("post/{postId}/count")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetLikeCount(int postId)
    {
        var count = await _likeService.GetLikeCountAsync(postId);
        return this.SuccessResponse(new { count }, "Like count retrieved successfully", 200);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<LikeResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateLikeDto createLikeDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return this.UnauthorizedResponse("User not authenticated");

        var like = new Like
        {
            PostId = createLikeDto.PostId,
            UserId = userId
        };

        var created = await _likeService.CreateAsync(like);

        var postInfo = await _postService.GetLiteAsync(created.PostId);
        if (postInfo != null && postInfo.UserId != created.UserId)
        {
            await _notificationService.NotifyLikeAsync(postInfo.UserId, created.UserId, created.PostId);
        }

        var likeDto = new LikeResponseDto
        {
            Id = created.Id,
            PostId = created.PostId,
            UserId = created.UserId
        };

        var likesCount = await _likeService.GetLikeCountAsync(created.PostId);

        if (postInfo?.GroupId != null)
        {
            await _postHub.Clients.Group($"group_{postInfo.GroupId.Value}")
                .SendAsync("GroupPostLiked", new
                {
                    postId = created.PostId,
                    groupId = postInfo.GroupId.Value,
                    likesCount,
                    userId = created.UserId
                });
        }

        await _postHub.Clients.Group("feed")
            .SendAsync("PostLiked", new {
                postId = created.PostId,
                likesCount,
                userId = created.UserId
            });

        return this.CreatedResponse(likeDto, "Like created successfully");
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var like = await _likeService.GetByIdAsync(id);
        if (like == null)
            return this.NotFoundResponse("Like not found");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (like.UserId != userId)
            return this.ForbiddenResponse("You cannot delete this like");

        var result = await _likeService.DeleteAsync(id);
        if (!result)
            return this.NotFoundResponse("Like not found");

        var postInfo = await _postService.GetLiteAsync(like.PostId);
        var likesCount = await _likeService.GetLikeCountAsync(like.PostId);

        if (postInfo?.GroupId != null)
        {
            await _postHub.Clients.Group($"group_{postInfo.GroupId.Value}")
                .SendAsync("GroupPostUnliked", new
                {
                    postId = like.PostId,
                    groupId = postInfo.GroupId.Value,
                    likesCount,
                    userId = like.UserId
                });
        }

        await _postHub.Clients.Group("feed")
            .SendAsync("PostUnliked", new {
                postId = like.PostId,
                likesCount,
                userId = like.UserId
            });

        return this.SuccessResponse(message: "Like deleted successfully", statusCode: 200);
    }

    [HttpDelete("post/{postId}/user/{userId}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteByPostAndUser(int postId, string userId)
    {
        var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (currentUserId != userId)
            return this.ForbiddenResponse("You cannot delete likes for other users");

        var likeToDelete = await _likeService.GetByPostAndUserIdAsync(postId, userId);

        if (likeToDelete == null)
            return this.NotFoundResponse("Like not found");

        var result = await _likeService.DeleteAsync(likeToDelete.Id);
        if (!result)
            return this.NotFoundResponse("Failed to delete like");

        var postInfo = await _postService.GetLiteAsync(postId);
        var likesCount = await _likeService.GetLikeCountAsync(postId);

        if (postInfo?.GroupId != null)
        {
            await _postHub.Clients.Group($"group_{postInfo.GroupId.Value}")
                .SendAsync("GroupPostUnliked", new
                {
                    postId = postId,
                    groupId = postInfo.GroupId.Value,
                    likesCount,
                    userId = userId
                });
        }

        await _postHub.Clients.Group("feed")
            .SendAsync("PostUnliked", new {
                postId = postId,
                likesCount,
                userId = userId
            });

        return this.SuccessResponse(message: "Like deleted successfully", statusCode: 200);
    }
}