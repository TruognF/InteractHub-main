using InteractHub.Application.Entities;
using InteractHub.Infrastructure.Service;
using InteractHub.Tests.Common;

namespace InteractHub.Tests.Unit.Services;

public class LikeServiceTests
{
    [Fact]
    public async Task GetLikeCountAsync_ShouldReturnCorrectCount_ForPost()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new LikeService(context);
        await service.CreateAsync(new Like { PostId = 10, UserId = "u1" });
        await service.CreateAsync(new Like { PostId = 10, UserId = "u2" });
        await service.CreateAsync(new Like { PostId = 11, UserId = "u3" });

        // Act
        var count = await service.GetLikeCountAsync(10);

        // Assert
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenLikeNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new LikeService(context);

        // Act
        var deleted = await service.DeleteAsync(99);

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateLike_WithValidData()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new LikeService(context);
        var like = new Like { PostId = 1, UserId = "u1" };

        // Act
        var created = await service.CreateAsync(like);

        // Assert
        Assert.NotNull(created);
        Assert.Single(context.Likes);
    }
}
