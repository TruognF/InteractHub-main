using InteractHub.Application.Entities;
using InteractHub.Infrastructure.Service;
using InteractHub.Tests.Common;

namespace InteractHub.Tests.Unit.Services;

public class UserServiceTests
{
    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new UserService(context);
        var user = new User 
        { 
            Id = "u1", 
            UserName = "u1", 
            Email = "u1@mail.com", 
            FullName = "User One",
            NormalizedEmail = "U1@MAIL.COM",
            NormalizedUserName = "U1"
        };
        await service.CreateAsync(user);

        // Act
        var result = await service.GetByEmailAsync("u1@mail.com");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("u1", result!.Id);
        Assert.Equal("User One", result.FullName);
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNull_WhenUserNotFound()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new UserService(context);

        // Act
        var result = await service.GetByEmailAsync("nonexistent@mail.com");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateUser_WithValidData()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new UserService(context);
        var user = new User 
        { 
            Id = "u1", 
            UserName = "testuser", 
            Email = "test@mail.com", 
            FullName = "Test User",
            NormalizedEmail = "TEST@MAIL.COM",
            NormalizedUserName = "TESTUSER"
        };

        // Act
        var created = await service.CreateAsync(user);

        // Assert
        Assert.NotNull(created);
        Assert.Equal("u1", created.Id);
        Assert.Single(context.Users);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnFalse_WhenUserIsNull()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new UserService(context);

        // Act
        var deleted = await service.DeleteAsync(null!);

        // Assert
        Assert.False(deleted);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveUser_WhenUserExists()
    {
        // Arrange
        using var context = TestDbContextFactory.Create();
        var service = new UserService(context);
        var user = new User 
        { 
            Id = "u1", 
            UserName = "testuser", 
            Email = "test@mail.com", 
            FullName = "Test User",
            NormalizedEmail = "TEST@MAIL.COM",
            NormalizedUserName = "TESTUSER"
        };
        await service.CreateAsync(user);

        // Act
        var deleted = await service.DeleteAsync(user);

        // Assert
        Assert.True(deleted);
        Assert.Empty(context.Users);
    }
}
