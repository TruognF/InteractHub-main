# 🌱 Hướng Dẫn Seed Data cho InteractHub

## Tính Năng Seed Data

Hệ thống seed data sẽ tự động tạo:

- **5 người dùng** với các thông tin khác nhau
- **3 bài viết** cho mỗi người dùng (tổng cộng 15 bài viết)
- **3 nhóm** (Groups)
- **Phân bố ngẫu nhiên** người dùng vào các nhóm (tối thiểu 2 người mỗi nhóm)

## Thông Tin Tài Khoản Mặc Định

### Admin Account

- **Email**: `admin@interacthub.com`
  - **Mật khẩu**: `Admin@123456`

### Test Users

| Email                 | Mật khẩu    | Tên             |
| --------------------- | ----------- | --------------- |
| user1@interacthub.com | User@123456 | John Doe        |
| user2@interacthub.com | User@123456 | Jane Smith      |
| user3@interacthub.com | User@123456 | Michael Johnson |
| user4@interacthub.com | User@123456 | Sarah Williams  |
| user5@interacthub.com | User@123456 | David Brown     |

## Cách Sử Dụng

### Phương Pháp 1: Dùng Docker Compose (Khuyên Dùng)

```bash
# 1. Tạo và khởi động các container
docker-compose up --build

# 2. Ứng dụng sẽ tự động:
#    - Tạo/cập nhật database
#    - Chạy migrations
#    - Tạo roles (Admin, User, Moderator)
#    - Tạo admin user
#    - Seed test data (5 users, 3 posts mỗi user, 3 groups)
```

Nhật ký khởi động sẽ hiển thị:

```
✅ Role 'Admin' created successfully
✅ Role 'User' created successfully
✅ Admin user created with role 'Admin'
✅ User 'user1@interacthub.com' created successfully
✅ User 'user2@interacthub.com' created successfully
...
✅ Created 15 posts successfully
✅ Created 3 groups successfully
✅ Group 'Tech Enthusiasts' has 2 members
✅ Group 'Photography Lovers' has 2 members
✅ Group 'Fitness Community' has 1 members
```

### Phương Pháp 2: Chạy Trực Tiếp (Local Development)

```bash
cd backend
dotnet restore
dotnet build
dotnet run
```

## Dữ Liệu Được Tạo

### Người Dùng

- 5 test users được tạo với tên thật khác nhau
- Mỗi user có bio riêng
- Email đã được xác nhận

### Bài Viết

- 3 bài viết mỗi người dùng
- Nội dung bài viết đa dạng
- Thời gian tạo bài viết được phân bố trong quá khứ

### Nhóm

1. **Tech Enthusiasts** - Dành cho những người yêu thích công nghệ
2. **Photography Lovers** - Chia sẻ ảnh đẹp và tips chụp ảnh
3. **Fitness Community** - Hỗ trợ nhau trên hành trình fitness

### Phân Bố Thành Viên Trong Nhóm

- Mỗi nhóm có **tối thiểu 2 thành viên**
- Phân bố **ngẫu nhiên** nhưng đảm bảo yêu cầu tối thiểu
- Với 5 users, phân bố có thể là: 2-2-1 (nhưng Group 3 sẽ được điều chỉnh)

## Kiểm Tra Dữ Liệu

### Truy Cập Database

```bash
# Connection String trong docker-compose
# Server: localhost,1433
# User: sa
# Password: StrongPass123!
# Database: InteractHub (sẽ tự tạo)
```

### Truy Cập API

```bash
# Swagger UI
http://localhost:5000/swagger/index.html

# Login với tài khoản test
POST http://localhost:5000/api/auth/login
{
  "email": "user1@interacthub.com",
  "password": "User@123456"
}
```

## Tắt/Bật Seed Data

### Tắt Seed Data Tự Động

Nếu không muốn tự động seed data mỗi lần khởi động, hãy comment dòng trong `Program.cs`:

```csharp
// 🌱 Seed Test Data (5 users, 3 posts each, 3 groups)
// await DbInitializer.SeedTestData(services);
```

### Bật Lại Seed Data

Uncomment dòng trên để bật lại.

## Xóa Dữ Liệu Test

Nếu muốn xóa tất cả dữ liệu và bắt đầu lại:

### Với Docker

```bash
# 1. Dừng containers
docker-compose down

# 2. Xóa volume chứa database
docker volume rm interacthub_sql_data

# 3. Khởi động lại
docker-compose up --build
```

### Local Development

```bash
# 1. Xóa database trong SQL Server Management Studio
# hoặc dùng sqlcmd:
sqlcmd -S localhost -U sa -P "StrongPass123!" -Q "DROP DATABASE InteractHub"

# 2. Chạy lại
dotnet run
```

## Ghi Chú Quan Trọng

⚠️ **LƯỚI ý:**

- Seed data chỉ tạo nếu database trống
- Nếu data đã tồn tại, nó sẽ **skip** các bước seed
- Roles luôn được tạo nếu chưa tồn tại

✅ **Lợi ích:**

- Có dữ liệu test sẵn để phát triển
- Có thể test chức năng Groups, Posts, Comments...
- Tài khoản test login ngay được

🔧 **Tùy Chỉnh:**

- Muốn thêm/sửa test data? Edit `DbInitializer.SeedTestData()` method
- Thay đổi usernames, posts content, group names...

## Troubleshooting

### Lỗi: "Cannot connect to database"

```bash
# Kiểm tra SQL Server container có chạy không
docker ps

# Kiểm tra logs
docker logs interacthub-db
```

### Lỗi: "Migration failed"

```bash
# 1. Kiểm tra connection string trong appsettings.json
# 2. Đảm bảo SQL Server đã sẵn sàng (chờ 10-15 giây)
# 3. Clear migrations nếu bị lỗi
```

### Data không được tạo

- Kiểm tra console output có lỗi không
- Đảm bảo `await DbInitializer.SeedTestData(services);` không bị comment
- Kiểm tra database role/permission

## Kế Tiếp

Sau khi seed data thành công, bạn có thể:

1. ✅ Login với test users
2. ✅ Tạo bài viết mới
3. ✅ Tham gia/Tạo groups
4. ✅ Test messaging, notifications...
5. ✅ Develop và test features

Chúc bạn phát triển vui vẻ! 🚀
