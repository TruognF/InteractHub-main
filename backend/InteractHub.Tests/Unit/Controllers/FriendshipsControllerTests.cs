using InteractHub.API.Controllers;
using InteractHub.API.DTOs;
using InteractHub.Application.Entities;
using InteractHub.Application.Entities.Enums;
using InteractHub.Application.Interfaces;
using InteractHub.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Microsoft.AspNetCore.SignalR;
using InteractHub.Infrastructure.Hubs;

namespace InteractHub.Tests.Unit.Controllers;

public class FriendshipsControllerTests
{
    private FriendshipsController CreateController(
        Mock<IFriendshipService> friendshipMock,
        Mock<IMessageService> messageMock,
        Mock<IUserPresenceService> presenceMock)
    {
        var notificationHubMock = new Mock<IHubContext<NotificationHub>>();
        var mockClients = new Mock<IHubClients>();
        var mockClientProxy = new Mock<IClientProxy>();
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        notificationHubMock.Setup(h => h.Clients).Returns(mockClients.Object);
        return new FriendshipsController(
            friendshipMock.Object,
            messageMock.Object,
            presenceMock.Object,
            notificationHubMock.Object);
    }

    // GetById tests - trả về friendship khi tồn tại
    [Fact]
    public async Task GetById_ShouldReturnFriendship_WhenFriendshipExists()
    {
        // Arrange
        var friendship = new Friendship { Id = 1, UserId = "u1", FriendId = "u2", Status = FriendshipStatus.Accepted, CreatedAt = DateTime.UtcNow };
        var friendshipMock = new Mock<IFriendshipService>();
        friendshipMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(friendship);
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // Act
        var result = await controller.GetById(1);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        friendshipMock.Verify(s => s.GetByIdAsync(1), Times.Once);
    }

    // GetById tests - trả về 404 khi friendship không tồn tại
    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenFriendshipMissing()
    {
        // Arrange
        var friendshipMock = new Mock<IFriendshipService>();
        friendshipMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((Friendship?)null);
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // Act
        var result = await controller.GetById(999);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    // GetAcceptedFriends tests - trả về danh sách bạn bè đã chấp nhận
    [Fact]
    public async Task GetAcceptedFriends_ShouldReturnAcceptedFriends_WhenFriendsExist()
    {
        // given
        var userId = "u1";
        var friends = new List<Friendship>
        {
            new Friendship { Id = 1, UserId = userId, FriendId = "u2", Status = FriendshipStatus.Accepted, CreatedAt = DateTime.UtcNow },
            new Friendship { Id = 2, UserId = userId, FriendId = "u3", Status = FriendshipStatus.Accepted, CreatedAt = DateTime.UtcNow }
        };
        var metadata = new InteractHub.Application.Helpers.PaginationMetadata { TotalCount = 2, PageNumber = 1, PageSize = 20, TotalPages = 1 };
        var friendshipMock = new Mock<IFriendshipService>();
        friendshipMock.Setup(s => s.GetAcceptedFriendsPaginatedAsync(userId, 1, 20)).ReturnsAsync((Friends: friends, Metadata: metadata));
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetAcceptedFriends(userId);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        friendshipMock.Verify(s => s.GetAcceptedFriendsPaginatedAsync(userId, 1, 20), Times.Once);
    }

    // GetAcceptedFriends tests - trả về danh sách rỗng khi không có bạn bè
    [Fact]
    public async Task GetAcceptedFriends_ShouldReturnEmpty_WhenNoAcceptedFriendships()
    {
        // given
        var userId = "u1";
        var friends = new List<Friendship>();
        var metadata = new InteractHub.Application.Helpers.PaginationMetadata { TotalCount = 0, PageNumber = 1, PageSize = 20, TotalPages = 0 };
        var friendshipMock = new Mock<IFriendshipService>();
        friendshipMock.Setup(s => s.GetAcceptedFriendsPaginatedAsync(userId, 1, 20)).ReturnsAsync((Friends: friends, Metadata: metadata));
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetAcceptedFriends(userId);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    // GetPendingRequests tests - trả về danh sách lời mời chờ xử lý
    [Fact]
    public async Task GetPendingRequests_ShouldReturnPendingRequests_WhenRequestsExist()
    {
        // given
        var userId = "u1";
        var requests = new List<Friendship>
        {
            new Friendship { Id = 1, UserId = "u2", FriendId = userId, Status = FriendshipStatus.Pending, CreatedAt = DateTime.UtcNow },
            new Friendship { Id = 2, UserId = "u3", FriendId = userId, Status = FriendshipStatus.Pending, CreatedAt = DateTime.UtcNow }
        };
        var metadata = new InteractHub.Application.Helpers.PaginationMetadata { TotalCount = 2, PageNumber = 1, PageSize = 20, TotalPages = 1 };
        var friendshipMock = new Mock<IFriendshipService>();
        friendshipMock.Setup(s => s.GetPendingRequestsPaginatedAsync(userId, 1, 20)).ReturnsAsync((Requests: requests, Metadata: metadata));
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetUser(controller, userId);

        // when
        var result = await controller.GetPendingRequests(userId);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        friendshipMock.Verify(s => s.GetPendingRequestsPaginatedAsync(userId, 1, 20), Times.Once);
    }

    // GetPendingRequests tests - trả về 403 khi user cố xem lời mời của người khác
    [Fact]
    public async Task GetPendingRequests_ShouldReturnForbidden_WhenUserId_NotMatchCurrentUser()
    {
        // given
        var friendshipMock = new Mock<IFriendshipService>();
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetPendingRequests("u2");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    // GetPendingRequests tests - trả về danh sách rỗng khi không có lời mời
    [Fact]
    public async Task GetPendingRequests_ShouldReturnEmpty_WhenNoPendingRequests()
    {
        // given
        var userId = "u1";
        var requests = new List<Friendship>();
        var metadata = new InteractHub.Application.Helpers.PaginationMetadata { TotalCount = 0, PageNumber = 1, PageSize = 20, TotalPages = 0 };
        var friendshipMock = new Mock<IFriendshipService>();
        friendshipMock.Setup(s => s.GetPendingRequestsPaginatedAsync(userId, 1, 20)).ReturnsAsync((Requests: requests, Metadata: metadata));
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetUser(controller, userId);

        // when
        var result = await controller.GetPendingRequests(userId);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    // SendFriendRequest tests - gửi lời mời kết bạn thành công
    [Fact]
    public async Task SendFriendRequest_ShouldReturnCreated_WhenRequestIsValid()
    {
        // given
        var requestDto = new SendFriendRequestDto { FriendId = "u2" };
        var friendship = new Friendship { Id = 1, UserId = "u1", FriendId = "u2", Status = FriendshipStatus.Pending, CreatedAt = DateTime.UtcNow };
        var friendshipMock = new Mock<IFriendshipService>();
        friendshipMock.Setup(s => s.SendFriendRequestAsync("u1", "u2")).ReturnsAsync(friendship);
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.SendFriendRequest(requestDto);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);
        friendshipMock.Verify(s => s.SendFriendRequestAsync("u1", "u2"), Times.Once);
    }

    // SendFriendRequest tests - trả về 401 khi không có user claim
    [Fact]
    public async Task SendFriendRequest_ShouldReturnUnauthorized_WhenNoUserClaim()
    {
        // given
        var friendshipMock = new Mock<IFriendshipService>();
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetAnonymous(controller);

        // when
        var result = await controller.SendFriendRequest(new SendFriendRequestDto { FriendId = "u2" });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(401, objectResult.StatusCode);
    }

    // SendFriendRequest tests - trả về 400 khi có lỗi validation
    [Fact]
    public async Task SendFriendRequest_ShouldReturnBadRequest_WhenServiceThrowsException()
    {
        // given
        var requestDto = new SendFriendRequestDto { FriendId = "u2" };
        var friendshipMock = new Mock<IFriendshipService>();
        friendshipMock.Setup(s => s.SendFriendRequestAsync("u1", "u2")).ThrowsAsync(new InvalidOperationException("Already friends"));
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.SendFriendRequest(requestDto);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // AcceptFriendRequest tests - chấp nhận lời mời thành công
    [Fact]
    public async Task AcceptFriendRequest_ShouldReturnOk_WhenRequestIsValid()
    {
        // given
        var friendship = new Friendship { Id = 1, UserId = "u2", FriendId = "u1", Status = FriendshipStatus.Accepted, CreatedAt = DateTime.UtcNow };
        var friendshipMock = new Mock<IFriendshipService>();
        friendshipMock.Setup(s => s.AcceptFriendRequestAsync(1, "u1")).ReturnsAsync(friendship);
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.AcceptFriendRequest(1);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        friendshipMock.Verify(s => s.AcceptFriendRequestAsync(1, "u1"), Times.Once);
    }

    // AcceptFriendRequest tests - trả về 400 khi lời mời không hợp lệ
    [Fact]
    public async Task AcceptFriendRequest_ShouldReturnBadRequest_WhenRequestInvalid()
    {
        // given
        var friendshipMock = new Mock<IFriendshipService>();
        friendshipMock.Setup(s => s.AcceptFriendRequestAsync(999, "u1")).ThrowsAsync(new InvalidOperationException("Request not found"));
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.AcceptFriendRequest(999);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // DeclineFriendRequest tests - từ chối lời mời thành công
    [Fact]
    public async Task DeclineFriendRequest_ShouldReturnOk_WhenRequestIsValid()
    {
        // given
        var friendshipMock = new Mock<IFriendshipService>();
        friendshipMock.Setup(s => s.DeclineFriendRequestAsync(22, "u1")).ReturnsAsync(true);
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.DeclineFriendRequest(22);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        friendshipMock.Verify(s => s.DeclineFriendRequestAsync(22, "u1"), Times.Once);
    }

    // DeclineFriendRequest tests - trả về 400 khi từ chối thất bại
    [Fact]
    public async Task DeclineFriendRequest_ShouldReturnBadRequest_WhenServiceReturnsFalse()
    {
        // given
        var friendshipMock = new Mock<IFriendshipService>();
        friendshipMock.Setup(s => s.DeclineFriendRequestAsync(22, "u1")).ReturnsAsync(false);
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.DeclineFriendRequest(22);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // RemoveFriend tests - xóa bạn thành công
    [Fact]
    public async Task RemoveFriend_ShouldReturnOk_WhenFriendExists()
    {
        // given
        var friendshipMock = new Mock<IFriendshipService>();
        friendshipMock.Setup(s => s.RemoveFriendAsync("u1", "u2")).ReturnsAsync(true);
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.RemoveFriend("u2");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        friendshipMock.Verify(s => s.RemoveFriendAsync("u1", "u2"), Times.Once);
    }

    // RemoveFriend tests - trả về 404 khi bạn không tồn tại
    [Fact]
    public async Task RemoveFriend_ShouldReturnNotFound_WhenFriendDoesNotExist()
    {
        // given
        var friendshipMock = new Mock<IFriendshipService>();
        friendshipMock.Setup(s => s.RemoveFriendAsync("u1", "u999")).ReturnsAsync(false);
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.RemoveFriend("u999");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    // RemoveFriend tests - trả về 401 khi không có user claim
    [Fact]
    public async Task RemoveFriend_ShouldReturnUnauthorized_WhenNoUserClaim()
    {
        // given
        var friendshipMock = new Mock<IFriendshipService>();
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetAnonymous(controller);

        // when
        var result = await controller.RemoveFriend("u2");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(401, objectResult.StatusCode);
    }

    // BlockUser tests - chặn người dùng thành công
    [Fact]
    public async Task BlockUser_ShouldReturnOk_WhenBlockIsValid()
    {
        // given
        var friendship = new Friendship { Id = 1, UserId = "u1", FriendId = "u2", Status = FriendshipStatus.Blocked, CreatedAt = DateTime.UtcNow };
        var friendshipMock = new Mock<IFriendshipService>();
        friendshipMock.Setup(s => s.BlockUserAsync("u1", "u2")).ReturnsAsync(friendship);
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.BlockUser("u2");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        friendshipMock.Verify(s => s.BlockUserAsync("u1", "u2"), Times.Once);
    }

    // BlockUser tests - trả về 401 khi không có user claim
    [Fact]
    public async Task BlockUser_ShouldReturnUnauthorized_WhenNoUserClaim()
    {
        // given
        var friendshipMock = new Mock<IFriendshipService>();
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetAnonymous(controller);

        // when
        var result = await controller.BlockUser("u2");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(401, objectResult.StatusCode);
    }

    // CheckFriendshipStatus tests - kiểm tra trạng thái kết bạn
    [Fact]
    public async Task CheckFriendshipStatus_ShouldReturnStatus_WhenStatusExists()
    {
        // given
        var friendshipMock = new Mock<IFriendshipService>();
        friendshipMock.Setup(s => s.CheckFriendshipStatusAsync("u1", "u2")).ReturnsAsync(FriendshipStatus.Accepted);
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.CheckFriendshipStatus("u2");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        friendshipMock.Verify(s => s.CheckFriendshipStatusAsync("u1", "u2"), Times.Once);
    }

    // CheckFriendshipStatus tests - trả về None khi không có kết bạn
    [Fact]
    public async Task CheckFriendshipStatus_ShouldReturnNone_WhenStatusDoesNotExist()
    {
        // given
        var friendshipMock = new Mock<IFriendshipService>();
        friendshipMock.Setup(s => s.CheckFriendshipStatusAsync("u1", "u999")).ReturnsAsync((FriendshipStatus?)null);
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.CheckFriendshipStatus("u999");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    // CheckFriendshipStatus tests - trả về 401 khi không có user claim
    [Fact]
    public async Task CheckFriendshipStatus_ShouldReturnUnauthorized_WhenNoUserClaim()
    {
        // given
        var friendshipMock = new Mock<IFriendshipService>();
        var messageMock = new Mock<IMessageService>();
        var presenceMock = new Mock<IUserPresenceService>();
        var controller = CreateController(friendshipMock, messageMock, presenceMock);
        ControllerTestHelper.SetAnonymous(controller);

        // when
        var result = await controller.CheckFriendshipStatus("u2");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(401, objectResult.StatusCode);
    }
}

