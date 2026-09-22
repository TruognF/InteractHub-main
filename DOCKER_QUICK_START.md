# 🐳 Docker Quick Start - InteractHub

## Yêu Cầu
- ✅ Docker Desktop đã cài đặt
- ✅ .NET SDK (để chạy local) - hoặc chỉ cần Docker nếu chạy hoàn toàn trên container

## 🚀 Bắt Đầu Nhanh (Dùng Docker Compose)

### 1️⃣ Khởi Động Docker
```bash
# Vào thư mục project
cd f:\HocKy5\CSharp\InteractHub

# Khởi động SQL Server + API
docker-compose up --build

# Hoặc chạy ở background
docker-compose up -d --build
```

### 2️⃣ Chờ Initialization
Nhân đợi khoảng 30-60 giây để:
- SQL Server khởi động
- API build và chạy
- Database migrations chạy
- Seed data tạo xong

### 3️⃣ Kiểm Tra Logs
```bash
# Xem logs toàn bộ
docker-compose logs

# Xem logs API specific
docker-compose logs interacthub-api

# Xem logs SQL Server
docker-compose logs interacthub-db

# Follow logs real-time
docker-compose logs -f
```

### 4️⃣ Truy Cập Ứng Dụng
- 🌐 **Swagger UI**: http://localhost:5000/swagger
- 🔌 **API Base URL**: http://localhost:5000
- 🗄️ **Database**: localhost,1433 (User: `sa`, Pass: `StrongPass123!`)

## 📝 Khác Biệt: Local vs Docker

| Tiêu Chí | Local | Docker |
|---------|-------|--------|
| Cách chạy | `dotnet run` | `docker-compose up` |
| SQL Server | Có sẵn trên máy | Container tự động |
| Port API | 5142 (HTTPS) | 5000 (HTTP) |
| Database | `localhost` | `localhost,1433` |
| Tốc độ | Nhanh hơn | Chậm hơn lần đầu |

## 🔧 Các Lệnh Hữu Ích

### Kiểm Tra Status
```bash
# Danh sách containers đang chạy
docker-compose ps

# Danh sách images
docker images | grep interacthub

# Kiểm tra volumes
docker volume ls | grep sql_data
```

### Dừng/Xóa
```bash
# Dừng containers nhưng giữ dữ liệu
docker-compose stop

# Dừng và xóa containers
docker-compose down

# Xóa tất cả bao gồm volumes (⚠️ MẤT DỮ LIỆU)
docker-compose down -v
```

### Rebuild
```bash
# Rebuild từ đầu
docker-compose up --build --force-recreate

# Rebuild không cache
docker-compose build --no-cache
docker-compose up
```

## 🧪 Test Seed Data

### 1. Login
```bash
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user1@interacthub.com",
    "password": "User@123456"
  }'

# Response sẽ có token
```

### 2. Lấy Token
Sao chép `token` từ response

### 3. Dùng Token
```bash
curl -X GET http://localhost:5000/api/posts \
  -H "Authorization: Bearer YOUR_TOKEN_HERE"
```

## 📊 Database Connection Strings

### Từ Docker Container
```
Server=localhost,1433;Database=InteractHubDb;User Id=sa;Password=StrongPass123!;TrustServerCertificate=True;
```

### Từ Azure Data Studio / SSMS
- **Server**: `localhost,1433`
- **Authentication**: SQL Server Authentication
- **Login**: `sa`
- **Password**: `StrongPass123!`
- **Database**: InteractHubDb (tạo tự động)

## 🔐 Passwords Mặc Định

| Dịch vụ | User | Pass |
|--------|------|------|
| SQL Server | sa | StrongPass123! |
| Admin | admin | Admin@123456 |
| User1 | user1 | User@123456 |

## 🐛 Troubleshooting

### ❌ Lỗi: "Cannot connect to database"
```bash
# Kiểm tra SQL Server container
docker logs interacthub-db

# Kiểm tra network
docker network ls
docker inspect bridge

# Restart everything
docker-compose down
docker-compose up --build
```

### ❌ Lỗi: "Port 1433 already in use"
```bash
# Tìm process dùng port
netstat -ano | findstr :1433

# Hoặc đổi port trong docker-compose.yml
# ports:
#   - "1434:1433"  # Use 1434 instead
```

### ❌ Lỗi: "API không kết nối được database"
```bash
# Kiểm tra connection string trong appsettings.json
# Đảm bảo chờ đủ thời gian SQL Server khởi động
# Xem logs: docker-compose logs
```

### ❌ Seed data không được tạo
- Kiểm tra `DbInitializer.SeedTestData()` có được gọi không
- Kiểm tra không có lỗi trong logs
- Xóa volume và restart: `docker-compose down -v && docker-compose up`

## 💾 Backup/Restore Data

### Backup Database
```bash
# Vào SQL Server container
docker exec -it interacthub-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "StrongPass123!" \
  -Q "BACKUP DATABASE InteractHubDb TO DISK='/var/opt/mssql/backup/interacthub.bak'"

# Copy backup file
docker cp interacthub-db:/var/opt/mssql/backup/interacthub.bak ./backup/
```

### Restore Database
```bash
# Copy file vào container
docker cp ./backup/interacthub.bak interacthub-db:/var/opt/mssql/backup/

# Restore
docker exec -it interacthub-db /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "StrongPass123!" \
  -Q "RESTORE DATABASE InteractHubDb FROM DISK='/var/opt/mssql/backup/interacthub.bak'"
```

## 📚 Resources

- [Docker Docs](https://docs.docker.com/)
- [Docker Compose Docs](https://docs.docker.com/compose/)
- [SQL Server on Linux](https://docs.microsoft.com/en-us/sql/linux/quickstart-install-connect-docker)
- [.NET Docker](https://hub.docker.com/_/microsoft-dotnet)

## ✅ Checklist

- [ ] Docker Desktop installed và chạy
- [ ] `docker-compose up` executed
- [ ] Chờ ~60 giây initialization
- [ ] Kiểm tra http://localhost:5000/swagger
- [ ] Test login với user1@interacthub.com
- [ ] Xem seed data được tạo
- [ ] Database có data (5 users, 15 posts, 3 groups)

---

**Tip:** Sử dụng `docker-compose logs -f` để monitor real-time logs trong tab khác! 🔍
