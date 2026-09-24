# BỘ GIÁO DỤC VÀ ĐÀO TẠO
# TRƯỜNG ĐẠI HỌC SÀI GÒN
# KHOA CÔNG NGHỆ THÔNG TIN

<p align="center">
  <img src="./ảnh chụp web/logo_trường_học.png" width="150" alt="Logo trường">
</p>

<h2 align="center">BÁO CÁO NHÓM</h2>
<h3 align="center">Môn học: KIỂM THỬ PHẦN MỀM</h3>

<h1 align="center">ĐỀ TÀI: NỀN TẢNG MẠNG XÃ HỘI INTERACTHUB</h1>

<br>

**Giảng viên hướng dẫn:** Nguyễn Lê Thanh Trúc  
**Nhóm sinh viên thực hiện:**
1. Đặng Ngọc Tuấn - 3124410386
2. Nguyễn Anh Sỹ - 3124410306
3. Lê Vũ Huy Trường - 3124410383
4. Nguyễn Phước Hòa Lâm - 3123410193

**TP. Hồ Chí Minh, 2026**

---

## NHẬN XÉT CỦA GIÁO VIÊN

...................................................................................................................................................  
...................................................................................................................................................  
...................................................................................................................................................  
...................................................................................................................................................  
...................................................................................................................................................  
...................................................................................................................................................  
...................................................................................................................................................  
...................................................................................................................................................  
...................................................................................................................................................  
...................................................................................................................................................  

---

## MỤC LỤC
- [Chương 1: Giới thiệu tổng quan](#chương-1-giới-thiệu-tổng-quan)
- [Chương 2: Công nghệ sử dụng](#chương-2-công-nghệ-sử-dụng)
- [Chương 3: Phân tích và Thiết kế Hệ thống](#chương-3-phân-tích-và-thiết-kế-hệ-thống)
- [Chương 4: Hiện thực và Kết quả đạt được](#chương-4-hiện-thực-và-kết-quả-đạt-được)
- [Chương 5: Kết luận](#chương-5-kết-luận)

---

## Chương 1: Giới thiệu tổng quan

### Bối cảnh và lý do chọn đề tài
Trong thời đại số hóa hiện nay, mạng xã hội đã trở thành một phần không thể thiếu trong cuộc sống hàng ngày. InteractHub được phát triển với mục tiêu tạo ra một nền tảng kết nối người dùng một cách nhanh chóng, thân thiện và an toàn, đáp ứng nhu cầu giao tiếp, chia sẻ thông tin và giải trí của cộng đồng mạng.

### Mục tiêu của dự án
Dự án tập trung xây dựng các tính năng cốt lõi của một mạng xã hội bao gồm: 
- Đăng bài viết (văn bản, hình ảnh, video).
- Tương tác nội dung (thích, bình luận, chia sẻ).
- Nhắn tin theo thời gian thực (real-time chat).
- Quản lý tài khoản cá nhân, thông tin người dùng và kết bạn.
- Hệ thống quản trị (Admin) để kiểm duyệt nội dung và báo cáo vi phạm.

---

## Chương 2: Công nghệ sử dụng

### Backend (Máy chủ)
- **Ngôn ngữ & Framework:** C#, .NET 9 Web API.
- **Kiến trúc:** Clean Architecture.
- **Cơ sở dữ liệu:** SQL Server với Entity Framework Core.
- **Bảo mật:** ASP.NET Core Identity & JWT (JSON Web Token).
- **Real-time:** SignalR (cho tính năng chat trực tuyến và thông báo).

### Frontend (Giao diện người dùng)
- **Thư viện/Framework:** ReactJS (Vite).
- **Ngôn ngữ:** JavaScript / JSX.
- **Giao diện:** CSS thuần kết hợp Tailwind CSS.

---

## Chương 3: Phân tích và Thiết kế Hệ thống

### 3.1. Đối tượng người dùng và Phân quyền
| Đối tượng | Vai trò và Quyền hạn trong hệ thống |
|---|---|
| **Khách vãng lai** | Người chưa có tài khoản hoặc chưa đăng nhập. Chỉ có thể thao tác ở màn hình Đăng ký / Đăng nhập. |
| **Người dùng (User)** | Tài khoản đã xác thực. Có quyền tạo bài viết, bình luận, thả tim, nhắn tin, quản lý trang cá nhân, gửi yêu cầu kết bạn và tham gia nhóm. |
| **Quản trị viên (Admin)**| Quản lý tổng thể hệ thống. Có quyền khóa/mở khóa tài khoản người dùng, xem danh sách báo cáo vi phạm và quyết định xóa bài viết. |

### 3.2. Bảng Phân rã Phân hệ (Modules)
Hệ thống được thiết kế chia thành 4 phân hệ lớn để đảm bảo tính độc lập và dễ mở rộng:
| Mã phân hệ | Tên phân hệ | Chức năng lớn bao gồm |
|:---:|---|---|
| **M1** | Quản lý Tài khoản & Hồ sơ | Đăng ký, Đăng nhập (JWT), Cập nhật thông tin cá nhân, Đổi ảnh đại diện, Tìm kiếm người dùng. |
| **M2** | Tương tác & Bảng tin | Đăng bài, Sửa/Xóa bài, Đăng Tin (Story), Bình luận bài viết, Thích (Like) nội dung. |
| **M3** | Kết nối & Trò chuyện | Gửi/Nhận yêu cầu kết bạn, Nhắn tin thời gian thực (SignalR), Tạo và Quản lý Nhóm. |
| **M4** | Quản trị Hệ thống (Admin) | Quản lý danh sách toàn bộ người dùng, Xử lý Phiếu Báo cáo vi phạm (Report), Cấm tài khoản. |

### 3.3. Quy trình nghiệp vụ cơ bản

### 3.3. Quy trình nghiệp vụ cơ bản

#### 1. Quy trình nghiệp vụ - Xác thực (Đăng ký & Đăng nhập)
Quy trình này diễn ra khi Khách vãng lai muốn tạo tài khoản mới và đăng nhập để trở thành Người dùng chính thức của hệ thống.

**Bước 1 – Đăng ký tài khoản:**
Khách truy cập hệ thống, chọn mục Đăng ký. Khách nhập các thông tin cá nhân bắt buộc bao gồm Họ tên, Email và Mật khẩu. Hệ thống kiểm tra sự tồn tại của Email trong cơ sở dữ liệu.

**Bước 2 – Xử lý thông tin Đăng ký:**
Nếu Email hợp lệ, hệ thống băm (hash) mật khẩu và tạo hồ sơ người dùng mới trong Cơ sở dữ liệu.

**Bước 3 – Yêu cầu Đăng nhập:**
Người dùng nhập Email và Mật khẩu tại màn hình Đăng nhập. 

**Bước 4 – Đối chiếu và Cấp Token:**
Hệ thống đối chiếu mật khẩu với cơ sở dữ liệu. Nếu chính xác, hệ thống khởi tạo phiên làm việc, cấp phát Token JWT.

**Bước 5 – Chuyển hướng:**
Hệ thống chuyển hướng người dùng vào Trang chủ (Newsfeed) và cho phép truy cập các chức năng của ứng dụng.

**Luồng Xác thực:**
Khách nhập Form Đăng ký $\rightarrow$ Kiểm tra & Mã hóa $\rightarrow$ Lưu DB $\rightarrow$ Khách nhập Form Đăng nhập $\rightarrow$ Đối chiếu DB $\rightarrow$ Cấp Token $\rightarrow$ Đăng nhập thành công.

<p align="center">
  <img src="./ảnh chụp web/Xác thực Đăng ký  Đăng nhập.png" width="800" alt="Sơ đồ nghiệp vụ Xác thực">
  <br><i>Sơ đồ nghiệp vụ Xác thực (Đăng ký & Đăng nhập)</i>
</p>

---

#### 2. Quy trình nghiệp vụ - Đăng bài và Tương tác
Quy trình này được thực hiện khi người dùng muốn chia sẻ thông tin lên nền tảng và tương tác với mạng lưới bạn bè của mình.

**Bước 1 – Tạo bài viết mới:**
Tại Trang chủ, người dùng nhấn nút "Tạo bài viết". Người dùng nhập nội dung văn bản (tối đa 2000 ký tự) và có thể lựa chọn đính kèm thêm tối đa 4 hình ảnh hoặc video để minh họa.

**Bước 2 – Kiểm duyệt và Lưu trữ:**
Sau khi người dùng nhấn "Đăng", hệ thống tự động kiểm tra tính hợp lệ của dữ liệu (không chứa từ ngữ cấm, đúng định dạng tệp). Nếu thỏa mãn, bài viết được lưu trữ vào cơ sở dữ liệu kèm theo dấu thời gian (Timestamp).

**Bước 3 – Phân phối nội dung:**
Hệ thống tự động cập nhật và phân phối bài viết vừa đăng lên Bảng tin (Newsfeed) của chính tác giả và danh sách tất cả bạn bè của họ.

**Bước 4 – Tương tác bài viết:**
Khi bạn bè thấy bài viết trên bảng tin, họ có thể thực hiện các thao tác tương tác như Thích (Like) hoặc gõ văn bản để Bình luận (Comment). Hệ thống ghi nhận lập tức các tương tác này và lưu vào cơ sở dữ liệu.

**Bước 5 – Nhận thông báo:**
Ngay khi có người Thích hoặc Bình luận, hệ thống sử dụng kết nối SignalR để đẩy thông báo trực tiếp (Real-time) hiển thị ngay trên màn hình của tác giả bài viết mà không cần tải lại trang.

**Luồng Đăng bài & Tương tác:**
Tác giả nhập nội dung/ảnh $\rightarrow$ Đăng bài $\rightarrow$ Kiểm duyệt $\rightarrow$ Lưu DB Bài viết $\rightarrow$ Cập nhật Bảng tin $\rightarrow$ Hiển thị cho Bạn bè $\rightarrow$ Bạn bè tương tác (Thích/Bình luận) $\rightarrow$ Gửi thông báo cho tác giả.

<p align="center">
  <img src="./ảnh chụp web/Đăng bài viết.png" width="800" alt="Sơ đồ nghiệp vụ Đăng bài">
  <br><i>Sơ đồ nghiệp vụ Đăng bài và Phân phối</i>
</p>

---

#### 3. Quy trình nghiệp vụ - Gửi yêu cầu Kết bạn
Quy trình này diễn ra khi người dùng muốn kết nối và mở rộng mạng lưới bạn bè của mình trên nền tảng.

**Bước 1 – Gửi yêu cầu:**
Người dùng (Người gửi) vào trang cá nhân của người lạ và bấm nút "Kết bạn".

**Bước 2 – Kiểm tra tính hợp lệ:**
Hệ thống kiểm tra ID của hai người dùng để đảm bảo không trùng lặp và chưa từng gửi yêu cầu trước đó.

**Bước 3 – Lưu trạng thái:**
Hệ thống ghi nhận vào Sổ Quan hệ bạn bè với trạng thái "Đang chờ" (Pending).

**Bước 4 – Gửi thông báo:**
Hệ thống tạo một bản ghi thông báo mới và sử dụng SignalR đẩy thông báo Real-time đến thiết bị của Người nhận.

**Bước 5 – Phản hồi:**
Người nhận thấy chuông thông báo, bấm vào và chọn Chấp nhận hoặc Từ chối yêu cầu.

**Luồng Kết bạn:**
Bấm nút Kết bạn $\rightarrow$ Kiểm tra hợp lệ $\rightarrow$ Lưu trạng thái Chờ (Pending) $\rightarrow$ Đẩy thông báo Real-time $\rightarrow$ Người nhận thấy chuông thông báo.

<p align="center">
  <img src="./ảnh chụp web/Gửi yêu cầu Kết bạn.png" width="800" alt="Sơ đồ nghiệp vụ Kết bạn">
  <br><i>Sơ đồ nghiệp vụ Gửi yêu cầu Kết bạn</i>
</p>

---

#### 4. Quy trình nghiệp vụ - Báo cáo và Xử lý vi phạm
Quy trình này diễn ra khi cộng đồng phát hiện nội dung độc hại và Quản trị viên tiến hành dọn dẹp nền tảng.

**Bước 1 – Phát hiện vi phạm:**
Trong quá trình lướt Bảng tin (Newsfeed) hoặc trang cá nhân, người dùng phát hiện một bài viết có nội dung xấu, phản cảm hoặc vi phạm tiêu chuẩn cộng đồng.

**Bước 2 – Gửi Phiếu báo cáo (Report):**
Người dùng nhấn vào tùy chọn "Báo cáo" gắn trên bài viết đó. Một biểu mẫu xuất hiện, người dùng bắt buộc chọn lý do vi phạm tương ứng (Spam, Bạo lực, Ngôn từ kích động...) và nhấn Gửi.

**Bước 3 – Ghi nhận hệ thống:**
Hệ thống tiếp nhận yêu cầu, tự động tạo ra một Phiếu báo cáo mới gắn liền với ID của bài viết và ID của người thực hiện báo cáo, đồng thời chuyển trạng thái phiếu thành "Đang chờ xử lý".

**Bước 4 – Quản trị viên tiếp nhận:**
Quản trị viên (Admin) đăng nhập vào hệ thống bằng tài khoản có đặc quyền và truy cập vào Bảng điều khiển (Dashboard) tại mục Quản lý Báo cáo.

**Bước 5 – Kiểm tra và Đối chiếu:**
Admin xem danh sách các báo cáo đang tồn đọng. Khi click vào một phiếu, hệ thống hiển thị chi tiết nội dung bài viết bị tố cáo, thông tin tác giả và lý do tố cáo để Admin xem xét trực quan.

**Bước 6 – Ra quyết định xử lý:**
Dựa trên các quy tắc cộng đồng, Admin đưa ra quyết định cuối cùng. Nếu nội dung bình thường, Admin chọn "Từ chối", phiếu báo cáo kết thúc. Nếu nội dung thực sự vi phạm, Admin chọn "Duyệt".

**Bước 7 – Thực thi hình phạt:**
Khi Admin chọn "Duyệt", hệ thống tự động ẩn bài viết đó khỏi toàn bộ Bảng tin bằng cơ chế Xóa mềm (`IsDeleted = true`). Đồng thời, hệ thống lưu lại lịch sử xử lý và ghi một cảnh cáo vào hồ sơ của tác giả bài viết.

**Luồng Xử lý vi phạm:**
Phát hiện bài viết xấu $\rightarrow$ Nhấn Báo cáo $\rightarrow$ Hệ thống tạo Phiếu báo cáo $\rightarrow$ Admin đăng nhập Dashboard $\rightarrow$ Xem chi tiết báo cáo $\rightarrow$ Đánh giá vi phạm $\rightarrow$ Quyết định Duyệt (Xóa bài) $\rightarrow$ Đổi trạng thái bài viết $\rightarrow$ Ghi lịch sử phạt.

<p align="center">
  <img src="./ảnh chụp web/Xử lý báo cáo vi phạm.png" width="800" alt="Sơ đồ nghiệp vụ Xử lý vi phạm">
  <br><i>Sơ đồ nghiệp vụ Báo cáo và Xử lý vi phạm</i>
</p>

### 3.4. Mô tả các Dữ liệu / Hồ sơ liên quan
| STT | Tên Hồ sơ/Dữ liệu | Bộ phận truy cập | Nội dung chính được lưu trữ |
|:---:|---|---|---|
| 1 | **Hồ sơ Người dùng** | User, Admin | ID hệ thống, Họ tên, Ảnh đại diện, Tiểu sử (Bio), Trạng thái hoạt động (Active/Banned). |
| 2 | **Dữ liệu Bài viết** | User, Admin | Nội dung văn bản, Danh sách Link ảnh, Thời gian đăng, Cờ trạng thái `IsDeleted`. |
| 3 | **Lịch sử Tin nhắn** | User | Nội dung tin nhắn, ID Người gửi, ID Người nhận, Dấu thời gian. |
| 4 | **Phiếu Báo cáo** | Admin | ID Bài viết vi phạm, ID Người báo cáo, Phân loại lý do báo cáo, Trạng thái (Đang chờ/Đã xử lý). |

### 3.5. Bảng Mô tả công việc chi tiết
| Mã việc | Công việc | Mô tả chi tiết | Điều kiện khởi động | Quy tắc nghiệp vụ | Vị trí | Hồ sơ nhập |
|:---:|---|---|---|---|---|---|
| **T1** | Đăng ký tài khoản | Nhập thông tin cá nhân (Email, Họ tên, Mật khẩu) để tạo tài khoản mới. | Khách truy cập web và chọn "Đăng ký". | Email chưa tồn tại trong hệ thống. Mật khẩu phải từ 8 ký tự trở lên. | Khách vãng lai | Họ tên, Email, Mật khẩu |
| **T2** | Đăng bài viết | Nhập nội dung văn bản và tải lên hình ảnh chia sẻ lên bảng tin. | Người dùng bấm "Tạo bài viết" trên Trang chủ. | Tối đa 2000 ký tự chữ và đính kèm tối đa 4 hình ảnh. Không chứa từ ngữ cấm. | Người dùng (User) | Nội dung chữ, File ảnh/video |
| **T3** | Bình luận bài đăng | Gửi phản hồi bằng văn bản dưới bài đăng của người khác. | Người dùng gõ nội dung và nhấn "Enter". | Bài viết phải đang tồn tại (chưa bị xóa). Bình luận không vượt quá 500 ký tự. | Người dùng (User) | Nội dung bình luận, ID Bài viết |
| **T4** | Gửi yêu cầu kết bạn | Nhấn nút kết bạn tại trang cá nhân của người khác để mở rộng mạng lưới. | Tìm thấy trang cá nhân người lạ và bấm "Kết bạn". | Không được tự gửi cho chính mình. Chưa có yêu cầu kết bạn nào đang chờ duyệt. | Người dùng (User) | ID người nhận, Trạng thái (Pending) |
| **T5** | Gửi tin nhắn (Chat) | Nhập nội dung và gửi tin nhắn tức thời (Real-time qua SignalR). | Mở hộp thoại chat và nhấn nút "Gửi". | Hai người dùng không được chặn (Block) nhau. Giới hạn 1000 ký tự/tin nhắn. | Người dùng (User) | Nội dung tin nhắn, ID Người nhận |
| **T6** | Đăng Tin (Story) | Chia sẻ hình ảnh khoảnh khắc ngắn hạn lên đầu trang chủ. | Người dùng bấm dấu "+" trên thanh Story. | Tin (Story) sẽ tự động hết hạn và ẩn đi sau đúng 24 giờ kể từ lúc đăng. | Người dùng (User) | File ảnh, Thời gian đăng |
| **T7** | Báo cáo vi phạm | Chọn lý do cụ thể (Spam, Bạo lực...) để báo cáo bài viết lên Admin. | Bấm nút "Report" trên bài viết có nội dung xấu. | Bắt buộc chọn 1 lý do từ danh sách. Không được tự báo cáo bài viết của mình. | Người dùng (User) | ID Bài viết, Lý do báo cáo |
| **T8** | Xử lý báo cáo | Xem danh sách Phiếu báo cáo và quyết định xóa bài hoặc từ chối. | Admin truy cập Dashboard mục "Quản lý Báo cáo". | Nếu bấm "Duyệt": Bài viết bị xóa mềm (Soft-delete) và hệ thống ghi log cảnh cáo. | Quản trị viên (Admin) | ID Phiếu báo cáo, Quyết định xử lý |

### 3.6. Danh sách Yêu cầu Chức năng (Functional Requirements)

Bảng dưới đây liệt kê các yêu cầu chức năng cốt lõi của hệ thống InteractHub, được phân rã chi tiết để đảm bảo tính nguyên tử (atomic), khả năng kiểm thử và đối chiếu trực tiếp với sơ đồ thực thể (ERD) cũng như các Use-case.

| Mã YC | Tên chức năng | Mô tả | Tác nhân | Điều kiện trước | Ràng buộc/Ngoại lệ | Ưu tiên |
|:---:|---|---|---|---|---|:---:|
| **FR-01** | Đăng ký tài khoản | Hệ thống cho phép tạo tài khoản mới bằng Email và Mật khẩu. | Người dùng (Khách) | Email chưa tồn tại trong CSDL. | Mật khẩu tối thiểu 8 ký tự.<br>Ngoại lệ: Email đã tồn tại -> Báo lỗi. | Must |
| **FR-02** | Đăng nhập hệ thống | Hệ thống xác thực và cấp token phiên làm việc (JWT). | Người dùng (Khách) | Đã có tài khoản hợp lệ trong hệ thống. | Sai mật khẩu 5 lần -> Khóa IP 15 phút.<br>Tài khoản bị ban -> Chặn đăng nhập. | Must |
| **FR-03** | Đăng bài viết | Hệ thống lưu trữ và hiển thị bài viết mới (chữ, ảnh) lên bảng tin. | Người dùng (User) | Đã đăng nhập và tài khoản không bị cấm đăng bài. | Tối đa 4 ảnh/bài, nội dung chữ ≤ 2000 ký tự. Không chứa từ ngữ cấm. | Must |
| **FR-04** | Chỉnh sửa bài viết | Hệ thống cập nhật nội dung mới cho bài viết đã đăng. | Người dùng (User) | Là tác giả bài viết, bài viết chưa bị xóa. | Chỉ được sửa nội dung chữ, không cho phép đổi/xóa ảnh sau khi đã đăng. | Should |
| **FR-05** | Xóa bài viết | Hệ thống ẩn bài viết khỏi bảng tin (Cơ chế Soft-delete). | User/Admin | Là tác giả hoặc có quyền Quản trị viên. | Chuyển trạng thái `IsDeleted = true`, bảo lưu dữ liệu vật lý dưới DB. | Must |
| **FR-06** | Thích bài viết | Hệ thống ghi nhận và tăng tổng số lượt thích của bài viết lên 1. | Người dùng (User) | Chưa thích bài viết này. | Không giới hạn số lượng bài có thể thích. (Click lần 2 sẽ gọi FR Bỏ thích). | Must |
| **FR-07** | Bình luận bài viết | Hệ thống lưu và hiển thị bình luận mới ngay dưới bài viết. | Người dùng (User) | Bài viết đang tồn tại (chưa bị xóa). | Chiều dài bình luận ≤ 500 ký tự. Không hỗ trợ đính kèm ảnh. | Must |
| **FR-08** | Gửi yêu cầu kết bạn | Hệ thống tạo trạng thái Pending và gửi thông báo đến đối phương. | Người dùng (User) | Chưa là bạn bè, chưa có yêu cầu chờ duyệt. | Không thể tự gửi yêu cầu cho chính ID của mình. | Must |
| **FR-09** | Chấp nhận kết bạn | Hệ thống cập nhật quan hệ FriendshipStatus thành Accepted. | Người dùng (User) | Có lời mời kết bạn (Pending) từ đối phương. | Một tài khoản có tối đa 5000 bạn bè. Vượt quá -> Báo lỗi. | Must |
| **FR-10** | Gửi tin nhắn cá nhân| Hệ thống gửi và hiển thị tin nhắn qua SignalR (Real-time). | Người dùng (User) | Hai người dùng không chặn (block) nhau. | Tin nhắn văn bản ≤ 1000 ký tự/lần gửi. | Must |
| **FR-11** | Đăng tin (Story) | Hệ thống lưu trữ và hiển thị hình ảnh/trạng thái ngắn hạn. | Người dùng (User) | Tài khoản đang hoạt động. | Tự động chuyển trạng thái Hết hạn (Expired) sau đúng 24 giờ. | Should |
| **FR-12** | Tạo nhóm (Group) | Hệ thống tạo thực thể Nhóm mới và cấp quyền Admin cho người tạo. | Người dùng (User) | Tên nhóm chưa tồn tại trong hệ thống. | Tên nhóm tối đa 100 ký tự. | Should |
| **FR-13** | Báo cáo bài viết | Hệ thống ghi nhận phản ánh của người dùng về nội dung xấu. | Người dùng (User) | Bài viết không phải do chính mình đăng. | Bắt buộc chọn 1 lý do từ danh sách (Spam, Bạo lực...). | Must |
| **FR-14** | Xử lý báo cáo | Hệ thống cập nhật trạng thái Report (Duyệt/Từ chối) và áp dụng phạt. | Admin | Đăng nhập bằng tài khoản Admin. | Nếu duyệt: Tự động kích hoạt FR-05 (Xóa bài) và ghi log cảnh cáo. | Must |
| **FR-15** | Khóa tài khoản | Hệ thống vô hiệu hóa quyền truy cập của người dùng vi phạm. | Admin | Đăng nhập bằng tài khoản Admin. | Người bị khóa sẽ lập tức bị mất hiệu lực token (out phiên hiện tại). | Must |


### Danh sách Yêu cầu Phi chức năng (Non-Functional Requirements)

Yêu cầu phi chức năng quy định các tiêu chuẩn về chất lượng, hiệu năng, bảo mật và khả năng chịu tải mà hệ thống phải đáp ứng để đảm bảo trải nghiệm người dùng tốt nhất.

| Mã YC | Phân loại | Tên yêu cầu | Mô tả chi tiết | Ưu tiên |
|:---:|---|---|---|:---:|
| **NFR-01** | Hiệu năng | Thời gian phản hồi API | Các thao tác thông thường (đăng bài, bình luận, lướt feed) phải phản hồi về Frontend trong vòng dưới **2 giây**. | Must |
| **NFR-02** | Hiệu năng | Độ trễ Real-time (SignalR) | Tin nhắn (Chat) và Thông báo phải truyền đến thiết bị người nhận với độ trễ (latency) dưới **500ms**. | Must |
| **NFR-03** | Chịu tải | Truy cập đồng thời (CCU) | Hệ thống backend phải chịu được ít nhất **500 người dùng trực tuyến** cùng lúc mà không bị crash. | Should |
| **NFR-04** | Bảo mật | Mã hóa & Xác thực | Mật khẩu phải được băm (Hash) bằng ASP.NET Core Identity. Phiên làm việc được bảo mật qua Token JWT. | Must |
| **NFR-05** | Khả dụng | Giao diện đáp ứng (Responsive) | Giao diện React/Tailwind phải co giãn và hiển thị tốt trên cả 3 nền tảng: **Desktop, Tablet, Mobile**. | Must |
| **NFR-06** | Tương thích | Trình duyệt hỗ trợ | Ứng dụng phải hoạt động mượt mà trên các trình duyệt hiện đại: Chrome, Edge, Safari, Firefox. | Should |
| **NFR-07** | Tin cậy | Tính vẹn toàn dữ liệu | Không xóa hẳn dữ liệu quan trọng (User, Post) mà dùng Soft-delete (`IsDeleted = true`) để dễ dàng khôi phục. | Must |

### Sơ đồ thực thể kết hợp (ERD)
Dưới đây là sơ đồ thực thể kết hợp (ERD) thể hiện cấu trúc cơ sở dữ liệu của hệ thống InteractHub, bao gồm các thực thể chính như Người dùng (Users), Bài viết (Posts), Bình luận (Comments), Lượt thích (Likes), và Tin nhắn (Messages).

<p align="center">
  <img src="./ảnh chụp web/erd.png" width="800" alt="Sơ đồ thực thể kết hợp (ERD)">
  <br>
  <i>Sơ đồ thực thể kết hợp (ERD) của hệ thống InteractHub</i>
</p>

---

## Chương 4: Hiện thực và Kết quả đạt được

### Giao diện Người dùng (User Frontend)

#### Đăng nhập Hệ thống
Giao diện cho phép người dùng xác thực và truy cập vào tài khoản cá nhân.
<p align="center">
  <img src="./ảnh chụp web/user_login.png" width="700" alt="Giao diện Đăng nhập người dùng">
  <br><i>Giao diện Đăng nhập người dùng</i>
</p>

#### Trang chủ & Bảng tin (Newsfeed)
Nơi hiển thị các bài viết mới nhất từ bạn bè và cộng đồng.
<p align="center">
  <img src="./ảnh chụp web/user_homepage.png" width="800" alt="Giao diện Trang chủ">
  <br><i>Giao diện Trang chủ (Bảng tin tổng quan)</i>
</p>

<p align="center">
  <img src="./ảnh chụp web/user_feed.png" width="800" alt="Giao diện Tương tác Bài viết">
  <br><i>Giao diện Tương tác Bài viết (Feed)</i>
</p>

#### Tính năng Tin (Story)
Cho phép người dùng chia sẻ những khoảnh khắc ngắn ngày.
<p align="center">
  <img src="./ảnh chụp web/user_story.png" width="800" alt="Giao diện Xem Tin (Story)">
  <br><i>Giao diện Xem Tin (Story)</i>
</p>

#### Trang Cá nhân (Profile)
Khu vực quản lý thông tin cá nhân và lịch sử bài đăng của người dùng.
<p align="center">
  <img src="./ảnh chụp web/user_profile.png" width="800" alt="Giao diện Trang Cá nhân">
  <br><i>Giao diện Trang Cá nhân (Profile)</i>
</p>

#### Nhóm (Groups)
Tính năng cho phép người dùng tham gia các cộng đồng chung sở thích.
<p align="center">
  <img src="./ảnh chụp web/user_group.png" width="800" alt="Giao diện Nhóm chi tiết">
  <br><i>Giao diện Nhóm chi tiết</i>
</p>
<p align="center">
  <img src="./ảnh chụp web/user_group_search.png" width="800" alt="Giao diện Tìm kiếm Nhóm">
  <br><i>Giao diện Tìm kiếm Nhóm</i>
</p>

#### Tin nhắn (Chat)
Tính năng trao đổi tin nhắn trực tiếp theo thời gian thực (Real-time Chat).
<p align="center">
  <img src="./ảnh chụp web/user_chat.png" width="800" alt="Giao diện Nhắn tin (Chat)">
  <br><i>Giao diện Nhắn tin (Chat)</i>
</p>

### Giao diện Quản trị viên (Admin Panel)

#### Đăng nhập Quản trị viên
<p align="center">
  <img src="./ảnh chụp web/admin_login.png" width="700" alt="Giao diện Đăng nhập Admin">
  <br><i>Giao diện Đăng nhập Admin</i>
</p>

#### Quản lý Người dùng (Users Management)
Cho phép quản trị viên xem danh sách người dùng, khóa hoặc mở khóa tài khoản.
<p align="center">
  <img src="./ảnh chụp web/admin_users.png" width="800" alt="Giao diện Danh sách Người dùng">
  <br><i>Giao diện Danh sách Người dùng</i>
</p>
<p align="center">
  <img src="./ảnh chụp web/admin_users_detail.png" width="800" alt="Giao diện Chi tiết Người dùng">
  <br><i>Giao diện Chi tiết Người dùng</i>
</p>

#### Quản lý Báo cáo Vi phạm (Reports Management)
Hỗ trợ kiểm duyệt các bài viết hoặc bình luận bị báo cáo bởi cộng đồng.
<p align="center">
  <img src="./ảnh chụp web/admin_reports.png" width="800" alt="Giao diện Danh sách Báo cáo Vi phạm">
  <br><i>Giao diện Danh sách Báo cáo Vi phạm</i>
</p>
<p align="center">
  <img src="./ảnh chụp web/admin_report_detail.png" width="600" alt="Giao diện Chi tiết và Xử lý Báo cáo">
  <br><i>Giao diện Chi tiết và Xử lý Báo cáo</i>
</p>

---

## Chương 5: Kết luận

### Kết quả đạt được
Hệ thống mạng xã hội InteractHub đã hoàn thành các mục tiêu cơ bản đề ra ban đầu. Nhóm đã áp dụng thành công các công nghệ hiện đại như .NET 9, ReactJS và SignalR để xây dựng một nền tảng hoạt động ổn định, có độ phản hồi cao và giao diện thân thiện với người dùng. Các tính năng cốt lõi đều hoạt động tốt trên môi trường thử nghiệm và sẵn sàng để phát triển, mở rộng ở những giai đoạn tiếp theo.
