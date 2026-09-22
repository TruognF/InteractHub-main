using InteractHub.API.Controllers;
using InteractHub.API.DTOs;
using InteractHub.Application.Entities;
using InteractHub.Application.Interfaces;
using InteractHub.Infrastructure.Hubs;
using InteractHub.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace InteractHub.Tests.Unit.Controllers;

public class CommentsControllerTests
{
    private CommentsController CreateController(
        Mock<ICommentService> commentMock,
        Mock<IPostService> postMock,
        Mock<INotificationService> notificationMock,
        Mock<IHubContext<CommentHub>> commentHubMock,
        Mock<IHubContext<PostHub>> postHubMock)
    {
        return new CommentsController(
            commentMock.Object,
            postMock.Object,
            notificationMock.Object,
            commentHubMock.Object,
            postHubMock.Object);
    }

    // GetAll tests - trả về tất cả comments
    [Fact]
    public async Task GetAll_ShouldReturnAllComments_WhenCommentsExist()
    {
        // Arrange
        var comments = new List<Comment>
        {
            new Comment { Id = 1, UserId = "u1", PostId = 1, Content = "comment 1", CreatedAt = DateTime.UtcNow },
            new Comment { Id = 2, UserId = "u2", PostId = 1, Content = "comment 2", CreatedAt = DateTime.UtcNow }
        };
        var commentMock = new Mock<ICommentService>();
        commentMock.Setup(s => s.GetAllAsync()).ReturnsAsync(comments);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var commentHubMock = new Mock<IHubContext<CommentHub>>();
        var postHubMock = new Mock<IHubContext<PostHub>>();
        
        var controller = CreateController(commentMock, postMock, notificationMock, commentHubMock, postHubMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // Act
        var result = await controller.GetAll();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        commentMock.Verify(s => s.GetAllAsync(), Times.Once);
    }

    // GetAll tests - trả về danh sách rỗng khi không có comments
    [Fact]
    public async Task GetAll_ShouldReturnEmpty_WhenNoCommentsExist()
    {
        // Arrange
        var comments = new List<Comment>();
        var commentMock = new Mock<ICommentService>();
        commentMock.Setup(s => s.GetAllAsync()).ReturnsAsync(comments);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var commentHubMock = new Mock<IHubContext<CommentHub>>();
        var postHubMock = new Mock<IHubContext<PostHub>>();
        
        var controller = CreateController(commentMock, postMock, notificationMock, commentHubMock, postHubMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // Act
        var result = await controller.GetAll();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    // GetById tests - trả về comment khi tồn tại
    [Fact]
    public async Task GetById_ShouldReturnComment_WhenCommentExists()
    {
        // given
        var comment = new Comment { Id = 1, UserId = "u1", PostId = 1, Content = "test comment", CreatedAt = DateTime.UtcNow };
        var commentMock = new Mock<ICommentService>();
        commentMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(comment);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var commentHubMock = new Mock<IHubContext<CommentHub>>();
        var postHubMock = new Mock<IHubContext<PostHub>>();
        var controller = CreateController(commentMock, postMock, notificationMock, commentHubMock, postHubMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetById(1);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        commentMock.Verify(s => s.GetByIdAsync(1), Times.Once);
    }

    // GetById tests - trả về 404 khi comment không tồn tại
    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenCommentMissing()
    {
        // given
        var commentMock = new Mock<ICommentService>();
        commentMock.Setup(s => s.GetByIdAsync(44)).ReturnsAsync((Comment?)null);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var commentHubMock = new Mock<IHubContext<CommentHub>>();
        var postHubMock = new Mock<IHubContext<PostHub>>();
        var controller = CreateController(commentMock, postMock, notificationMock, commentHubMock, postHubMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetById(44);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    // GetByPostId tests - trả về comments của một bài viết
    [Fact]
    public async Task GetByPostId_ShouldReturnPostComments_WhenCommentsExist()
    {
        // given
        var postId = 5;
        var comments = new List<Comment>
        {
            new Comment { Id = 1, UserId = "u1", PostId = postId, Content = "comment 1", CreatedAt = DateTime.UtcNow },
            new Comment { Id = 2, UserId = "u2", PostId = postId, Content = "comment 2", CreatedAt = DateTime.UtcNow }
        };
        var commentMock = new Mock<ICommentService>();
        commentMock.Setup(s => s.GetByPostIdAsync(postId)).ReturnsAsync(comments);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var commentHubMock = new Mock<IHubContext<CommentHub>>();
        var postHubMock = new Mock<IHubContext<PostHub>>();
        var controller = CreateController(commentMock, postMock, notificationMock, commentHubMock, postHubMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetByPostId(postId);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        commentMock.Verify(s => s.GetByPostIdAsync(postId), Times.Once);
    }

    // GetByPostId tests - trả về danh sách rỗng khi không có comments cho bài viết
    [Fact]
    public async Task GetByPostId_ShouldReturnEmpty_WhenNoCommentsForPost()
    {
        // given
        var postId = 5;
        var comments = new List<Comment>();
        var commentMock = new Mock<ICommentService>();
        commentMock.Setup(s => s.GetByPostIdAsync(postId)).ReturnsAsync(comments);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var commentHubMock = new Mock<IHubContext<CommentHub>>();
        var postHubMock = new Mock<IHubContext<PostHub>>();
        var controller = CreateController(commentMock, postMock, notificationMock, commentHubMock, postHubMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetByPostId(postId);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    // Create tests - tạo comment thành công
    [Fact]
    public async Task Create_ShouldReturnCreated_WhenCommentIsValid()
    {
        // given
        var createDto = new CreateCommentDto { PostId = 1, Content = "new comment" };
        var comment = new Comment { Id = 1, UserId = "u1", PostId = 1, Content = createDto.Content, CreatedAt = DateTime.UtcNow };
        var commentMock = new Mock<ICommentService>();
        commentMock.Setup(s => s.CreateAsync(It.IsAny<Comment>())).ReturnsAsync(comment);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var commentHubMock = new Mock<IHubContext<CommentHub>>();
        var postHubMock = new Mock<IHubContext<PostHub>>();
        var controller = CreateController(commentMock, postMock, notificationMock, commentHubMock, postHubMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.Create(createDto);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);
        commentMock.Verify(s => s.CreateAsync(It.IsAny<Comment>()), Times.Once);
    }

    // Create tests - trả về 401 khi không có user claim
    [Fact]
    public async Task Create_ShouldReturnUnauthorized_WhenNoUserClaim()
    {
        // given
        var commentMock = new Mock<ICommentService>();
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var commentHubMock = new Mock<IHubContext<CommentHub>>();
        var postHubMock = new Mock<IHubContext<PostHub>>();
        var controller = CreateController(commentMock, postMock, notificationMock, commentHubMock, postHubMock);
        ControllerTestHelper.SetAnonymous(controller);

        // when
        var result = await controller.Create(new CreateCommentDto { PostId = 1, Content = "comment" });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(401, objectResult.StatusCode);
    }

    // Update tests - cập nhật comment thành công
    [Fact]
    public async Task Update_ShouldReturnOk_WhenCommentOwner()
    {
        // given
        var comment = new Comment { Id = 1, UserId = "u1", PostId = 1, Content = "old", CreatedAt = DateTime.UtcNow };
        var updateDto = new UpdateCommentDto { Content = "updated" };
        var commentMock = new Mock<ICommentService>();
        commentMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(comment);
        commentMock.Setup(s => s.UpdateAsync(It.IsAny<Comment>())).ReturnsAsync(true);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var commentHubMock = new Mock<IHubContext<CommentHub>>();
        var postHubMock = new Mock<IHubContext<PostHub>>();
        var controller = CreateController(commentMock, postMock, notificationMock, commentHubMock, postHubMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.Update(1, updateDto);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        commentMock.Verify(s => s.UpdateAsync(It.IsAny<Comment>()), Times.Once);
    }

    // Update tests - trả về 404 khi comment không tồn tại
    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenCommentMissing()
    {
        // given
        var commentMock = new Mock<ICommentService>();
        commentMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((Comment?)null);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var commentHubMock = new Mock<IHubContext<CommentHub>>();
        var postHubMock = new Mock<IHubContext<PostHub>>();
        var controller = CreateController(commentMock, postMock, notificationMock, commentHubMock, postHubMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.Update(999, new UpdateCommentDto { Content = "updated" });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    // Update tests - trả về 403 khi không phải chủ sở hữu
    [Fact]
    public async Task Update_ShouldReturnForbidden_WhenCurrentUserIsNotCommentOwner()
    {
        // given
        var comment = new Comment { Id = 1, UserId = "owner", PostId = 1, Content = "old" };
        var commentMock = new Mock<ICommentService>();
        commentMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(comment);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var commentHubMock = new Mock<IHubContext<CommentHub>>();
        var postHubMock = new Mock<IHubContext<PostHub>>();
        var controller = CreateController(commentMock, postMock, notificationMock, commentHubMock, postHubMock);
        ControllerTestHelper.SetUser(controller, "other");

        // when
        var result = await controller.Update(1, new UpdateCommentDto { Content = "updated" });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    // Delete tests - xóa comment thành công
    [Fact]
    public async Task Delete_ShouldReturnOk_WhenCommentOwner()
    {
        // given
        var comment = new Comment { Id = 1, UserId = "u1", PostId = 1, Content = "test", CreatedAt = DateTime.UtcNow };
        var commentMock = new Mock<ICommentService>();
        commentMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(comment);
        commentMock.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var commentHubMock = new Mock<IHubContext<CommentHub>>();
        var postHubMock = new Mock<IHubContext<PostHub>>();
        var controller = CreateController(commentMock, postMock, notificationMock, commentHubMock, postHubMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.Delete(1);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        commentMock.Verify(s => s.DeleteAsync(1), Times.Once);
    }

    // Delete tests - trả về 404 khi comment không tồn tại
    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenCommentMissing()
    {
        // given
        var commentMock = new Mock<ICommentService>();
        commentMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((Comment?)null);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var commentHubMock = new Mock<IHubContext<CommentHub>>();
        var postHubMock = new Mock<IHubContext<PostHub>>();
        var controller = CreateController(commentMock, postMock, notificationMock, commentHubMock, postHubMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.Delete(999);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    // Delete tests - trả về 403 khi không phải chủ sở hữu
    [Fact]
    public async Task Delete_ShouldReturnForbidden_WhenCurrentUserIsNotCommentOwner()
    {
        // given
        var comment = new Comment { Id = 1, UserId = "owner", PostId = 1, Content = "test" };
        var commentMock = new Mock<ICommentService>();
        commentMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(comment);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var commentHubMock = new Mock<IHubContext<CommentHub>>();
        var postHubMock = new Mock<IHubContext<PostHub>>();
        var controller = CreateController(commentMock, postMock, notificationMock, commentHubMock, postHubMock);
        ControllerTestHelper.SetUser(controller, "other");

        // when
        var result = await controller.Delete(1);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    // Delete tests - trả về 404 khi xóa thất bại
    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenDeleteFails()
    {
        // given
        var comment = new Comment { Id = 1, UserId = "u1", PostId = 1, Content = "test", CreatedAt = DateTime.UtcNow };
        var commentMock = new Mock<ICommentService>();
        commentMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(comment);
        commentMock.Setup(s => s.DeleteAsync(1)).ReturnsAsync(false);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var commentHubMock = new Mock<IHubContext<CommentHub>>();
        var postHubMock = new Mock<IHubContext<PostHub>>();
        var controller = CreateController(commentMock, postMock, notificationMock, commentHubMock, postHubMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.Delete(1);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }
}

