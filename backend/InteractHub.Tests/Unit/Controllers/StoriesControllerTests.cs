using InteractHub.API.Controllers;
using InteractHub.API.DTOs;
using InteractHub.Application.Entities;
using InteractHub.Application.Entities.Enums;
using InteractHub.Application.Interfaces;
using InteractHub.Infrastructure.Hubs;
using InteractHub.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace InteractHub.Tests.Unit.Controllers;

public class StoriesControllerTests
{
    private static StoriesController CreateController(
        Mock<IStoryService> serviceMock,
        Mock<IFriendshipService>? friendshipMock = null,
        Mock<IHubContext<StoryHub>>? hubContextMock = null,
        Mock<IImageStorageService>? imageStorageMock = null)
    {
        var friendship = friendshipMock ?? CreateDefaultFriendshipMock();
        var hub = hubContextMock ?? CreateStoryHubContextMock();
        var imageStorage = imageStorageMock ?? new Mock<IImageStorageService>();
        return new StoriesController(serviceMock.Object, friendship.Object, hub.Object, imageStorage.Object);
    }

    private static Mock<IFriendshipService> CreateDefaultFriendshipMock()
    {
        var friendship = new Mock<IFriendshipService>();
        friendship.Setup(f => f.GetAcceptedFriendsAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<Friendship>());
        friendship.Setup(f =>
                f.CheckFriendshipStatusAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(FriendshipStatus.Accepted);
        return friendship;
    }

    private static Mock<IHubContext<StoryHub>> CreateStoryHubContextMock()
    {
        var clientProxy = new Mock<IClientProxy>();
        var hubClients = new Mock<IHubClients>();
        hubClients.Setup(c => c.Groups(It.IsAny<IReadOnlyList<string>>())).Returns(clientProxy.Object);
        var hubContext = new Mock<IHubContext<StoryHub>>();
        hubContext.Setup(h => h.Clients).Returns(hubClients.Object);
        return hubContext;
    }

    private static List<Friendship> AcceptedFriendshipsFor(string viewerId, params string[] otherUserIds)
    {
        return otherUserIds.Select(oid => new Friendship
        {
            UserId = viewerId,
            FriendId = oid,
            Friend = new User { Id = oid, UserName = oid },
            User = new User { Id = viewerId }
        }).ToList();
    }

    // GetAll tests - trả về tất cả stories (của bản thân và bạn bè được chấp nhận)
    [Fact]
    public async Task GetAll_ShouldReturnAllStories_WhenStoriesExist()
    {
        // given
        var stories = new List<Story>
        {
            new Story { Id = 1, UserId = "u1", Content = "story 1", ImageUrl = "img1.jpg", CreatedAt = DateTime.UtcNow, ExpireAt = DateTime.UtcNow.AddDays(1) },
            new Story { Id = 2, UserId = "u2", Content = "story 2", ImageUrl = "img2.jpg", CreatedAt = DateTime.UtcNow, ExpireAt = DateTime.UtcNow.AddDays(1) }
        };
        var serviceMock = new Mock<IStoryService>();
        serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(stories);

        var friendshipMock = CreateDefaultFriendshipMock();
        friendshipMock.Setup(f => f.GetAcceptedFriendsAsync("u1")).ReturnsAsync(
            AcceptedFriendshipsFor("u1", "u2"));

        var controller = CreateController(serviceMock, friendshipMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetAll();

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        serviceMock.Verify(s => s.GetAllAsync(), Times.Once);
    }

    // GetAll tests - trả về danh sách rỗng khi không có stories
    [Fact]
    public async Task GetAll_ShouldReturnEmpty_WhenNoStoriesExist()
    {
        // given
        var stories = new List<Story>();
        var serviceMock = new Mock<IStoryService>();
        serviceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(stories);
        var controller = CreateController(serviceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetAll();

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    // GetById tests - trả về story khi tồn tại
    [Fact]
    public async Task GetById_ShouldReturnStory_WhenStoryExists()
    {
        // given
        var story = new Story { Id = 1, UserId = "u1", Content = "test story", ImageUrl = "img.jpg", CreatedAt = DateTime.UtcNow, ExpireAt = DateTime.UtcNow.AddDays(1) };
        var serviceMock = new Mock<IStoryService>();
        serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(story);
        var controller = CreateController(serviceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetById(1);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        serviceMock.Verify(s => s.GetByIdAsync(1), Times.Once);
    }

    // GetById tests - trả về 404 khi story không tồn tại
    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenStoryMissing()
    {
        // given
        var serviceMock = new Mock<IStoryService>();
        serviceMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((Story?)null);
        var controller = CreateController(serviceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetById(999);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    // GetByUserId tests - trả về stories của một user
    [Fact]
    public async Task GetByUserId_ShouldReturnUserStories_WhenStoriesExist()
    {
        // given
        var userId = "u1";
        var stories = new List<Story>
        {
            new Story { Id = 1, UserId = userId, Content = "story 1", ImageUrl = "img1.jpg", CreatedAt = DateTime.UtcNow, ExpireAt = DateTime.UtcNow.AddDays(1) },
            new Story { Id = 2, UserId = userId, Content = "story 2", ImageUrl = "img2.jpg", CreatedAt = DateTime.UtcNow, ExpireAt = DateTime.UtcNow.AddDays(1) }
        };
        var serviceMock = new Mock<IStoryService>();
        serviceMock.Setup(s => s.GetByUserIdAsync(userId)).ReturnsAsync(stories);
        var controller = CreateController(serviceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetByUserId(userId);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        serviceMock.Verify(s => s.GetByUserIdAsync(userId), Times.Once);
    }

    // GetByUserId tests - trả về danh sách rỗng khi không có stories
    [Fact]
    public async Task GetByUserId_ShouldReturnEmpty_WhenNoStoriesForUser()
    {
        // given
        var userId = "u1";
        var stories = new List<Story>();
        var serviceMock = new Mock<IStoryService>();
        serviceMock.Setup(s => s.GetByUserIdAsync(userId)).ReturnsAsync(stories);
        var controller = CreateController(serviceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetByUserId(userId);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    // Create tests - tạo story thành công
    [Fact]
    public async Task Create_ShouldReturnCreated_WhenStoryIsValid()
    {
        // given
        var createDto = new CreateStoryDto { Content = "new story", ImageUrl = "image.jpg", ExpireAt = DateTime.UtcNow.AddDays(1) };
        var story = new Story { Id = 1, UserId = "u1", Content = createDto.Content, ImageUrl = createDto.ImageUrl, ExpireAt = createDto.ExpireAt, CreatedAt = DateTime.UtcNow };
        var serviceMock = new Mock<IStoryService>();
        serviceMock.Setup(s => s.CreateAsync(It.IsAny<Story>())).ReturnsAsync(story);
        serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(story);
        var controller = CreateController(serviceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.Create(createDto);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);
        serviceMock.Verify(s => s.CreateAsync(It.IsAny<Story>()), Times.Once);
        serviceMock.Verify(s => s.GetByIdAsync(1), Times.Once);
    }

    // Create tests - trả về 401 khi không có user claim
    [Fact]
    public async Task Create_ShouldReturnUnauthorized_WhenNoUserClaim()
    {
        // given
        var serviceMock = new Mock<IStoryService>();
        var controller = CreateController(serviceMock);
        ControllerTestHelper.SetAnonymous(controller);

        // when
        var result = await controller.Create(new CreateStoryDto { Content = "story", ExpireAt = DateTime.UtcNow.AddDays(1) });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(401, objectResult.StatusCode);
    }

    // Delete tests - xóa story thành công
    [Fact]
    public async Task Delete_ShouldReturnOk_WhenStoryOwner()
    {
        // given
        var story = new Story { Id = 3, UserId = "u1", Content = "abc", ExpireAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow };
        var serviceMock = new Mock<IStoryService>();
        serviceMock.Setup(s => s.GetByIdAsync(3)).ReturnsAsync(story);
        serviceMock.Setup(s => s.DeleteAsync(3)).ReturnsAsync(true);
        var controller = CreateController(serviceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.Delete(3);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(204, objectResult.StatusCode);
        serviceMock.Verify(s => s.DeleteAsync(3), Times.Once);
    }

    // Delete tests - trả về 404 khi story không tồn tại
    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenStoryMissing()
    {
        // given
        var serviceMock = new Mock<IStoryService>();
        serviceMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((Story?)null);
        var controller = CreateController(serviceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.Delete(999);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    // Delete tests - trả về 403 khi không phải chủ sở hữu
    [Fact]
    public async Task Delete_ShouldReturnForbidden_WhenCurrentUserIsNotOwner()
    {
        // given
        var story = new Story { Id = 3, UserId = "owner", Content = "abc", ExpireAt = DateTime.UtcNow.AddDays(1) };
        var serviceMock = new Mock<IStoryService>();
        serviceMock.Setup(s => s.GetByIdAsync(3)).ReturnsAsync(story);
        var controller = CreateController(serviceMock);
        ControllerTestHelper.SetUser(controller, "other");

        // when
        var result = await controller.Delete(3);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    // Delete tests - trả về 404 khi xóa thất bại
    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenDeleteFails()
    {
        // given
        var story = new Story { Id = 3, UserId = "u1", Content = "abc", ExpireAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow };
        var serviceMock = new Mock<IStoryService>();
        serviceMock.Setup(s => s.GetByIdAsync(3)).ReturnsAsync(story);
        serviceMock.Setup(s => s.DeleteAsync(3)).ReturnsAsync(false);
        var controller = CreateController(serviceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.Delete(3);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }
}
