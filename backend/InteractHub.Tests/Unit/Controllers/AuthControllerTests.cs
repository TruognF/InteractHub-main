using InteractHub.API.Controllers;
using InteractHub.API.DTOs;
using InteractHub.Application.Constants;
using InteractHub.Application.Entities;
using InteractHub.Tests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;

namespace InteractHub.Tests.Unit.Controllers;

public class AuthControllerTests
{
    // Register tests - kiểm tra định dạng email
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenEmailFormatInvalid()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Register(new RegisterDto
        {
            UserName = "valid_name",
            Email = "invalid-email",
            Password = "Aa123456!",
            FullName = "Test User"
        });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // Register tests - kiểm tra email rỗng
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenEmailIsEmpty()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Register(new RegisterDto
        {
            UserName = "valid_name",
            Email = "",
            Password = "Aa123456!",
            FullName = "Test User"
        });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // Register tests - kiểm tra username quá ngắn
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenUsernameTooShort()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Register(new RegisterDto
        {
            UserName = "ab",
            Email = "test@mail.com",
            Password = "Aa123456!",
            FullName = "Test User"
        });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // Register tests - kiểm tra username quá dài
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenUsernameTooLong()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Register(new RegisterDto
        {
            UserName = "this_is_a_very_long_username_that_exceeds_limit",
            Email = "test@mail.com",
            Password = "Aa123456!",
            FullName = "Test User"
        });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // Register tests - kiểm tra username chứa ký tự không hợp lệ
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenUsernameHasInvalidCharacters()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Register(new RegisterDto
        {
            UserName = "invalid-name!",
            Email = "test@mail.com",
            Password = "Aa123456!",
            FullName = "Test User"
        });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // Register tests - kiểm tra fullname quá ngắn
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenFullNameTooShort()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Register(new RegisterDto
        {
            UserName = "valid_name",
            Email = "test@mail.com",
            Password = "Aa123456!",
            FullName = "A"
        });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // Register tests - kiểm tra fullname quá dài
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenFullNameTooLong()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());
        var longName = new string('A', 101);

        // when
        var result = await controller.Register(new RegisterDto
        {
            UserName = "valid_name",
            Email = "test@mail.com",
            Password = "Aa123456!",
            FullName = longName
        });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // Register tests - kiểm tra password quá ngắn
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordTooShort()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Register(new RegisterDto
        {
            UserName = "valid_name",
            Email = "test@mail.com",
            Password = "Short1!",
            FullName = "Test User"
        });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // Register tests - kiểm tra password thiếu chữ hoa
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordMissingUppercase()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Register(new RegisterDto
        {
            UserName = "valid_name",
            Email = "test@mail.com",
            Password = "lowercase1!",
            FullName = "Test User"
        });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // Register tests - kiểm tra password thiếu chữ thường
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordMissingLowercase()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Register(new RegisterDto
        {
            UserName = "valid_name",
            Email = "test@mail.com",
            Password = "UPPERCASE1!",
            FullName = "Test User"
        });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // Register tests - kiểm tra password thiếu số
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordMissingNumber()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Register(new RegisterDto
        {
            UserName = "valid_name",
            Email = "test@mail.com",
            Password = "NoNumber!",
            FullName = "Test User"
        });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // Register tests - kiểm tra password thiếu ký tự đặc biệt
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenPasswordMissingSpecialChar()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Register(new RegisterDto
        {
            UserName = "valid_name",
            Email = "test@mail.com",
            Password = "NoSpecial123",
            FullName = "Test User"
        });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // Register tests - kiểm tra username đã tồn tại
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenUsernameAlreadyExists()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByNameAsync("taken_name"))
            .ReturnsAsync(new User { Id = "u1", UserName = "taken_name", Email = "taken@mail.com", FullName = "Taken" });
        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Register(new RegisterDto
        {
            UserName = "taken_name",
            Email = "new@mail.com",
            Password = "Aa123456!",
            FullName = "New User"
        });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // Register tests - kiểm tra email đã tồn tại
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenEmailAlreadyExists()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByNameAsync("new_name")).ReturnsAsync((User?)null);
        userManagerMock.Setup(m => m.FindByEmailAsync("taken@mail.com"))
            .ReturnsAsync(new User { Id = "u1", UserName = "old_user", Email = "taken@mail.com", FullName = "Old User" });
        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Register(new RegisterDto
        {
            UserName = "new_name",
            Email = "taken@mail.com",
            Password = "Aa123456!",
            FullName = "New User"
        });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // Register tests - đăng ký thành công
    [Fact]
    public async Task Register_ShouldReturnCreated_WhenInputIsValid()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByNameAsync("new_user")).ReturnsAsync((User?)null);
        userManagerMock.Setup(m => m.FindByEmailAsync("new@mail.com")).ReturnsAsync((User?)null);
        userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), "Aa123456!"))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), RoleConstants.User))
            .ReturnsAsync(IdentityResult.Success);
        userManagerMock
            .Setup(m => m.GetRolesAsync(It.IsAny<User>()))
            .ReturnsAsync(new List<string> { RoleConstants.User });

        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Register(new RegisterDto
        {
            UserName = "new_user",
            Email = "new@mail.com",
            Password = "Aa123456!",
            FullName = "New User"
        });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    // Register tests - tạo user thất bại
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenCreateUserFails()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByNameAsync("new_user")).ReturnsAsync((User?)null);
        userManagerMock.Setup(m => m.FindByEmailAsync("new@mail.com")).ReturnsAsync((User?)null);
        var error = new IdentityError { Description = "Creation failed" };
        userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), "Aa123456!"))
            .ReturnsAsync(IdentityResult.Failed(error));

        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Register(new RegisterDto
        {
            UserName = "new_user",
            Email = "new@mail.com",
            Password = "Aa123456!",
            FullName = "New User"
        });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // Register tests - gán role thất bại
    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenAddRoleFails()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByNameAsync("new_user")).ReturnsAsync((User?)null);
        userManagerMock.Setup(m => m.FindByEmailAsync("new@mail.com")).ReturnsAsync((User?)null);
        userManagerMock
            .Setup(m => m.CreateAsync(It.IsAny<User>(), "Aa123456!"))
            .ReturnsAsync(IdentityResult.Success);
        var roleError = new IdentityError { Description = "Role assignment failed" };
        userManagerMock
            .Setup(m => m.AddToRoleAsync(It.IsAny<User>(), RoleConstants.User))
            .ReturnsAsync(IdentityResult.Failed(roleError));
        userManagerMock
            .Setup(m => m.DeleteAsync(It.IsAny<User>()))
            .ReturnsAsync(IdentityResult.Success);

        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Register(new RegisterDto
        {
            UserName = "new_user",
            Email = "new@mail.com",
            Password = "Aa123456!",
            FullName = "New User"
        });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(400, objectResult.StatusCode);
    }

    // Login tests - user không tồn tại
    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenUserNotFound()
    {
        // given
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByNameAsync("missing")).ReturnsAsync((User?)null);
        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Login(new LoginDto { UserName = "missing", Password = "password" });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(401, objectResult.StatusCode);
    }

    // Login tests - password không đúng
    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenPasswordIsInvalid()
    {
        // given
        var user = new User { Id = "u1", UserName = "john", Email = "john@mail.com", FullName = "John" };
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByNameAsync("john")).ReturnsAsync(user);
        userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { RoleConstants.User });
        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        signInManagerMock
            .Setup(m => m.CheckPasswordSignInAsync(user, "bad-pass", false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Login(new LoginDto { UserName = "john", Password = "bad-pass" });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(401, objectResult.StatusCode);
    }

    // Login tests - đăng nhập thành công với token
    [Fact]
    public async Task Login_ShouldReturnOk_WhenCredentialsAreValid()
    {
        // given
        var user = new User { Id = "u1", UserName = "john", Email = "john@mail.com", FullName = "John Doe" };
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByNameAsync("john")).ReturnsAsync(user);
        userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { RoleConstants.User });
        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        signInManagerMock
            .Setup(m => m.CheckPasswordSignInAsync(user, "ValidPass123!", false))
            .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Login(new LoginDto { UserName = "john", Password = "ValidPass123!" });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(200, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);
    }

    // Login tests - ngăn Admin đăng nhập qua endpoint này
    [Fact]
    public async Task Login_ShouldReturnUnauthorized_WhenUserIsAdmin()
    {
        // given
        var user = new User { Id = "u1", UserName = "admin", Email = "admin@mail.com", FullName = "Admin User" };
        var userManagerMock = IdentityMockFactory.CreateUserManagerMock();
        userManagerMock.Setup(m => m.FindByNameAsync("admin")).ReturnsAsync(user);
        userManagerMock.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { RoleConstants.Admin, RoleConstants.User });
        var signInManagerMock = IdentityMockFactory.CreateSignInManagerMock(userManagerMock.Object);
        var controller = new AuthController(userManagerMock.Object, signInManagerMock.Object, BuildJwtConfiguration());

        // when
        var result = await controller.Login(new LoginDto { UserName = "admin", Password = "AdminPass123!" });

        // then
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(401, objectResult.StatusCode);
    }

    private static IConfiguration BuildJwtConfiguration()
    {
        var settings = new Dictionary<string, string?>
        {
            ["JWT:SecretKey"] = "this-is-a-test-secret-key-with-minimum-length",
            ["JWT:Issuer"] = "test-issuer",
            ["JWT:Audience"] = "test-audience",
            ["JWT:ExpirationMinutes"] = "60"
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();
    }
}
