# ☁️ InteractHub - Triển Khai Lên Microsoft Azure

**Dự án:** InteractHub - Social Media Application  
**Nền tảng:** Microsoft Azure (App Service & SQL Database)  
**Mô hình:** SPA Integration (React + .NET trên cùng domain)  
**Link Demo:** [https://interacthub-demo.azurewebsites.net/home](https://interacthub-demo.azurewebsites.net/home)  
**Cập nhật:** May 02, 2026

---

## 📑 Mục Lục

1. [D1: Cơ Chế Hoạt Động Của Hệ Thống](#d1-cơ-chế-hoạt-động-của-hệ-thống)
2. [D2: Chuẩn Bị Tài Nguyên Trên Azure](#d2-chuẩn-bị-tài-nguyên-trên-azure)
3. [D3: Thay Đổi Mã Nguồn (Codebase)](#d3-thay-đổi-mã-nguồn-codebase)
4. [D4: Phân Tích Pipeline CI/CD](#d4-phân-tích-pipeline-cicd)

---

# D1: Cơ Chế Hoạt Động Của Hệ Thống

## 🎯 Ý Nghĩa

Tài liệu này mô tả chi tiết quy trình đưa ứng dụng **InteractHub** (bao gồm Backend .NET 9 và Frontend React Vite) lên hạ tầng điện toán đám mây **Microsoft Azure**. 

Thay vì thuê 2 máy chủ riêng biệt (1 cho Frontend, 1 cho Backend) và gặp phải rắc rối về CORS, hệ thống áp dụng chiến lược **SPA Integration**:
- **Frontend (React)** sẽ được biên dịch (Build) thành các tệp HTML, CSS, JS tĩnh.
- Toàn bộ các tệp tĩnh này sẽ được nhồi vào thư mục `wwwroot` của máy chủ **Backend (.NET)**.
- Khi người dùng truy cập web, máy chủ .NET sẽ đóng vai trò phục vụ giao diện web trước. Nếu người dùng gọi API, hệ thống sẽ trả về dữ liệu.

---

# D2: Chuẩn Bị Tài Nguyên Trên Azure

## 🌐 1. Tạo Database (Azure SQL Database)

- Khởi tạo Single SQL Database trên Azure Portal.
- **Lưu ý cực kỳ quan trọng:** Ở phần Firewall của Database, phải chọn bật tuỳ chọn **"Allow Azure services and resources to access this server"** để máy chủ Web App có quyền chui vào Database tạo bảng.
- Copy chuỗi kết nối **ADO.NET Connection String** để chuẩn bị cho bước sau.

## 🚀 2. Tạo Máy chủ Web (Azure App Service)

- Tạo một Web App mới với Runtime stack là `.NET 9 (STS)`. Có thể chọn HĐH Windows hoặc Linux (Linux thường khởi động nhanh và rẻ hơn).
- Vào mục **Settings > Environment variables** của App Service và khai báo các biến bảo mật để code có thể hoạt động (Lưu ý tên biến phải có 2 dấu gạch dưới `__`):

| Tên biến (Name) | Giá trị cần điền (Value) |
| :--- | :--- |
| `ConnectionStrings__DefaultConnection` | `Server=tcp:YOUR_SERVER.database.windows.net,1433;Initial Catalog=YOUR_DB;User ID=YOUR_USER;Password=YOUR_PASS;MultipleActiveResultSets=True;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;` |
| `JWT__SecretKey` | `this-is-a-very-secure-secret-key-for-interacthub-12345` |
| `JWT__Issuer` | `InteractHub` |
| `JWT__Audience` | `InteractHubUsers` |
| `Cloudinary__CloudName` | `demo` |
| `Cloudinary__ApiKey` | `demo` |
| `Cloudinary__ApiSecret` | `demo` |

## 🔑 3. Gắn Chìa Khóa Cho GitHub (Publish Profile)

- Tại trang quản trị của App Service, bấm vào nút **Download publish profile** ở thanh công cụ phía trên.
- Mở file `.PublishSettings` vừa tải về, copy toàn bộ nội dung.
- Vào kho lưu trữ trên GitHub -> **Settings** -> **Secrets and variables** -> **Actions**.
- Tạo một biến mới tên là `AZURE_WEBAPP_PUBLISH_PROFILE` và dán nội dung vào.

*Từ lúc này, cứ mỗi lần bạn `git push` Code lên nhánh `dev` hoặc `main`, Github Actions sẽ tự động Build và tống lên Azure.*

---

# D3: Thay Đổi Mã Nguồn (Codebase)

## 🎨 1. Thay Đổi Tại Frontend (React Vite)

- **Lệnh gọi API:** Frontend không được fix cứng gọi đến `localhost:5000` hay một đường link cứng nào, mà sử dụng đường dẫn tương đối `/api`.
  - *Giải thích:* Vì Frontend và Backend chạy chung trên 1 tên miền (ví dụ: `interacthub.azurewebsites.net`), lệnh gọi `fetch('/api/users')` sẽ gọi thẳng vào chính domain đang chạy nó, vô cùng an toàn và mượt mà.
- **Loại bỏ `node_modules` khỏi Github:** Thư mục này buộc phải nằm trong `.gitignore`.
  - *Giải thích:* Máy tính phát triển dùng hệ điều hành Windows, trong khi máy chủ Build của Github dùng Ubuntu (Linux). Nếu đẩy thư mục `node_modules` lên Github, các file thực thi (như `vite`) sẽ bị mất cờ cấp quyền chạy (`chmod +x`). Dẫn đến lỗi chí mạng: `sh: 1: vite: Permission denied`. 

## ⚙️ 2. Thay Đổi Tại Backend (`Program.cs`)

Để C# biết cách đọc file Frontend, chúng ta phải tiêm một số lệnh vào luồng Pipeline của `Program.cs`:

```csharp
// File: InteractHub.API/Program.cs

// 1. Phục vụ các file tĩnh của React (JS, CSS, HTML)
app.UseDefaultFiles();
app.UseStaticFiles();
```
**Giải thích:** Lệnh này báo cho Kestrel (Máy chủ .NET) biết rằng hãy cấp quyền đọc các tệp tin HTML/CSS/JS nằm trong thư mục `wwwroot` và trả nó ra màn hình nếu người dùng truy cập.

```csharp
// 2. Chuyển hướng các đường dẫn của React về trang chủ index.html
app.MapFallbackToFile("index.html");
```
**Giải thích:** React hoạt động dưới dạng Single Page Application (SPA), nó tự đẻ ra các đường dẫn giả (như `/profile`, `/messages`). Máy chủ C# không hiểu các đường dẫn giả này nên thường báo lỗi 404. Lệnh này ép C#: "Gặp đường link nào không hiểu, đừng báo lỗi 404, hãy đẩy nó về `index.html` để React tự xử lý".

```csharp
// 3. Tự động hóa tạo Bảng DB và Phục vụ Swagger
await context.Database.MigrateAsync();
app.UseSwagger();
```
**Giải thích:** Bật `MigrateAsync` giúp Azure tự tạo các cấu trúc Bảng trong SQL mà không cần chạy lệnh tay.

---

# D4: Phân Tích Pipeline CI/CD (`deploy-azure.yml`)

File này nằm ở `.github/workflows/deploy-azure.yml`, đóng vai trò là não bộ chỉ huy con Bot Github làm nhiệm vụ "Triển Khai Tự Động".

## 📜 Các Khối Cốt Lõi:

### **1. Trigger Pipeline**
```yaml
on:
  push:
    branches:
      - main
      - dev
```
**Giải thích:** Bất cứ khi nào có hành động đẩy Code lên nhánh `main` hoặc `dev`, luồng này sẽ tự kích hoạt.

### **2. Build & Publish Backend**
```yaml
      - name: Build and Publish Backend
        run: dotnet publish backend/InteractHub.API/InteractHub.API.csproj -c Release -o ./publish /p:UseAppHost=false
```
**Giải thích:** Biến mã nguồn C# thành file nhị phân (`.dll`) và đổ vào thư mục `publish`.

### **3. Build Frontend**
```yaml
      - name: Build Frontend
        run: |
          cd frontend
          npm install
          npm run build
```
**Giải thích:** Tải các thư viện Node.js và dịch React ra thành HTML/JS chuẩn ngót nghét đưa vào thư mục `dist`.

### **4. Gộp Frontend Vào Backend**
```yaml
      - name: Copy Frontend Build to Publish Folder
        run: |
          mkdir -p ./publish/wwwroot
          cp -r frontend/dist/* ./publish/wwwroot/
```
**Giải thích:** Đây là lệnh đỉnh cao kết hợp 2 hệ thống. Nó tạo một thư mục tên là `wwwroot` bên trong lòng thư mục `publish` của Backend, rồi bốc toàn bộ giao diện Frontend nhét vào đó.

### **5. Deploy Lên Azure**
```yaml
      - name: Deploy to Azure Web App
        uses: azure/webapps-deploy@v2
        with:
          app-name: ${{ env.AZURE_WEBAPP_NAME }}
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: ./publish
```
**Giải thích:** Lấy toàn bộ cái cục `publish` đã được nhồi nhét cẩn thận ở bước trên, sử dụng Chìa khóa Bí mật (Publish Profile) nối mạng với Microsoft Azure và đắp đè tệp tin lên máy chủ mà không gây gián đoạn hệ thống.
