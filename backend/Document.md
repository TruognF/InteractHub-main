### BACKEND

## Structure

1. API (Presentation Layer)
   - Nhận request từ frontend
   - validate dữ liệu đầu vào
   - Gọi service
   - trả JSON

2. Application (BUS)
   - Xử lý logic
   - Quyết định
   - Gọi repository/DB
   - ví dụ:
     """
     public async Task addFriend(string userId, string FriendId){

     if(userId == FriendId){
     throw new Exception("Người dùng không thể tự kết bạn");
     }

     // logic khác
     }
     """

3. Infrastructure (DAO)
   - Làm việc với database
   - EF core
   - Dapper
   - File storage (Azure)

# Flow hoạt động

    Frontend (React)
            ↓
    API (Controller)
            ↓
    Application (Service)
            ↓
    Infrastructure (EF / DB)
            ↓
    Database

## Các thư viện cần cài đặt trước

# API

    dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 9.0.0
    dotnet add package Swashbuckle.AspNetCore --version 9.0.0
    dotnet add package FluentValidation.AspNetCore --version 9.0.0

# Application

    dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection --version 9.0.0

# Infrastructure

    dotnet add package Microsoft.EntityFrameworkCore --version 9.0.0
    dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 9.0.0
    dotnet add package Microsoft.EntityFrameworkCore.Tools --version 9.0.0
    dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore --version 9.0.0

    dotnet add package Dapper (optional) --version 9.0.0

# Tests

    dotnet new xunit -n InteractHub.Tests
    dotnet sln add InteractHub.Tests
    cd InteractHub.Tests

    dotnet add package xunit
    dotnet add package Moq

# Liên kết project

    - chạy ở root

    dotnet add InteractHub.API reference InteractHub.Application
    dotnet add InteractHub.Application reference InteractHub.Infrastructure

## Tạo các Entities

# ở trong thư mục InteractHub.Infrasturcture/Entities

1. Tạo các Entites
   - User : IdentityUser (Interface có sẵn các thứ về người dùng)
   - Post
   - Comment
   - Like
   - FriendShip
   - Hashtag
   - PostHashtag (bảng trung gian)
   - PostReport
   - Notification
   - Story

Ví dụ:
"
public class User : IdentityUser
{
public string FullName {get; set;}
public string? ProfilePictureUrl {get; set;}
public string? Bio {get; set;}

    public ICollection<Post> Posts {get; set;}
    public ICollection<Like> Likes {get; set;}
    public ICollection<Comment> Comments {get; set;}

}
",

"public class Post
{
public int Id {get; set;}
public string Content {get; set;}
public string? ImageUrl {get; set;}
public DateTime CreatedAt {get; set;} = DateTime.Now;

    public string UserId {get; set;}
    public User User {get; set;}


    public ICollection <Comment> Comments {get; set;}
    public ICollection <Like> Likes {get; set;}
    public ICollection<PostReport> Reports {get; set;}

}
"

## tạo DbContext

# Tạo trong thư mục InteractHub.Infrastructure/Data

AppDbContext:
"
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
public class AppDbContext : IdentityDbContext<User>
{
public DbSet<Post> Posts {get; set;}
public DbSet<Comment> Comments {get; set;}
public DbSet<Like> Likes {get; set;}
public DbSet<FriendShip> FriendShips {get; set;}
public DbSet<Story> Stories {get; set;}
public DbSet<Notification> Notifications {get; set;}
public DbSet<Hashtag> Hashtags {get; set;}
public DbSet<PostReport> Reports {get; set;}  
}
"

## Tạo config cho các bảng phức tạp trong InteractHub/Infrastructure/Configurations

xem trong file

## khởi tạo database

dotnet ef migrations add Init
-> khởi tạo bản vẽ database

dotnet ef migrations add <tên>
-> khi thay đổi hay thêm entity mới

dotnet ef database update
-> tạo database (Có thể xem trong sqlserver), tạo database sạch, tạo lại toàn bộ bảng

dotnet ef database drop
-> Xóa database

dotnet ef migrations list
-> Xem danh sách migration

sử dụng các lệnh sau từ thư mục root của proj

dotnet ef database drop --force --project InteractHub.Infras/ --startup-project InteractHub.API/
-> xóa database từ root

dotnet ef migrations remove --project InteractHub.Infras/ --startup-project InteractHub.API/
-> xóa bản vẽ

dotnet ef migrations add Init --project InteractHub.Infras/ --startup-project InteractHub.API/
-> tạo bản vẽ mới

dotnet ef database update --project InteractHub.Infrastructure/ --startup-project InteractHub.API/

-> update lại database mới

## Tiếp theo

làm theo flow sau

1. Service (logic)
2. Controller (API)
3. Test bằng Swagger

# tạo service trong interface

public interface IPostService
{
Task<List<Post>> GetAllAsync();
Task<Post?> GetByIdAsync(int id);
Task<Post> CreateAsync(Post post);
Task<bool> DeleteAsync(int id);
}

# Implement service

#### kiểm tra nếu là môi trường developer thì mới cho sử dụng swagger/ nếu không bị lỗi dữ liệu, mất dữ liệu
