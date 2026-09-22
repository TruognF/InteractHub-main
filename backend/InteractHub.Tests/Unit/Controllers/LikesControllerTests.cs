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

public class LikesControllerTests
{
    private LikesController CreateController(
        Mock<ILikeService> likeMock,
        Mock<IPostService> postMock,
        Mock<INotificationService> notificationMock,
        Mock<IHubContext<PostHub>>? postHubMock = null)
    {
        var postHubContextMock = postHubMock ?? SignalRMockFactory.CreatePostHubMock();
        
        return new LikesController(
            likeMock.Object,
            postMock.Object,
            notificationMock.Object,
            postHubContextMock.Object);
    }

    // GetById tests - trả về like khi tồn tại
    [Fact]
    public async Task GetById_ShouldReturnLike_WhenLikeExists()
    {
        // Arrange
        var like = new Like { Id = 1, PostId = 1, UserId = "u1" };
        var likeMock = new Mock<ILikeService>();
        likeMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(like);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        
        var controller = CreateController(likeMock, postMock, notificationMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // Act
        var result = await controller.GetById(1);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        likeMock.Verify(s => s.GetByIdAsync(1), Times.Once);
    }

    // GetById tests - trả về 404 khi like không tồn tại
    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenLikeMissing()
    {
        // Arrange
        var likeMock = new Mock<ILikeService>();
        likeMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((Like?)null);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        
        var controller = CreateController(likeMock, postMock, notificationMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // Act
        var result = await controller.GetById(999);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    // GetByPostId tests - trả về likes của một bài viết
    [Fact]
    public async Task GetByPostId_ShouldReturnPostLikes_WhenLikesExist()
    {
        // given
        var postId = 5;
        var likes = new List<Like>
        {
            new Like { Id = 1, PostId = postId, UserId = "u1" },
            new Like { Id = 2, PostId = postId, UserId = "u2" }
        };
        var likeMock = new Mock<ILikeService>();
        likeMock.Setup(s => s.GetByPostIdAsync(postId)).ReturnsAsync(likes);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var postHubMock = new Mock<IHubContext<PostHub>>();
        var controller = CreateController(likeMock, postMock, notificationMock, postHubMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetByPostId(postId);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        likeMock.Verify(s => s.GetByPostIdAsync(postId), Times.Once);
    }

    // GetByPostId tests - trả về danh sách rỗng khi không có likes
    [Fact]
    public async Task GetByPostId_ShouldReturnEmpty_WhenNoLikesForPost()
    {
        // given
        var postId = 5;
        var likes = new List<Like>();
        var likeMock = new Mock<ILikeService>();
        likeMock.Setup(s => s.GetByPostIdAsync(postId)).ReturnsAsync(likes);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var controller = CreateController(likeMock, postMock, notificationMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetByPostId(postId);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    // GetLikeCount tests - trả về số like của một bài viết
    [Fact]
    public async Task GetLikeCount_ShouldReturnLikeCount_WhenPostExists()
    {
        // given
        var postId = 5;
        var count = 10;
        var likeMock = new Mock<ILikeService>();
        likeMock.Setup(s => s.GetLikeCountAsync(postId)).ReturnsAsync(count);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var controller = CreateController(likeMock, postMock, notificationMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetLikeCount(postId);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        likeMock.Verify(s => s.GetLikeCountAsync(postId), Times.Once);
    }

    // GetLikeCount tests - trả về 0 khi không có likes
    [Fact]
    public async Task GetLikeCount_ShouldReturnZero_WhenNoLikesForPost()
    {
        // given
        var postId = 5;
        var count = 0;
        var likeMock = new Mock<ILikeService>();
        likeMock.Setup(s => s.GetLikeCountAsync(postId)).ReturnsAsync(count);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var controller = CreateController(likeMock, postMock, notificationMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetLikeCount(postId);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    // Create tests - tạo like thành công
    [Fact]
    public async Task Create_ShouldReturnCreated_WhenLikeIsValid()
    {
        // given
        var createDto = new CreateLikeDto { PostId = 10 };
        var like = new Like { Id = 1, PostId = 10, UserId = "u1" };
        var likeMock = new Mock<ILikeService>();
        likeMock.Setup(s => s.CreateAsync(It.IsAny<Like>())).ReturnsAsync(like);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var controller = CreateController(likeMock, postMock, notificationMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.Create(createDto);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);
        likeMock.Verify(s => s.CreateAsync(It.IsAny<Like>()), Times.Once);
    }

    // Create tests - trả về 401 khi không có user claim
    [Fact]
    public async Task Create_ShouldReturnUnauthorized_WhenNoUserClaim()
    {
        // given
        var likeMock = new Mock<ILikeService>();
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var controller = CreateController(likeMock, postMock, notificationMock);
        ControllerTestHelper.SetAnonymous(controller);

        // when
        var result = await controller.Create(new CreateLikeDto { PostId = 9 });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(401, objectResult.StatusCode);
    }

    // Delete tests - xóa like thành công
    [Fact]
    public async Task Delete_ShouldReturnOk_WhenLikeOwner()
    {
        // given
        var like = new Like { Id = 6, PostId = 2, UserId = "u1" };
        var likeMock = new Mock<ILikeService>();
        likeMock.Setup(s => s.GetByIdAsync(6)).ReturnsAsync(like);
        likeMock.Setup(s => s.DeleteAsync(6)).ReturnsAsync(true);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var controller = CreateController(likeMock, postMock, notificationMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.Delete(6);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        likeMock.Verify(s => s.DeleteAsync(6), Times.Once);
    }

    // Delete tests - trả về 404 khi like không tồn tại
    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenLikeMissing()
    {
        // given
        var likeMock = new Mock<ILikeService>();
        likeMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((Like?)null);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var controller = CreateController(likeMock, postMock, notificationMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.Delete(999);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    // Delete tests - trả về 403 khi không phải chủ sở hữu
    [Fact]
    public async Task Delete_ShouldReturnForbidden_WhenCurrentUserIsNotLikeOwner()
    {
        // given
        var like = new Like { Id = 6, PostId = 2, UserId = "owner" };
        var likeMock = new Mock<ILikeService>();
        likeMock.Setup(s => s.GetByIdAsync(6)).ReturnsAsync(like);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var controller = CreateController(likeMock, postMock, notificationMock);
        ControllerTestHelper.SetUser(controller, "other");

        // when
        var result = await controller.Delete(6);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    // Delete tests - trả về 404 khi xóa thất bại
    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenDeleteFails()
    {
        // given
        var like = new Like { Id = 6, PostId = 2, UserId = "u1" };
        var likeMock = new Mock<ILikeService>();
        likeMock.Setup(s => s.GetByIdAsync(6)).ReturnsAsync(like);
        likeMock.Setup(s => s.DeleteAsync(6)).ReturnsAsync(false);
        var postMock = new Mock<IPostService>();
        var notificationMock = new Mock<INotificationService>();
        var controller = CreateController(likeMock, postMock, notificationMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.Delete(6);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }
}

