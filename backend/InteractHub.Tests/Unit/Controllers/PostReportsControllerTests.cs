using AutoMapper;
using InteractHub.API.Controllers;
using InteractHub.API.DTOs;
using InteractHub.Application.Entities;
using InteractHub.Application.Entities.Enums;
using InteractHub.Application.Interfaces;
using InteractHub.Tests.Common;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InteractHub.Tests.Unit.Controllers;

public class PostReportsControllerTests
{
    private readonly Mock<IPostReportService> _postReportServiceMock;
    private readonly Mock<IPostService> _postServiceMock;
    private readonly Mock<IUserService> _userServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IPostDeletionLogService> _postDeletionLogServiceMock;
    private readonly Mock<IMapper> _mapperMock;
    private readonly PostReportsController _controller;

    public PostReportsControllerTests()
    {
        _postReportServiceMock = new Mock<IPostReportService>();
        _postServiceMock = new Mock<IPostService>();
        _userServiceMock = new Mock<IUserService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _postDeletionLogServiceMock = new Mock<IPostDeletionLogService>();
        _mapperMock = new Mock<IMapper>();

        _controller = new PostReportsController(
            _postReportServiceMock.Object,
            _postServiceMock.Object,
            _userServiceMock.Object,
            _notificationServiceMock.Object,
            _postDeletionLogServiceMock.Object,
            _mapperMock.Object
        );
    }

    // GetAll tests - trả về tất cả reports (Admin only)
    [Fact]
    public async Task GetAll_ShouldReturnAllReports_WhenReportsExist()
    {
        // Arrange
        var reports = new List<PostReport>
        {
            new PostReport { Id = 1, ReporterUserId = "u1", PostId = 1, Reason = ReportReason.Spam, CreatedAt = DateTime.UtcNow },
            new PostReport { Id = 2, ReporterUserId = "u2", PostId = 2, Reason = ReportReason.HarmfulContent, CreatedAt = DateTime.UtcNow }
        };
        _postReportServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(reports);
        ControllerTestHelper.SetUser(_controller, "admin");

        // Act
        var result = await _controller.GetAll();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        _postReportServiceMock.Verify(s => s.GetAllAsync(), Times.Once);
    }

    // GetAll tests - trả về danh sách rỗng khi không có reports
    [Fact]
    public async Task GetAll_ShouldReturnEmpty_WhenNoReportsExist()
    {
        // Arrange
        var reports = new List<PostReport>();
        _postReportServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(reports);
        ControllerTestHelper.SetUser(_controller, "admin");

        // Act
        var result = await _controller.GetAll();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    // GetById tests - trả về report khi tồn tại
    [Fact]
    public async Task GetById_ShouldReturnReport_WhenReportExists()
    {
        // given
        var report = new PostReport { Id = 1, ReporterUserId = "u1", PostId = 1, Reason = ReportReason.Spam, CreatedAt = DateTime.UtcNow };
        _postReportServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(report);
        ControllerTestHelper.SetUser(_controller, "u1");

        // when
        var result = await _controller.GetById(1);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
        _postReportServiceMock.Verify(s => s.GetByIdAsync(1), Times.Once);
    }

    // GetById tests - trả về 404 khi report không tồn tại
    [Fact]
    public async Task GetById_ShouldReturnNotFound_WhenReportMissing()
    {
        // given
        _postReportServiceMock.Setup(s => s.GetByIdAsync(999)).ReturnsAsync((PostReport?)null);
        ControllerTestHelper.SetUser(_controller, "u1");

        // when
        var result = await _controller.GetById(999);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    // Create tests - tạo report thành công
    [Fact]
    public async Task Create_ShouldReturnCreated_WhenReportIsValid()
    {
        // given
        var createDto = new CreatePostReportDto { PostId = 1, Reason = ReportReason.Spam };
        var post = new Post { Id = 1, UserId = "author_id", Content = "test" }; // Post owner != current user (u1)
        var existingReports = new List<PostReport>(); // No existing report
        var report = new PostReport { Id = 1, ReporterUserId = "u1", PostId = 1, Reason = createDto.Reason, CreatedAt = DateTime.UtcNow };
        
        _postServiceMock.Setup(s => s.GetByIdAsync(1)).ReturnsAsync(post);
        _postReportServiceMock.Setup(s => s.GetAllAsync()).ReturnsAsync(existingReports);
        _postReportServiceMock.Setup(s => s.CreateAsync(It.IsAny<PostReport>())).ReturnsAsync(report);
        
        ControllerTestHelper.SetUser(_controller, "u1");

        // when
        var result = await _controller.CreateReport(createDto);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);
        _postReportServiceMock.Verify(s => s.CreateAsync(It.IsAny<PostReport>()), Times.Once);
    }

    // Create tests - trả về 401 khi không có user claim
    [Fact]
    public async Task Create_ShouldReturnUnauthorized_WhenNoUserClaim()
    {
        // given
        ControllerTestHelper.SetAnonymous(_controller);

        // when
        var result = await _controller.CreateReport(new CreatePostReportDto { PostId = 1, Reason = ReportReason.Spam });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(401, objectResult.StatusCode);
    }

    // RejectReport tests - từ chối report thành công
    [Fact]
    public async Task RejectReport_ShouldReturnSuccess_WhenReportRejected()
    {
        // given
        var report = new PostReport { Id = 11, ReporterUserId = "u1", PostId = 1, Reason = ReportReason.Spam, CreatedAt = DateTime.UtcNow, Status = ReportStatus.Pending };
        _postReportServiceMock.Setup(s => s.GetByIdAsync(11)).ReturnsAsync(report);
        ControllerTestHelper.SetUser(_controller, "admin");

        // when
        var result = await _controller.RejectReport(11);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        _postReportServiceMock.Verify(s => s.UpdateAsync(It.IsAny<PostReport>()), Times.Once);
    }

    // RejectReport tests - trả về 404 khi report không tồn tại
    [Fact]
    public async Task RejectReport_ShouldReturnNotFound_WhenReportMissing()
    {
        // given
        _postReportServiceMock.Setup(s => s.GetByIdAsync(11)).ReturnsAsync((PostReport?)null);
        ControllerTestHelper.SetUser(_controller, "admin");

        // when
        var result = await _controller.RejectReport(11);

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }
}

