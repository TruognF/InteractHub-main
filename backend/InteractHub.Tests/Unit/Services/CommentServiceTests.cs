using InteractHub.Application.Entities;
using InteractHub.Infrastructure.Service;
using InteractHub.Tests.Common;

namespace InteractHub.Tests.Unit.Services;

public class CommentServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistComment_WithValidData()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new CommentService(context);
        var comment = new Comment 
        { 
            Content = "test comment", 
            PostId = 1, 
            UserId = "u1",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var created = await service.CreateAsync(comment);

        // Assert
        Assert.NotEqual(0, created.Id);
        Assert.Single(context.Comments);
        Assert.Equal("test comment", created.Content);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenCommentNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new CommentService(context);

        // Act
        var deleted = await service.DeleteAsync(999);

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveComment_WhenCommentExists()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new CommentService(context);
        var comment = await service.CreateAsync(new Comment 
        { 
            Content = "test comment", 
            PostId = 1, 
            UserId = "u1",
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var deleted = await service.DeleteAsync(comment.Id);

        // Assert
        Assert.True(deleted);
        Assert.Empty(context.Comments);
    }
}
