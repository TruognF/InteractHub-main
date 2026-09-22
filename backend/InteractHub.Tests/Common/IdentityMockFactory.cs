using InteractHub.Application.Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace InteractHub.Tests.Common;

// Factory tạo các đối tượng Mock (giả mạo) cho ASP.NET Core Identity giúp viết Unit Test nhanh hơn.
internal static class IdentityMockFactory
{

    // Tạo Mock UserManager để test các tính năng thao tác dữ liệu người dùng (tạo, xóa, đổi mật khẩu...).
    public static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<User>>();
        return new Mock<UserManager<User>>(
            store.Object,
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<IPasswordHasher<User>>(),
            new List<IUserValidator<User>>(),
            new List<IPasswordValidator<User>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<IServiceProvider>(),
            Mock.Of<ILogger<UserManager<User>>>());
    }

    // Tạo Mock SignInManager để test các tính năng đăng nhập/đăng xuất mà không cần HTTP Context thật.
    public static Mock<SignInManager<User>> CreateSignInManagerMock(UserManager<User> userManager)
    {
        return new Mock<SignInManager<User>>(
            userManager,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<User>>(),
            Mock.Of<IOptions<IdentityOptions>>(),
            Mock.Of<ILogger<SignInManager<User>>>(),
            Mock.Of<IAuthenticationSchemeProvider>(),
            Mock.Of<IUserConfirmation<User>>());
    }

    // Tạo Mock RoleManager để test các tính năng phân quyền người dùng (VD: thẻ Admin/User...).
    public static Mock<RoleManager<IdentityRole>> CreateRoleManagerMock()
    {
        return new Mock<RoleManager<IdentityRole>>(
            Mock.Of<IRoleStore<IdentityRole>>(),
            new List<IRoleValidator<IdentityRole>>(),
            Mock.Of<ILookupNormalizer>(),
            new IdentityErrorDescriber(),
            Mock.Of<ILogger<RoleManager<IdentityRole>>>());
    }
}
