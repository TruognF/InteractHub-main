using InteractHub.Application.Entities;
using InteractHub.Infrastructure.Service;
using InteractHub.Tests.Common;

namespace InteractHub.Tests.Unit.Services;

public class HashtagServiceTests
{
    [Fact]
    public async Task GetByNameAsync_ShouldReturnHashtag_WhenExists()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new HashtagService(context);
        await service.CreateAsync(new Hashtag { Name = "dotnet" });

        // Act
        var hashtag = await service.GetByNameAsync("dotnet");

        // Assert
        Assert.NotNull(hashtag);
        Assert.Equal("dotnet", hashtag!.Name);
    }

    [Fact]
    public async Task GetByNameAsync_ShouldReturnNull_WhenHashtagNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new HashtagService(context);

        // Act
        var hashtag = await service.GetByNameAsync("nonexistent");

        // Assert
        Assert.Null(hashtag);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenHashtagNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new HashtagService(context);

        // Act
        var deleted = await service.DeleteAsync(777);

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateHashtag_WithValidData()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new HashtagService(context);
        var hashtag = new Hashtag { Name = "csharp" };

        // Act
        var created = await service.CreateAsync(hashtag);

        // Assert
        Assert.NotNull(created);
        Assert.NotEqual(0, created.Id);
    }
}
