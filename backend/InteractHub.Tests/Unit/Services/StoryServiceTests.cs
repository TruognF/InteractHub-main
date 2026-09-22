using InteractHub.Application.Entities;
using InteractHub.Infrastructure.Service;
using InteractHub.Tests.Common;

namespace InteractHub.Tests.Unit.Services;

public class StoryServiceTests
{
    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnOnlyUserStories()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var user1 = new User { Id = "u1", UserName = "user1", Email = "user1@example.com", FullName = "User One" };
        var user2 = new User { Id = "u2", UserName = "user2", Email = "user2@example.com", FullName = "User Two" };
        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();
        
        var service = new StoryService(context);
        var expireDate = DateTime.UtcNow.AddDays(1);
        await service.CreateAsync(new Story { UserId = "u1", Content = "s1", Id = 1, ExpireAt = expireDate, CreatedAt = DateTime.UtcNow });
        await service.CreateAsync(new Story { UserId = "u1", Content = "s2", Id = 2, ExpireAt = expireDate, CreatedAt = DateTime.UtcNow });
        await service.CreateAsync(new Story { UserId = "u2", Content = "s3", Id = 3, ExpireAt = expireDate, CreatedAt = DateTime.UtcNow });

        // Act
        var stories = await service.GetByUserIdAsync("u1");

        // Assert
        Assert.Equal(2, stories.Count);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenStoryNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new StoryService(context);

        // Act
        var deleted = await service.DeleteAsync(123);

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateStory_WithValidData()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new StoryService(context);
        var story = new Story 
        { 
            UserId = "u1", 
            Content = "test story", 
            ExpireAt = DateTime.UtcNow.AddDays(1),
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var created = await service.CreateAsync(story);

        // Assert
        Assert.NotNull(created);
        Assert.NotEqual(0, created.Id);
    }
}

