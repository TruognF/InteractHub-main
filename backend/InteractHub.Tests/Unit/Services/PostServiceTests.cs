using InteractHub.Application.Entities;
using InteractHub.Infrastructure.Service;
using InteractHub.Tests.Common;

namespace InteractHub.Tests.Unit.Services;

public class PostServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldPersistPost_WithValidData()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new PostService(context);
        var post = new Post 
        { 
            Content = "first post", 
            UserId = "u1",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var created = await service.CreateAsync(post);

        // Assert
        Assert.NotEqual(0, created.Id);
        Assert.Single(context.Posts);
        Assert.Equal("first post", created.Content);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenPostNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new PostService(context);

        // Act
        var result = await service.GetByIdAsync(12345);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnPost_WhenPostExists()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var user = new User { Id = "u1", UserName = "testuser", Email = "test@example.com", FullName = "Test User" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        
        var service = new PostService(context);
        var createdPost = await service.CreateAsync(new Post 
        { 
            Content = "test post", 
            UserId = "u1",
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var result = await service.GetByIdAsync(createdPost.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test post", result.Content);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllPosts_WithCorrectCount()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var user1 = new User { Id = "u1", UserName = "user1", Email = "user1@example.com", FullName = "User One" };
        var user2 = new User { Id = "u2", UserName = "user2", Email = "user2@example.com", FullName = "User Two" };
        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();
        
        var service = new PostService(context);
        await service.CreateAsync(new Post { Content = "p1", Id = 1, UserId = "u1", CreatedAt = DateTime.UtcNow });
        await service.CreateAsync(new Post { Content = "p2", Id = 2, UserId = "u2", CreatedAt = DateTime.UtcNow });

        // Act
        var posts = await service.GetAllAsync();

        // Assert
        Assert.Equal(2, posts.Count);
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoPosts()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new PostService(context);

        // Act
        var posts = await service.GetAllAsync();

        // Assert
        Assert.Empty(posts);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenPostDoesNotExist()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new PostService(context);

        // Act
        var result = await service.DeleteAsync(55);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemovePost_WhenPostExists()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new PostService(context);
        var post = await service.CreateAsync(new Post 
        { 
            Content = "delete me", 
            UserId = "u1",
            CreatedAt = DateTime.UtcNow
        });

        // Act
        var deleted = await service.DeleteAsync(post.Id);

        // Assert
        Assert.True(deleted);
        var fetched = await service.GetByIdAsync(post.Id);
        Assert.Null(fetched);
    }

    [Fact]
    public async Task GetFeedAsync_ShouldMaskSharedPostContent_WhenOriginalPostIsDeleted()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var user1 = new User { Id = "u1", UserName = "user1", Email = "user1@example.com", FullName = "User One" };
        var user2 = new User { Id = "u2", UserName = "user2", Email = "user2@example.com", FullName = "User Two" };
        context.Users.AddRange(user1, user2);
        await context.SaveChangesAsync();

        var service = new PostService(context);
        var originalPost = await service.CreateAsync(new Post
        {
            Content = "Sensitive original content",
            UserId = "u1",
            CreatedAt = DateTime.UtcNow
        });

        var sharedPost = await service.CreateAsync(new Post
        {
            Content = "Sharing this post",
            UserId = "u2",
            SharedPostId = originalPost.Id,
            CreatedAt = DateTime.UtcNow.AddMinutes(5)
        });

        // Delete original post
        await service.DeleteAsync(originalPost.Id);

        // Act
        var (posts, totalCount) = await service.GetFeedAsync(1, 10);

        // Assert
        Assert.Single(posts); // Only sharedPost is returned (originalPost is soft-deleted)
        var feedPost = posts[0];
        Assert.NotNull(feedPost.SharedPost);
        Assert.True(feedPost.SharedPost.IsDeleted);
        Assert.NotEqual("Sensitive original content", feedPost.SharedPost.Content);
        Assert.Equal("Nội dung không khả dụng", feedPost.SharedPost.UserFullName);
    }
}

