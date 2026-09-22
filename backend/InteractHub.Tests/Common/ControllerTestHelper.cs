using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace InteractHub.Tests.Common;

internal static class ControllerTestHelper
{
    /*
    File ControllerTestHelper cung cấp các công cụ để fake
    thông tin người dùng đang truy cập, 
    giúp Controller hoạt động trơn tru trong lúc test 
    mà không cần phải bật server thật lên.
    */

    // Giả lập trạng thái người dùng ẩn danh (chưa đăng nhập hệ thống)
    public static void SetAnonymous(ControllerBase controller)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
    }

    // Giả lập trạng thái người dùng đã đăng nhập dựa vào Id
    public static void SetUser(ControllerBase controller, string userId)
    {
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, userId) },
                    "test"))
            }
        };
    }
}
