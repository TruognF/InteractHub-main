using InteractHub.API.Controllers;
using InteractHub.Application.Entities;
using InteractHub.Application.Entities.Enums;
using InteractHub.Application.Interfaces;
using InteractHub.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Moq;
using InteractHub.API.DTOs;

namespace InteractHub.Tests.Unit.Controllers;

public class NotificationsControllerTests
{
    // GetById tests - trả về notification khi tồn tại
    [Fact]
    public async Task GetById_ShouldReturnNotification_WhenNotificationExists()
    {
        // Arrange
        var notification = new Notification { Id = 1, UserId = "u1", Content = "New like", Type = NotificationType.Like, IsRead = false, CreatedAt = DateTime.UtcNow };
        var serviceMock = new Mock<INotificationService>();
        serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(notification);
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, "u1");

        // Act
        var result = await controller.GetById(1);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        serviceMock.Verify(s => s.GetByIdAsync(1), Times.Once);
    }

    // GetById tests - trả về 404 khi notification không tồn tại
    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenNotificationMissing()
    {
        // Arrange
        var serviceMock = new Mock<INotificationService>();
        serviceMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((Notification?)null);
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, "u1");

        // Act
        var result = await controller.GetById(999);

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    // GetByUserId tests - trả về notifications của user
    [Fact]
    public async Task GetByUserId_ShouldReturnUserNotifications_WhenNotificationsExist()
    {
        // given
        var userId = "u1";
        var notifications = new List<Notification>
        {
            new Notification { Id = 1, UserId = userId, Content = "New like", Type = NotificationType.Like, IsRead = false, CreatedAt = DateTime.UtcNow },
            new Notification { Id = 2, UserId = userId, Content = "New comment", Type = NotificationType.Comment, IsRead = true, CreatedAt = DateTime.UtcNow }
        };
        var serviceMock = new Mock<INotificationService>();
        serviceMock.Setup(s => s.GetByUserIdAsync(userId)).ReturnsAsync(notifications);
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, userId);

        // when
        var result = await controller.GetByUserId(userId);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        serviceMock.Verify(s => s.GetByUserIdAsync(userId), Times.Once);
    }

    // GetByUserId tests - trả về 403 khi user cố xem notifications của người khác
    [Fact]
    public async Task GetByUserId_ShouldReturnForbidden_WhenUserRequestsAnotherUsersNotifications()
    {
        // given
        var serviceMock = new Mock<INotificationService>();
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetByUserId("u2");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    // GetByUserId tests - trả về danh sách rỗng khi không có notifications
    [Fact]
    public async Task GetByUserId_ShouldReturnEmpty_WhenNoNotificationsExist()
    {
        // given
        var userId = "u1";
        var notifications = new List<Notification>();
        var serviceMock = new Mock<INotificationService>();
        serviceMock.Setup(s => s.GetByUserIdAsync(userId)).ReturnsAsync(notifications);
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, userId);

        // when
        var result = await controller.GetByUserId(userId);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    // GetUnreadNotifications tests - trả về notifications chưa đọc
    [Fact]
    public async Task GetUnreadNotifications_ShouldReturnUnreadNotifications_WhenUnreadExist()
    {
        // given
        var userId = "u1";
        var unreadNotifications = new List<Notification>
        {
            new Notification { Id = 1, UserId = userId, Content = "New like", Type = NotificationType.Like, IsRead = false, CreatedAt = DateTime.UtcNow }
        };
        var serviceMock = new Mock<INotificationService>();
        serviceMock.Setup(s => s.GetUnreadNotificationsAsync(userId)).ReturnsAsync(unreadNotifications);
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, userId);

        // when
        var result = await controller.GetUnreadNotifications(userId);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        serviceMock.Verify(s => s.GetUnreadNotificationsAsync(userId), Times.Once);
    }

    // GetUnreadNotifications tests - trả về 403 khi user cố xem của người khác
    [Fact]
    public async Task GetUnreadNotifications_ShouldReturnForbidden_WhenUserRequestsAnotherUsersUnread()
    {
        // given
        var serviceMock = new Mock<INotificationService>();
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetUnreadNotifications("u2");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    // GetUnreadCount tests - trả về số lượng notifications chưa đọc
    [Fact]
    public async Task GetUnreadCount_ShouldReturnUnreadCount_WhenCalled()
    {
        // given
        var userId = "u1";
        var count = 5;
        var serviceMock = new Mock<INotificationService>();
        serviceMock.Setup(s => s.GetUnreadCountAsync(userId)).ReturnsAsync(count);
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, userId);

        // when
        var result = await controller.GetUnreadCount(userId);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        serviceMock.Verify(s => s.GetUnreadCountAsync(userId), Times.Once);
    }

    // GetUnreadCount tests - trả về 403 khi user cố xem của người khác
    [Fact]
    public async Task GetUnreadCount_ShouldReturnForbidden_WhenUserRequestsAnotherUsersCount()
    {
        // given
        var serviceMock = new Mock<INotificationService>();
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.GetUnreadCount("u2");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    // MarkAsRead tests - đánh dấu notification là đã đọc
    [Fact]
    public async Task MarkAsRead_ShouldReturnOk_WhenNotificationOwner()
    {
        // given
        var notification = new Notification { Id = 50, UserId = "u1", Content = "test", Type = NotificationType.Like, IsRead = false, CreatedAt = DateTime.UtcNow };
        var serviceMock = new Mock<INotificationService>();
        serviceMock.Setup(s => s.GetByIdAsync(50)).ReturnsAsync(notification);
        serviceMock.Setup(s => s.MarkAsReadAsync(50)).ReturnsAsync(true);
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.MarkAsRead(50);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        serviceMock.Verify(s => s.MarkAsReadAsync(50), Times.Once);
    }

    // MarkAsRead tests - trả về 404 khi notification không tồn tại
    [Fact]
    public async Task MarkAsRead_ShouldReturnNotFound_WhenNotificationMissing()
    {
        // given
        var serviceMock = new Mock<INotificationService>();
        serviceMock.Setup(s => s.GetByIdAsync(50)).ReturnsAsync((Notification?)null);
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.MarkAsRead(50);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    // MarkAsRead tests - trả về 403 khi không phải chủ sở hữu
    [Fact]
    public async Task MarkAsRead_ShouldReturnForbidden_WhenNotNotificationOwner()
    {
        // given
        var notification = new Notification { Id = 50, UserId = "u2", Content = "test", Type = NotificationType.Like, IsRead = false };
        var serviceMock = new Mock<INotificationService>();
        serviceMock.Setup(s => s.GetByIdAsync(50)).ReturnsAsync(notification);
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.MarkAsRead(50);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    // MarkAllAsRead tests - đánh dấu tất cả notifications là đã đọc
    [Fact]
    public async Task MarkAllAsRead_ShouldReturnOk_WhenCalled()
    {
        // given
        var userId = "u1";
        var serviceMock = new Mock<INotificationService>();
        serviceMock.Setup(s => s.MarkAllAsReadAsync(userId)).ReturnsAsync(true);
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, userId);

        // when
        var result = await controller.MarkAllAsRead(userId);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        serviceMock.Verify(s => s.MarkAllAsReadAsync(userId), Times.Once);
    }

    // MarkAllAsRead tests - trả về 403 khi user cố update của người khác
    [Fact]
    public async Task MarkAllAsRead_ShouldReturnForbidden_WhenUserIdNotMatchCurrentUser()
    {
        // given
        var serviceMock = new Mock<INotificationService>();
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.MarkAllAsRead("u2");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    // Delete tests - xóa notification thành công
    [Fact]
    public async Task Delete_ShouldReturnOk_WhenNotificationOwner()
    {
        // given
        var notification = new Notification { Id = 1, UserId = "u1", Content = "test", Type = NotificationType.Like, IsRead = false, CreatedAt = DateTime.UtcNow };
        var serviceMock = new Mock<INotificationService>();
        serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(notification);
        serviceMock.Setup(s => s.DeleteAsync(1)).ReturnsAsync(true);
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.Delete(1);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        serviceMock.Verify(s => s.DeleteAsync(1), Times.Once);
    }

    // Delete tests - trả về 404 khi notification không tồn tại
    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenNotificationMissing()
    {
        // given
        var serviceMock = new Mock<INotificationService>();
        serviceMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((Notification?)null);
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.Delete(999);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    // Delete tests - trả về 403 khi không phải chủ sở hữu
    [Fact]
    public async Task Delete_ShouldReturnForbidden_WhenNotNotificationOwner()
    {
        // given
        var notification = new Notification { Id = 1, UserId = "u2", Content = "test", Type = NotificationType.Like, IsRead = false };
        var serviceMock = new Mock<INotificationService>();
        serviceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(notification);
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.Delete(1);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    // DeleteByType tests - xóa notifications theo type
    [Fact]
    public async Task DeleteByType_ShouldReturnOk_WhenTypeIsValid()
    {
        // given
        var userId = "u1";
        var serviceMock = new Mock<INotificationService>();
        serviceMock.Setup(s => s.DeleteNotificationsByTypeAsync(userId, NotificationType.Like)).ReturnsAsync(true);
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, userId);

        // when
        var result = await controller.DeleteByType(userId, "Like");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        serviceMock.Verify(s => s.DeleteNotificationsByTypeAsync(userId, NotificationType.Like), Times.Once);
    }

    // DeleteByType tests - trả về 403 khi user cố xóa của người khác
    [Fact]
    public async Task DeleteByType_ShouldReturnForbidden_WhenUserIdNotMatchCurrentUser()
    {
        // given
        var serviceMock = new Mock<INotificationService>();
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, "u1");

        // when
        var result = await controller.DeleteByType("u2", "Like");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, objectResult.StatusCode);
    }

    // DeleteByType tests - trả về 400 khi type không hợp lệ
    [Fact]
    public async Task DeleteByType_ShouldReturnBadRequest_WhenTypeIsInvalid()
    {
        // given
        var userId = "u1";
        var serviceMock = new Mock<INotificationService>();
        var controller = new NotificationsController(serviceMock.Object);
        ControllerTestHelper.SetUser(controller, userId);

        // when
        var result = await controller.DeleteByType(userId, "InvalidType");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }
}
