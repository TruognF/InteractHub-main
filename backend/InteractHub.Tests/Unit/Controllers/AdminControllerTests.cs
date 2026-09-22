using System.Security.Claims;
using InteractHub.API.Controllers;
using InteractHub.API.DTOs;
using InteractHub.Application.Constants;
using InteractHub.Application.Entities;
using InteractHub.Tests.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace InteractHub.Tests.Unit.Controllers;

public class AdminControllerTests
{
    // GetAllUsersWithRoles tests - trả về tất cả users với roles
    [Fact]
    public async Task GetAllUsersWithRoles_ShouldReturnAllUsersWithRoles_WhenUsersExist()
    {
        // Arrange
        var users = new List<User>
        {
            new User { Id = "u1", UserName = "user1", FullName = "User One", Email = "user1@mail.com" },
            new User { Id = "u2", UserName = "user2", FullName = "User Two", Email = "user2@mail.com" }
        };
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.Users).Returns(users.AsQueryable());
        userManagerMock.Setup(m => m.GetRolesAsync(users[0])).ReturnsAsync(new List<string> { RoleConstants.User });
        userManagerMock.Setup(m => m.GetRolesAsync(users[1])).ReturnsAsync(new List<string> { RoleConstants.User });

        var roleManagerMock = IdentityMockFactory.CreateRoleManagerMock();
        var controller = new AdminController(userManagerMock.Object, roleManagerMock.Object);
        ControllerTestHelper.SetUser(controller, "admin");

        // Act
        var result = await controller.GetAllUsersWithRoles();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    // GetAllUsersWithRoles tests - trả về danh sách rỗng khi không có users
    [Fact]
    public async Task GetAllUsersWithRoles_ShouldReturnEmpty_WhenNoUsersExist()
    {
        // Arrange
        var users = new List<User>();
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.Users).Returns(users.AsQueryable());

        var roleManagerMock = IdentityMockFactory.CreateRoleManagerMock();
        var controller = new AdminController(userManagerMock.Object, roleManagerMock.Object);
        ControllerTestHelper.SetUser(controller, "admin");

        // Act
        var result = await controller.GetAllUsersWithRoles();

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    // AssignRoleToUser tests - gán role thành công
    [Fact]
    public async Task AssignRoleToUser_ShouldReturnSuccess_WhenInputIsValid()
    {
        // Arrange
        var user = new User { Id = "u1", UserName = "test-user", FullName = "Test User", Email = "test@mail.com" };
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        userManagerMock.Setup(m => m.IsInRoleAsync(user, RoleConstants.User)).ReturnsAsync(false);
        userManagerMock
            .Setup(m => m.AddToRoleAsync(user, RoleConstants.User))
            .ReturnsAsync(IdentityResult.Success);

        var roleManagerMock = IdentityMockFactory.CreateRoleManagerMock();
        roleManagerMock.Setup(m => m.RoleExistsAsync(RoleConstants.User)).ReturnsAsync(true);

        var controller = new AdminController(userManagerMock.Object, roleManagerMock.Object);
        ControllerTestHelper.SetUser(controller, "admin");

        // Act
        var result = await controller.AssignRoleToUser(new AssignRoleDto { UserId = "u1", Role = RoleConstants.User });

        // Assert
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
    }

    // AssignRoleToUser tests - trả về 404 khi user không tồn tại
    [Fact]
    public async Task AssignRoleToUser_ShouldReturnNotFound_WhenUserMissing()
    {
        // Arrange
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((User?)null);
        var roleManagerMock = IdentityMockFactory.CreateRoleManagerMock();
        var controller = new AdminController(userManagerMock.Object, roleManagerMock.Object);
        ControllerTestHelper.SetUser(controller, "admin");

        // when
        var result = await controller.AssignRoleToUser(new AssignRoleDto { UserId = "missing", Role = "User" });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    // AssignRoleToUser tests - trả về 400 khi role không tồn tại
    [Fact]
    public async Task AssignRoleToUser_ShouldReturnBadRequest_WhenRoleDoesNotExist()
    {
        // given
        var user = new User { Id = "u1", UserName = "test-user", FullName = "Test User", Email = "test@mail.com" };
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);

        var roleManagerMock = IdentityMockFactory.CreateRoleManagerMock();
        roleManagerMock.Setup(m => m.RoleExistsAsync("NonExistentRole")).ReturnsAsync(false);

        var controller = new AdminController(userManagerMock.Object, roleManagerMock.Object);
        ControllerTestHelper.SetUser(controller, "admin");

        // when
        var result = await controller.AssignRoleToUser(new AssignRoleDto { UserId = "u1", Role = "NonExistentRole" });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // AssignRoleToUser tests - trả về 400 khi user đã có role này
    [Fact]
    public async Task AssignRoleToUser_ShouldReturnBadRequest_WhenUserAlreadyHasRole()
    {
        // given
        var user = new User { Id = "u1", UserName = "test-user", FullName = "Test User", Email = "test@mail.com" };
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        userManagerMock.Setup(m => m.IsInRoleAsync(user, RoleConstants.User)).ReturnsAsync(true);

        var roleManagerMock = IdentityMockFactory.CreateRoleManagerMock();
        roleManagerMock.Setup(m => m.RoleExistsAsync(RoleConstants.User)).ReturnsAsync(true);

        var controller = new AdminController(userManagerMock.Object, roleManagerMock.Object);
        ControllerTestHelper.SetUser(controller, "admin");

        // when
        var result = await controller.AssignRoleToUser(new AssignRoleDto { UserId = "u1", Role = RoleConstants.User });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // RemoveRoleFromUser tests - xóa role thành công
    [Fact]
    public async Task RemoveRoleFromUser_ShouldReturnSuccess_WhenInputIsValid()
    {
        // given
        var user = new User { Id = "u1", UserName = "test-user", FullName = "Test User", Email = "test@mail.com" };
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        userManagerMock.Setup(m => m.IsInRoleAsync(user, RoleConstants.User)).ReturnsAsync(true);
        userManagerMock.Setup(m => m.RemoveFromRoleAsync(user, RoleConstants.User)).ReturnsAsync(IdentityResult.Success);

        var roleManagerMock = IdentityMockFactory.CreateRoleManagerMock();
        var controller = new AdminController(userManagerMock.Object, roleManagerMock.Object);
        ControllerTestHelper.SetUser(controller, "admin");

        // when
        var result = await controller.RemoveRoleFromUser(new RemoveRoleDto { UserId = "u1", Role = RoleConstants.User });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
    }

    // RemoveRoleFromUser tests - trả về 404 khi user không tồn tại
    [Fact]
    public async Task RemoveRoleFromUser_ShouldReturnNotFound_WhenUserMissing()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByIdAsync("missing")).ReturnsAsync((User?)null);

        var roleManagerMock = IdentityMockFactory.CreateRoleManagerMock();
        var controller = new AdminController(userManagerMock.Object, roleManagerMock.Object);
        ControllerTestHelper.SetUser(controller, "admin");

        // when
        var result = await controller.RemoveRoleFromUser(new RemoveRoleDto { UserId = "missing", Role = RoleConstants.User });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    // RemoveRoleFromUser tests - trả về 400 khi user không có role này
    [Fact]
    public async Task RemoveRoleFromUser_ShouldReturnBadRequest_WhenUserDoesNotHaveRole()
    {
        // given
        var user = new User { Id = "u1", UserName = "test-user", FullName = "Test User", Email = "test@mail.com" };
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        userManagerMock.Setup(m => m.IsInRoleAsync(user, RoleConstants.User)).ReturnsAsync(false);

        var roleManagerMock = IdentityMockFactory.CreateRoleManagerMock();
        var controller = new AdminController(userManagerMock.Object, roleManagerMock.Object);
        ControllerTestHelper.SetUser(controller, "admin");

        // when
        var result = await controller.RemoveRoleFromUser(new RemoveRoleDto { UserId = "u1", Role = RoleConstants.User });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // RemoveRoleFromUser tests - trả về 400 khi cố xóa admin role từ chính mình
    [Fact]
    public async Task RemoveRoleFromUser_ShouldReturnBadRequest_WhenRemovingOwnAdminRole()
    {
        // given
        var user = new User { Id = "admin-1", UserName = "admin", FullName = "Admin", Email = "admin@mail.com" };
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByIdAsync("admin-1")).ReturnsAsync(user);
        userManagerMock.Setup(m => m.IsInRoleAsync(user, RoleConstants.Admin)).ReturnsAsync(true);
        var roleManagerMock = IdentityMockFactory.CreateRoleManagerMock();
        var controller = new AdminController(userManagerMock.Object, roleManagerMock.Object);
        ControllerTestHelper.SetUser(controller, "admin-1");

        // when
        var result = await controller.RemoveRoleFromUser(new RemoveRoleDto { UserId = "admin-1", Role = RoleConstants.Admin });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // GetUserWithRoles tests - trả về user với roles
    [Fact]
    public async Task GetUserWithRoles_ShouldReturnUserWithRoles_WhenUserExists()
    {
        // given
        var user = new User { Id = "u1", UserName = "test-user", FullName = "Test User", Email = "test@mail.com" };
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { RoleConstants.User });

        var roleManagerMock = IdentityMockFactory.CreateRoleManagerMock();
        var controller = new AdminController(userManagerMock.Object, roleManagerMock.Object);
        ControllerTestHelper.SetUser(controller, "admin");

        // when
        var result = await controller.GetUserWithRoles("u1");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    // GetUserWithRoles tests - trả về 404 khi user không tồn tại
    [Fact]
    public async Task GetUserWithRoles_ShouldReturnNotFound_WhenUserMissing()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByIdAsync("u999")).ReturnsAsync((User?)null);

        var roleManagerMock = IdentityMockFactory.CreateRoleManagerMock();
        var controller = new AdminController(userManagerMock.Object, roleManagerMock.Object);
        ControllerTestHelper.SetUser(controller, "admin");

        // when
        var result = await controller.GetUserWithRoles("u999");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    // DeleteUser tests - xóa user thành công
    [Fact]
    public async Task DeleteUser_ShouldReturnSuccess_WhenUserExists()
    {
        // given
        var user = new User { Id = "u1", UserName = "test-user", FullName = "Test User", Email = "test@mail.com" };
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByIdAsync("u1")).ReturnsAsync(user);
        userManagerMock.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);

        var roleManagerMock = IdentityMockFactory.CreateRoleManagerMock();
        var controller = new AdminController(userManagerMock.Object, roleManagerMock.Object);
        ControllerTestHelper.SetUser(controller, "admin2");

        // when
        var result = await controller.DeleteUser("u1");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
    }

    // DeleteUser tests - trả về 404 khi user không tồn tại
    [Fact]
    public async Task DeleteUser_ShouldReturnNotFound_WhenUserMissing()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByIdAsync("u999")).ReturnsAsync((User?)null);

        var roleManagerMock = IdentityMockFactory.CreateRoleManagerMock();
        var controller = new AdminController(userManagerMock.Object, roleManagerMock.Object);
        ControllerTestHelper.SetUser(controller, "admin");

        // when
        var result = await controller.DeleteUser("u999");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(404, objectResult.StatusCode);
    }

    // DeleteUser tests - trả về 400 khi cố xóa chính mình
    [Fact]
    public async Task DeleteUser_ShouldReturnBadRequest_WhenDeletingOwnAccount()
    {
        // given
        var user = new User { Id = "admin", UserName = "admin-user", FullName = "Admin", Email = "admin@mail.com" };
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByIdAsync("admin")).ReturnsAsync(user);

        var roleManagerMock = IdentityMockFactory.CreateRoleManagerMock();
        var controller = new AdminController(userManagerMock.Object, roleManagerMock.Object);
        ControllerTestHelper.SetUser(controller, "admin");

        // when
        var result = await controller.DeleteUser("admin");

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }
}
