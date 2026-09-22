using InteractHub.API.Controllers;
using InteractHub.API.DTOs;
using InteractHub.Application.DTOs;
using InteractHub.Application.Entities;
using InteractHub.Application.Interfaces;
using InteractHub.Infrastructure.Hubs;
using InteractHub.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace InteractHub.Tests.Unit.Controllers;

public class PostsControllerTests
{
    private PostsController CreateController(
        Mock<IPostService> postServiceMock,
        Mock<IFriendshipService>? friendshipServiceMock = null,
        Mock<INotificationService>? notificationServiceMock = null,
        Mock<IHubContext<PostHub>>? postHubMock = null,
        Mock<IImageStorageService>? imageStorageMock = null)
    {
        var friendshipMock = friendshipServiceMock ?? new Mock<IFriendshipService>();
        var notificationMock = notificationServiceMock ?? new Mock<INotificationService>();
        var postHubContextMock = postHubMock ?? SignalRMockFactory.CreatePostHubMock();
        var imageStorageContextMock = imageStorageMock ?? new Mock<IImageStorageService>();

        return new PostsController(
            postServiceMock.Object,
            friendshipMock.Object,
            notificationMock.Object,
            postHubContextMock.Object,
            imageStorageContextMock.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOkResult_WithPaginatedPosts()
    {
        // Arrange
        var postDtos = new List<PostResponseDto>
        {
            new PostResponseDto { Id = 1, UserId = "u1", Content = "post 1", CreatedAt = DateTime.UtcNow },
            new PostResponseDto { Id = 2, UserId = "u1", Content = "post 2", CreatedAt = DateTime.UtcNow }
        };

        var postServiceMock = new Mock<IPostService>();
        postServiceMock.Setup(s => s.GetFeedAsync(1, 20)).ReturnsAsync((postDtos, 2));

        var controller = CreateController(postServiceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // Act
        var result = await controller.GetAll(page: 1, pageSize: 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        postServiceMock.Verify(s => s.GetFeedAsync(1, 20), Times.Once);
    }

    [Fact]
    public async Task GetAll_ShouldReturnEmpty_WhenNoPosts()
    {
        // Arrange
        var postServiceMock = new Mock<IPostService>();
        postServiceMock.Setup(s => s.GetFeedAsync(1, 20)).ReturnsAsync((new List<PostResponseDto>(), 0));

        var controller = CreateController(postServiceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // Act
        var result = await controller.GetAll(page: 1, pageSize: 20);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }

    [Fact]
    public async Task GetAll_ShouldReturnUnauthorized_WhenUserNotAuthenticated()
    {
        // Arrange
        var postServiceMock = new Mock<IPostService>();
        var controller = CreateController(postServiceMock);
        ControllerTestHelper.SetAnonymous(controller);

        // Act
        var result = await controller.GetAll(page: 1, pageSize: 20);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(401, objectResult.StatusCode);
    }

    [Fact]
    public async Task GetById_ShouldReturnPost_WhenPostExists()
    {
        // Arrange
        var postDto = new PostResponseDto
        {
            Id = 1,
            UserId = "u1",
            Content = "test post",
            CreatedAt = DateTime.UtcNow
        };

        var postServiceMock = new Mock<IPostService>();
        postServiceMock.Setup(s => s.GetByIdDtoAsync(1)).ReturnsAsync(postDto);

        var controller = CreateController(postServiceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // Act
        var result = await controller.GetById(1);

        // Assert
        var okResult = Assert.IsType<ObjectResult>(result);
        Assert.NotNull(okResult.Value);
        postServiceMock.Verify(s => s.GetByIdDtoAsync(1), Times.Once);
    }

    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenPostDoesNotExist()
    {
        // Arrange
        var postServiceMock = new Mock<IPostService>();
        postServiceMock.Setup(s => s.GetByIdDtoAsync(999)).ReturnsAsync((PostResponseDto?)null);

        var controller = CreateController(postServiceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // Act
        var result = await controller.GetById(999);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    [Fact]
    public async Task Create_ShouldReturnCreated_WhenValidPostSubmitted()
    {
        // Arrange
        var createDto = new CreatePostDto { Content = "new post", ImageUrl = null };
        var createdPost = new Post 
        { 
            Id = 1, 
            UserId = "u1", 
            Content = "new post",
            CreatedAt = DateTime.UtcNow
        };

        var postServiceMock = new Mock<IPostService>();
        postServiceMock.Setup(s => s.CreateAsync(It.IsAny<Post>())).ReturnsAsync(createdPost);
        postServiceMock.Setup(s => s.GetByIdDtoAsync(1)).ReturnsAsync(new PostResponseDto
        {
            Id = 1,
            UserId = "u1",
            Content = "new post",
            CreatedAt = DateTime.UtcNow
        });

        var controller = CreateController(postServiceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // Act
        var result = await controller.Create(createDto);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        postServiceMock.Verify(s => s.CreateAsync(It.IsAny<Post>()), Times.Once);
    }

    [Fact]
    public async Task Create_ShouldReturnBadRequest_WhenEmptyContent()
    {
        // Arrange
        var createDto = new CreatePostDto { Content = "", ImageUrl = null };
        var postServiceMock = new Mock<IPostService>();
        var controller = CreateController(postServiceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // Act
        var result = await controller.Create(createDto);

        // Assert
        var badRequestResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, badRequestResult.StatusCode);
    }
}
