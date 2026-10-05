using InteractHub.API.Controllers;
using InteractHub.API.DTOs;
using InteractHub.Application.Entities;
using InteractHub.Application.Interfaces;
using InteractHub.Infrastructure.Hubs;
using InteractHub.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Moq;
using Xunit;

namespace InteractHub.Tests.Unit.Controllers;

public class MessagesControllerTests
{
    private MessagesController CreateController(
        Mock<IMessageService> messageServiceMock,
        Mock<INotificationService>? notificationServiceMock = null,
        Mock<IGroupService>? groupServiceMock = null,
        Mock<IHubContext<MessageHub>>? messageHubMock = null,
        Mock<IImageStorageService>? imageStorageMock = null)
    {
        var notifMock = notificationServiceMock ?? new Mock<INotificationService>();
        var grpMock = groupServiceMock ?? new Mock<IGroupService>();
        var hubMock = messageHubMock ?? SignalRMockFactory.CreateMessageHubMock();
        var imgMock = imageStorageMock ?? new Mock<IImageStorageService>();

        return new MessagesController(
            messageServiceMock.Object,
            notifMock.Object,
            grpMock.Object,
            hubMock.Object,
            imgMock.Object);
    }

    [Fact]
    public async Task SendMessage_ShouldReturnBadRequest_WhenContentExceeds2000Characters()
    {
        // Arrange
        var messageServiceMock = new Mock<IMessageService>();
        var controller = CreateController(messageServiceMock);
        ControllerTestHelper.SetUser(controller, "user-1");

        var longContent = new string('a', 2001);
        var dto = new CreateMessageDto
        {
            ReceiverId = "user-2",
            Content = longContent
        };

        // Act
        var result = await controller.SendMessage(dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Nội dung tin nhắn không được vượt quá 2000 ký tự", badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateMessage_ShouldReturnSuccess_WhenSenderUpdatesContent()
    {
        // Arrange
        var messageServiceMock = new Mock<IMessageService>();
        var existingMessage = new Message
        {
            Id = 10,
            SenderId = "user-1",
            ReceiverId = "user-2",
            Content = "Old content",
            IsDeleted = false
        };
        var updatedMessage = new Message
        {
            Id = 10,
            SenderId = "user-1",
            ReceiverId = "user-2",
            Content = "New content",
            IsEdited = true,
            IsDeleted = false
        };

        messageServiceMock.Setup(s => s.GetByIdAsync(10)).ReturnsAsync(existingMessage);
        messageServiceMock.Setup(s => s.UpdateMessageAsync(10, "user-1", "New content")).ReturnsAsync(updatedMessage);

        var controller = CreateController(messageServiceMock);
        ControllerTestHelper.SetUser(controller, "user-1");

        var dto = new UpdateMessageDto { Content = "New content" };

        // Act
        var result = await controller.UpdateMessage(10, dto);

        // Assert
        var okResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.NotNull(okResult.Value);
        messageServiceMock.Verify(s => s.UpdateMessageAsync(10, "user-1", "New content"), Times.Once);
    }

    [Fact]
    public async Task UpdateMessage_ShouldReturnBadRequest_WhenContentExceeds2000Characters()
    {
        // Arrange
        var messageServiceMock = new Mock<IMessageService>();
        var controller = CreateController(messageServiceMock);
        ControllerTestHelper.SetUser(controller, "user-1");

        var dto = new UpdateMessageDto { Content = new string('b', 2005) };

        // Act
        var result = await controller.UpdateMessage(10, dto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Nội dung tin nhắn không được vượt quá 2000 ký tự", badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateMessage_ShouldReturnForbidden_WhenNotSender()
    {
        // Arrange
        var messageServiceMock = new Mock<IMessageService>();
        var existingMessage = new Message
        {
            Id = 10,
            SenderId = "user-other",
            ReceiverId = "user-1",
            Content = "Hello",
            IsDeleted = false
        };
        messageServiceMock.Setup(s => s.GetByIdAsync(10)).ReturnsAsync(existingMessage);

        var controller = CreateController(messageServiceMock);
        ControllerTestHelper.SetUser(controller, "user-1");

        var dto = new UpdateMessageDto { Content = "Hacked content" };

        // Act
        var result = await controller.UpdateMessage(10, dto);

        // Assert
        var objResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objResult.StatusCode);
    }

    [Fact]
    public async Task RecallMessage_ShouldReturnSuccess_WhenSenderRecallsMessage()
    {
        // Arrange
        var messageServiceMock = new Mock<IMessageService>();
        var existingMessage = new Message
        {
            Id = 10,
            SenderId = "user-1",
            ReceiverId = "user-2",
            Content = "Secret message",
            IsDeleted = false
        };
        var recalledMessage = new Message
        {
            Id = 10,
            SenderId = "user-1",
            ReceiverId = "user-2",
            Content = "Secret message",
            IsDeleted = true
        };

        messageServiceMock.Setup(s => s.GetByIdAsync(10)).ReturnsAsync(existingMessage);
        messageServiceMock.Setup(s => s.RecallMessageAsync(10, "user-1")).ReturnsAsync(recalledMessage);

        var controller = CreateController(messageServiceMock);
        ControllerTestHelper.SetUser(controller, "user-1");

        // Act
        var result = await controller.RecallMessage(10);

        // Assert
        var okResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status200OK, okResult.StatusCode);
        Assert.NotNull(okResult.Value);
        messageServiceMock.Verify(s => s.RecallMessageAsync(10, "user-1"), Times.Once);
    }

    [Fact]
    public async Task RecallMessage_ShouldReturnForbidden_WhenNotSender()
    {
        // Arrange
        var messageServiceMock = new Mock<IMessageService>();
        var existingMessage = new Message
        {
            Id = 10,
            SenderId = "user-other",
            ReceiverId = "user-1",
            Content = "Secret message",
            IsDeleted = false
        };
        messageServiceMock.Setup(s => s.GetByIdAsync(10)).ReturnsAsync(existingMessage);

        var controller = CreateController(messageServiceMock);
        ControllerTestHelper.SetUser(controller, "user-1");

        // Act
        var result = await controller.RecallMessage(10);

        // Assert
        var objResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, objResult.StatusCode);
    }

    [Fact]
    public async Task SendMessage_ShouldSucceed_WhenOnlyImageIsProvided()
    {
        // Arrange
        var messageServiceMock = new Mock<IMessageService>();
        var imageStorageMock = new Mock<IImageStorageService>();
        imageStorageMock.Setup(s => s.SaveDataUriAsync("data:image/png;base64,abc", "messages", null))
            .ReturnsAsync("/uploads/messages/img.png");

        var createdMessage = new Message
        {
            Id = 99,
            SenderId = "user-1",
            ReceiverId = "user-2",
            Content = "",
            ImageUrl = "/uploads/messages/img.png",
            CreatedAt = DateTime.UtcNow
        };

        messageServiceMock.Setup(s => s.SendMessageAsync("user-1", "user-2", "", "/uploads/messages/img.png"))
            .ReturnsAsync(createdMessage);

        var controller = CreateController(messageServiceMock, imageStorageMock: imageStorageMock);
        ControllerTestHelper.SetUser(controller, "user-1");

        var dto = new CreateMessageDto
        {
            Content = "",
            ImageUrl = "data:image/png;base64,abc",
            ReceiverId = "user-2"
        };

        // Act
        var result = await controller.SendMessage(dto);

        // Assert
        var createdResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status201Created, createdResult.StatusCode);
        messageServiceMock.Verify(s => s.SendMessageAsync("user-1", "user-2", "", "/uploads/messages/img.png"), Times.Once);
    }
}
