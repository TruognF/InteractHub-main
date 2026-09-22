using InteractHub.Application.Entities;
using InteractHub.Application.Entities.Enums;
using InteractHub.Infrastructure.Service;
using InteractHub.Tests.Common;

namespace InteractHub.Tests.Unit.Services;

public class PostReportServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreatePostReport_WithValidData()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new PostReportService(context);
        var report = new PostReport 
        { 
            PostId = 1, 
            ReporterUserId = "u1", 
            Reason = ReportReason.Spam,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var created = await service.CreateAsync(report);

        // Assert
        Assert.NotNull(created);
        Assert.NotEqual(0, created.Id);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenReportNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new PostReportService(context);

        // Act
        var deleted = await service.DeleteAsync(809);

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveReport_WhenReportExists()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new PostReportService(context);
        var report = await service.CreateAsync(new PostReport 
        { 
            PostId = 1, 
            ReporterUserId = "u1", 
            Reason = ReportReason.Spam,
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var deleted = await service.DeleteAsync(report.Id);

        // Assert
        Assert.True(deleted);
    }
}

