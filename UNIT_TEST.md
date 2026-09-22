# 🧪 InteractHub Backend - Hướng Dẫn Kiểm Thử (Testing)

**Dự án:** InteractHub - Social Media Application  
**Ngôn ngữ:** C# 11 (.NET 9)  
**Công nghệ:** xUnit, Moq, EF Core In-Memory  
**Cập nhật:** May 03, 2026

---

## 📑 Mục Lục

1. [T1: Cơ Sở Lý Thuyết & Mẫu Thiết Kế](#t1-cơ-sở-lý-thuyết--mẫu-thiết-kế)
2. [T2: Cấu Trúc Mã Nguồn Kiểm Thử](#t2-cấu-trúc-mã-nguồn-kiểm-thử)
3. [T3: Code Examples](#t3-code-examples)
4. [T4: Thao Tác Kỹ Thuật (CLI)](#t4-thao-tác-kỹ-thuật-cli)
5. [T5: Bí Quyết & Best Practices](#t5-bí-quyết--best-practices)

---

# T1: Cơ Sở Lý Thuyết & Mẫu Thiết Kế

## 🎯 Ý Nghĩa

Tài liệu này được biên soạn theo chuẩn học thuật (academic standards), diễn giải chi tiết các khái niệm cốt lõi nhằm hỗ trợ kỹ sư mới tiếp cận dễ dàng với phương pháp kiểm thử phần mềm (Unit Testing). Tại InteractHub, hệ thống áp dụng kiến trúc **Clean Architecture**. Một trong những điểm mạnh của mô hình này là cho phép cô lập (isolate) các khối tính năng để kiểm thử độc lập mà không ảnh hưởng lẫn nhau.

## 🧩 Các Khái Niệm Quan Trọng Cần Nắm

- **xUnit Framework**: Công cụ chuẩn định hướng cho việc khởi chạy bài kiểm thử. Trong C#, việc đánh dấu thẻ phụ (attribute) `[Fact]` dùng để nhận diện một nhánh chạy (1 hàm thuần), còn `[Theory]` đi kèm `[InlineData]` nhận diện nhóm bài test có truyền tham số đầu vào.
- **SUT (System Under Test - Hệ thống đang được kiểm thử)**: Mọi bài test đều có nhân vật chính. Nếu chúng ta đang viết test cho module `UserService`, thì `UserService` chính là SUT. Tôn chỉ quan trọng: *Chỉ test phần logic lõi của SUT, mọi phụ kiện bao quanh phải được ảo hóa.*
- **Khái niệm Ảo hóa (Mocking & Stubs)**: Giả lập các "vật thế thân" cho các đối tượng ngoại vi (Dependencies). Ví dụ, nếu code của bạn gọi xuống dịch vụ gửi Email ngoài, ta sẽ sử dụng thư viện **Moq** để làm một bức tường khóa chặn việc gởi đi tốn kém ấy và tự lập trình sẵn kết quả để kiểm định hàm.
- **Cơ sở dữ liệu In-Memory (EF Core In-Memory)**: Thay vì lưu trữ thực tế vào bảng CSDL vật lý (SQL Server), ta giả lập các bảng điện toán đám mây ngay trên dung lượng **RAM** khi test chạy. Tránh việc để lại "rác dữ liệu" (data noise) và giúp hàng trăm bài test chạy tính bằng vi giây (microseconds).

---

## 📊 Mẫu Thiết Kế: AAA (Arrange - Act - Assert)

Học thuật quy định mọi hàm kiểm thử đều phải chia thành 3 pha độc lập rõ rệt để có tính tường minh cao nhất (Trong mã nguồn thường ký hiệu bằng comment: `// given`, `// when`, `// then`):

1. **Arrange (Chuẩn bị / given)**: Thiết lập cấu hình đầu vào, xây dựng SUT, xây dựng database In-memory và viết các định nghĩa hàm Mock (`Setup`).
2. **Act (Thực thi / when)**: Việc duy nhất ở bước này là "nhấn nút" gọi tới phương thức cần được test (Invoke).
3. **Assert (Xác minh / then)**: So sánh các giá trị kết quả được trả về từ *Hành vi (Act)* so với *Dự phóng lý thuyết ban đầu*. Nếu đúng thì qua (Pass), ngược lại là Rớt (Fail).

---

# T2: Cấu Trúc Mã Nguồn Kiểm Thử

Cấu trúc thư mục trong `InteractHub.Tests` được thiết kế ánh xạ (mapping) trực tiếp tới dự án thật:

## 📁 1. Tầng Setup & Dữ Liệu Tĩnh (Thư mục `Common/`)

Gồm các công cụ trợ lực rút ngắn pha **Arrange** (tránh mã lặp lại - DRY).

- `TestDbContextFactory.cs`: Nhà máy chế tạo cấu hình Database bộ nhớ ảo (In-Memory EF Core).
- `IdentityMockFactory.cs`: Lớp MockFactory làm nhiệm vụ gói (encapsulate) lại toàn bộ hệ thống gầm bệ của Identity, thả cho lập trình viên một Mock Entity cực đơn giản để test mà không cần dính tới các interface rườm rà.
- `ControllerTestHelper.cs`: Phục dựng lại môi trường HTTP/Context (Gắn Tokens nặc danh hoặc Gắn Users quyền hạn Role) để ép Controllers chịu ảnh hưởng của bảo mật.

## 📁 2. Tầng Nghiệp Vụ - Business Logic (Thư mục `Unit/Services/`)

- **Mạch tương đương:** Application Layer.
- **Lớp học thuật:** Sử dụng mô hình kiểm thử trạng thái dữ liệu (State-Based Testing).
- **Giải thích:** Ở đây, ta kiểm định hệ thống ra quyết định. Ví dụ tại `FriendshipServiceTests`, bài test sẽ bơm các Entity `Friendship` ảo. Khi kích hoạt hàm `AcceptFriendRequest`, hệ thống phải chứng minh biến trạng thái thay đổi từ `Pending` thành `Accepted` trực tiếp trên tầng Database (In-memory).

## 📁 3. Tầng Điều Hướng HTTP API (Thư mục `Unit/Controllers/`)

- **Mạch tương đương:** Presentation/API Layer.
- **Lớp học thuật:** Mô hình hành vi (Behavioral Testing).
- **Giải thích:** SUT là Interface HTTP (như `UsersController`). Chúng ta **Tuyệt Đối Không Sử Dụng Database In-Memory ở chốt chặn này**! Thay vào đó, dùng `Moq` để bắt chước dữ liệu trả ra cho `IUserService`. Phản ứng mong đợi là Controller phải ép các lệnh theo phân trang chuẩn và sinh ra nhãn chuẩn HTTP *(Trả về 200 OK, Json Output cho DTO, hay từ chối bằng 400 Bad Request, 403 Forbidden)*.

---

# T3: Code Examples

Dưới đây là một số ví dụ minh họa cách viết Unit Test theo đúng cấu trúc `Given - When - Then` được sử dụng trong dự án.

## 💻 1. Unit Test - Tầng Service (Kiểm tra logic)

Đoạn code kiểm tra luồng gửi yêu cầu kết bạn. Bài test sử dụng `In-Memory Database` để lưu dữ liệu và dùng `Moq` để giả lập việc gửi Notification.

```csharp
[Fact]
// Kiểm tra gửi yêu cầu thành công: Hệ thống tạo trạng thái Pending và gửi Noti cho bên B.
public async Task SendFriendRequestAsync_ShouldCreatePendingAndNotifyReceiver()
{
    // given (Arrange)
    using var context = TestDbContextFactory.Create();
    var notificationMock = new Mock<INotificationService>();
    notificationMock
        .Setup(n => n.NotifyFriendRequestAsync("u2", "u1"))
        .ReturnsAsync(new Notification { UserId = "u2", Type = NotificationType.FriendRequest });
        
    var service = new FriendshipService(context, notificationMock.Object);

    // when (Act)
    var friendship = await service.SendFriendRequestAsync("u1", "u2");

    // then (Assert)
    Assert.Equal(FriendshipStatus.Pending, friendship.Status);
    notificationMock.Verify(n => n.NotifyFriendRequestAsync("u2", "u1"), Times.Once);
}
```

## 💻 2. Unit Test - Tầng Controller (Kiểm tra HTTP Response)

Đoạn code kiểm tra hành vi của Controller khi gặp một thao tác lỗi (chấp nhận lời mời kết bạn không tồn tại). Bài test dùng `Moq` để bắt Service ném ra Exception, sau đó kiểm tra xem Controller có trả về đúng mã lỗi `400 Bad Request` hay không.

```csharp
[Fact]
// Kiểm tra Controller trả về 400 Bad Request khi Service ném lỗi InvalidOperationException
public async Task AcceptFriendRequest_ShouldReturnBadRequest_WhenInvalidOperation()
{
    // given
    var serviceMock = new Mock<IFriendshipService>();
    serviceMock
        .Setup(s => s.AcceptFriendRequestAsync(777))
        .ThrowsAsync(new InvalidOperationException("Invalid request"));

    var controller = new FriendshipsController(serviceMock.Object);
    ControllerTestHelper.SetAnonymousUser(controller);

    // when
    var result = await controller.AcceptFriendRequest(777);

    // then
    var badRequest = Assert.IsType<BadRequestObjectResult>(result);
    var response = Assert.IsType<ApiResponse>(badRequest.Value);
    
    Assert.False(response.Success);
    Assert.Equal(400, badRequest.StatusCode);
}
```

---

# T4: Thao Tác Kỹ Thuật (CLI)

## 🚀 1. Khởi động Bộ Kiểm Thử

Sử dụng công cụ command prompt / powershell được trỏ tới gốc (root) của dự án (`/InteractHub/`).

```powershell
# Chạy từ thư mục gốc
dotnet test backend/InteractHub.Tests/InteractHub.Tests.csproj

# Hoặc nếu đang ở thư mục backend
dotnet test
```

## 📈 2. Khảo sát Mật độ Bao Phủ Mã Dòng (Code Coverage)

Để đánh giá khoa học sự uy tín của bài luận, phần trăm Code Coverage cho biết tỷ lệ mã code dự án mà các bài kiểm thử đã lướt qua (scan) thành công.

```powershell
dotnet test backend/InteractHub.Tests/InteractHub.Tests.csproj --collect:"XPlat Code Coverage"
```

**Giải thích:** Kết quả định xuất bằng tệp XML mở (Cobertura) sẽ được sinh tại ngách thư mục: `backend/InteractHub.Tests/TestResults/**/coverage.cobertura.xml`

---

# T5: Bí Quyết & Best Practices

> **💡 CHÚ Ý: BÍ QUYẾT CHO CÁC KỸ SƯ (BEST PRACTICES)**
> 
> - **Đảm bảo tính cô độc (Isolation):** Mỗi bài kiểm thử (`[Fact]`) không được nương tựa vào bài nào khác. Dù chạy riêng hoặc chạy song song 10.000 test, kết quả vẫn phải không đổi.
> - **Bám chặt chuẩn hóa Đặt Tên:** Format Tên Method `TênHàmTest_PhảnỨngKỳVọng_TrongBốiCảnhNào` (VD: `SendFriendRequest_ShouldThrowException_WhenSenderEqualsReceiver`).
> - **Nguyên tắc "Một Trách Nhiệm":** Mỗi chức năng kiểm thử hạn chế việc "nhồi nhét", chỉ kiểm tra **DUY NHẤT LÝ DO CHÍNH** để không làm biến dạng hệ nguyên mẫu kiểm soát.
