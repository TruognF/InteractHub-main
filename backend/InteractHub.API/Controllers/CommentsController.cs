using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using InteractHub.Application.Interfaces;
using InteractHub.Application.Entities;
using InteractHub.API.DTOs;
using InteractHub.API.DTOs.Response;
using InteractHub.API.Extensions;
using InteractHub.Infrastructure.Hubs;
using System.Security.Claims;

namespace InteractHub.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CommentsController : ControllerBase
{
    private readonly ICommentService _commentService;
    private readonly IPostService _postService;
    private readonly INotificationService _notificationService;
    private readonly IHubContext<CommentHub> _commentHubContext;
    private readonly IHubContext<PostHub> _postHubContext;

    public CommentsController(
        ICommentService commentService,
        IPostService postService,
        INotificationService notificationService,
        IHubContext<CommentHub> commentHubContext,
        IHubContext<PostHub> postHubContext)
    {
        _commentService = commentService;
        _postService = postService;
        _notificationService = notificationService;
        _commentHubContext = commentHubContext;
        _postHubContext = postHubContext;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CommentResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        var comments = await _commentService.GetAllAsync();
        var commentDtos = comments.Select(c => new CommentResponseDto
        {
            Id = c.Id,
            Content = c.Content,
            PostId = c.PostId,
            UserId = c.UserId,
            CreatedAt = c.CreatedAt
        }).ToList();

        return this.SuccessResponse(commentDtos, "Comments retrieved successfully", 200);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CommentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetById(int id)
    {
        var comment = await _commentService.GetByIdAsync(id);
        if (comment == null)
            return this.NotFoundResponse("Comment not found");

        var commentDto = new CommentResponseDto
        {
            Id = comment.Id,
            Content = comment.Content,
            PostId = comment.PostId,
            UserId = comment.UserId,
            CreatedAt = comment.CreatedAt
        };

        return this.SuccessResponse(commentDto, "Comment retrieved successfully", 200);
    }

    [HttpGet("post/{postId}")]
    [ProducesResponseType(typeof(ApiResponse<List<CommentResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetByPostId(int postId)
    {
        var comments = await _commentService.GetByPostIdAsync(postId);
        var commentDtos = comments.Select(c => new CommentResponseDto
        {
            Id = c.Id,
            Content = c.Content,
            PostId = c.PostId,
            UserId = c.UserId,
            CreatedAt = c.CreatedAt
        }).ToList();

        return this.SuccessResponse(commentDtos, "Comments retrieved successfully", 200);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CommentResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create([FromBody] CreateCommentDto createCommentDto)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return this.UnauthorizedResponse("User not authenticated");

        var comment = new Comment
        {
            Content = createCommentDto.Content,
            PostId = createCommentDto.PostId,
            UserId = userId
        };

        var created = await _commentService.CreateAsync(comment);

        var postInfo = await _postService.GetLiteAsync(created.PostId);
        if (postInfo != null && postInfo.UserId != created.UserId)
        {
            await _notificationService.NotifyCommentAsync(postInfo.UserId, created.UserId, created.PostId, created.Content);
        }

        var commentDto = new CommentResponseDto
        {
            Id = created.Id,
            Content = created.Content,
            PostId = created.PostId,
            UserId = created.UserId,
            CreatedAt = created.CreatedAt
        };

        try
        {
            await _commentHubContext.Clients
                .Group(GetPostGroupName(created.PostId))
                .SendAsync("ReceiveCommentCreated", commentDto);
        }
        catch
        {
            // Ignore real-time delivery failures.
        }

        try
        {
            var commentsCount = await _commentService.GetCountByPostIdAsync(created.PostId);
            await _postHubContext.Clients.Group("feed")
                .SendAsync("CommentAdded", new
                {
                    postId = created.PostId,
                    commentsCount,
                    comment = commentDto
                });

            if (postInfo?.GroupId != null)
            {
                await _postHubContext.Clients.Group($"group_{postInfo.GroupId.Value}")
                    .SendAsync("GroupCommentAdded", new
                    {
                        groupId = postInfo.GroupId.Value,
                        postId = created.PostId,
                        commentsCount,
                        comment = commentDto
                    });
            }
        }
        catch
        {
            // Ignore real-time delivery failures.
        }

        return this.CreatedResponse(commentDto, "Comment created successfully");
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCommentDto updateCommentDto)
    {
        var comment = await _commentService.GetByIdAsync(id);
        if (comment == null)
            return this.NotFoundResponse("Comment not found");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (comment.UserId != userId)
            return this.ForbiddenResponse("You cannot update this comment");

        comment.Content = updateCommentDto.Content;

        await _commentService.UpdateAsync(comment);

        var updatedDto = new CommentResponseDto
        {
            Id = comment.Id,
            Content = comment.Content,
            PostId = comment.PostId,
            UserId = comment.UserId,
            CreatedAt = comment.CreatedAt
        };

        try
        {
            await _commentHubContext.Clients
                .Group(GetPostGroupName(comment.PostId))
                .SendAsync("ReceiveCommentUpdated", updatedDto);
        }
        catch
        {
            // Ignore real-time delivery failures.
        }

        try
        {
            var postInfo = await _postService.GetLiteAsync(comment.PostId);
            await _postHubContext.Clients.Group("feed")
                .SendAsync("CommentUpdated", new
                {
                    postId = comment.PostId,
                    commentId = comment.Id,
                    content = comment.Content
                });

            if (postInfo?.GroupId != null)
            {
                await _postHubContext.Clients.Group($"group_{postInfo.GroupId.Value}")
                    .SendAsync("GroupCommentUpdated", new
                    {
                        groupId = postInfo.GroupId.Value,
                        postId = comment.PostId,
                        comment = updatedDto
                    });
            }
        }
        catch
        {
            // Ignore real-time delivery failures.
        }

        return this.SuccessResponse(message: "Comment updated successfully", statusCode: 200);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id)
    {
        var comment = await _commentService.GetByIdAsync(id);
        if (comment == null)
            return this.NotFoundResponse("Comment not found");

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (comment.UserId != userId)
            return this.ForbiddenResponse("You cannot delete this comment");

        var result = await _commentService.DeleteAsync(id);
        if (!result)
            return this.NotFoundResponse("Comment not found");

        try
        {
            await _commentHubContext.Clients
                .Group(GetPostGroupName(comment.PostId))
                .SendAsync("ReceiveCommentDeleted", new { Id = id, PostId = comment.PostId });
        }
        catch
        {
            // Ignore real-time delivery failures.
        }

        try
        {
            var postInfo = await _postService.GetLiteAsync(comment.PostId);
            var commentsCount = await _commentService.GetCountByPostIdAsync(comment.PostId);
            await _postHubContext.Clients.Group("feed")
                .SendAsync("CommentDeleted", new
                {
                    postId = comment.PostId,
                    commentId = id,
                    commentsCount
                });

            if (postInfo?.GroupId != null)
            {
                await _postHubContext.Clients.Group($"group_{postInfo.GroupId.Value}")
                    .SendAsync("GroupCommentDeleted", new
                    {
                        groupId = postInfo.GroupId.Value,
                        postId = comment.PostId,
                        commentId = id,
                        commentsCount
                    });
            }
        }
        catch
        {
            // Ignore real-time delivery failures.
        }

        return this.SuccessResponse(message: "Comment deleted successfully", statusCode: 200);
    }

    private static string GetPostGroupName(int postId) => $"comments-{postId}";
}
