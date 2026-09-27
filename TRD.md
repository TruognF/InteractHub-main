# TECHNICAL REQUIREMENT DOCUMENT (TRD)
## DỰ ÁN: INTERACTHUB – MẠNG XÃ HỘI TƯƠNG TÁC
**Phiên bản:** 2.0  
**Ngày cập nhật:** 25 Tháng 9, 2026  
**Công nghệ:** ASP.NET Core 9 (Web API) + React 19 (Vite) + SQL Server + SignalR  

---

## 1. ĐĂNG KÝ TÀI KHOẢN (User Registration)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T01** | Tên đăng nhập (UserName) không được để trống. |
| **T02** | Tên đăng nhập phải có ít nhất 3 ký tự. |
| **T03** | Tên đăng nhập không được vượt quá 20 ký tự. |
| **T04** | Tên đăng nhập chỉ được chứa chữ cái (a-z, A-Z), số (0-9) và dấu gạch dưới (_). |
| **T05** | Tên đăng nhập chưa được sử dụng bởi tài khoản khác trong hệ thống (không trùng lặp). |
| **T06** | Email không được để trống. |
| **T07** | Email phải đúng định dạng email hợp lệ (ví dụ: user@domain.com). |
| **T08** | Email chưa được đăng ký bởi tài khoản khác trong hệ thống (không trùng lặp). |
| **T09** | Họ tên (FullName) không được để trống. |
| **T10** | Họ tên phải có ít nhất 2 ký tự. |
| **T11** | Họ tên không được vượt quá 100 ký tự. |
| **T12** | Mật khẩu (Password) không được để trống. |
| **T13** | Mật khẩu phải có ít nhất 8 ký tự. |
| **T14** | Mật khẩu phải chứa ít nhất 1 chữ cái viết hoa (A-Z). |
| **T15** | Mật khẩu phải chứa ít nhất 1 chữ cái viết thường (a-z). |
| **T16** | Mật khẩu phải chứa ít nhất 1 chữ số (0-9). |
| **T17** | Mật khẩu phải chứa ít nhất 1 ký tự đặc biệt (!@#$%^&*(),.?":{}|<>). |
| **T18** | Mật khẩu phải được băm (hash) bằng thuật toán PBKDF2 (ASP.NET Core Identity) trước khi lưu vào cơ sở dữ liệu. |
| **T19** | Nếu tất cả dữ liệu hợp lệ, hệ thống tạo tài khoản mới và tự động gán vai trò mặc định là "User". |
| **T20** | Nếu gán vai trò mặc định thất bại, hệ thống phải xóa tài khoản vừa tạo (rollback). |
| **T21** | Đăng ký thành công trả về HTTP 201 Created kèm JWT Token (thời hạn 60 phút). |
| **T22** | Đăng ký thất bại trả về HTTP 400 Bad Request kèm danh sách lỗi validation chi tiết theo từng trường. |

---

## 2. ĐĂNG NHẬP NGƯỜI DÙNG (User Login)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T23** | Tên đăng nhập (UserName) không được để trống. |
| **T24** | Mật khẩu (Password) không được để trống. |
| **T25** | Tên đăng nhập và mật khẩu không được để trống cùng lúc. |
| **T26** | Tài khoản người dùng phải tồn tại trong cơ sở dữ liệu. |
| **T27** | Mật khẩu nhập vào phải khớp với mật khẩu đã hash trong cơ sở dữ liệu. |
| **T28** | Tài khoản có vai trò Admin không được phép đăng nhập qua cổng này (/api/Auth/login). Admin phải sử dụng cổng riêng (/api/admin/login). |
| **T29** | Nếu thông tin đăng nhập hợp lệ, hệ thống cấp JWT Bearer Token (thời hạn 60 phút) chứa UserId, UserName, Email, FullName và trả về HTTP 200 OK. |
| **T30** | Nếu sai tên đăng nhập hoặc mật khẩu, hệ thống trả về HTTP 401 Unauthorized với thông báo chung "Invalid username or password" (không chỉ rõ sai trường nào để bảo mật). |

---

## 3. ĐĂNG NHẬP QUẢN TRỊ VIÊN (Admin Login)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T31** | Cổng đăng nhập Admin (/api/admin/login) cho phép truy cập công khai (AllowAnonymous), không yêu cầu Token. |
| **T32** | Tên đăng nhập (UserName) không được để trống. |
| **T33** | Mật khẩu (Password) không được để trống. |
| **T34** | Tên đăng nhập và mật khẩu không được để trống cùng lúc. |
| **T35** | Tài khoản phải tồn tại trong cơ sở dữ liệu. |
| **T36** | Mật khẩu nhập vào phải khớp với mật khẩu đã hash trong cơ sở dữ liệu. |
| **T37** | Tài khoản bắt buộc phải có vai trò "Admin" mới được phép đăng nhập qua cổng này. |
| **T38** | Người dùng thường (chỉ có role User) cố đăng nhập qua cổng Admin sẽ bị từ chối với HTTP 403 Forbidden ("Only admin users can access this endpoint"). |
| **T39** | Đăng nhập Admin thành công, hệ thống cấp JWT Token với thời hạn 120 phút và claim IsAdmin=true. |
| **T40** | Sai tên đăng nhập hoặc mật khẩu trả về HTTP 401 Unauthorized. |

---

## 4. LÀM MỚI TOKEN ADMIN (Admin Refresh Token)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T41** | Yêu cầu phải kèm JWT Token Admin hợp lệ (Authorize Roles = "Admin"). |
| **T42** | Tài khoản Admin phải còn tồn tại trong cơ sở dữ liệu. |
| **T43** | Tài khoản phải còn giữ vai trò Admin tại thời điểm yêu cầu làm mới. |
| **T44** | Nếu tài khoản không còn vai trò Admin, trả về HTTP 403 Forbidden ("User is not an admin"). |
| **T45** | Làm mới thành công cấp Token mới với thời hạn 120 phút. |

---

## 5. QUẢN LÝ VAI TRÒ — GÁN VAI TRÒ (Admin Assign Role)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T46** | Yêu cầu phải có quyền AdminOnly (Policy). |
| **T47** | UserId của người dùng mục tiêu không được để trống. |
| **T48** | Người dùng mục tiêu phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 404 Not Found. |
| **T49** | Vai trò (Role) cần gán phải tồn tại trong hệ thống Identity. Nếu không → HTTP 400 Bad Request ("Role 'xxx' does not exist"). |
| **T50** | Người dùng mục tiêu chưa sở hữu vai trò đó (không gán trùng). Nếu đã có → HTTP 400 ("User already has role 'xxx'"). |
| **T51** | Gán vai trò thành công trả về HTTP 200 OK. |

---

## 6. QUẢN LÝ VAI TRÒ — GỠ VAI TRÒ (Admin Remove Role)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T52** | Yêu cầu phải có quyền AdminOnly (Policy). |
| **T53** | Người dùng mục tiêu phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 404 Not Found. |
| **T54** | Người dùng mục tiêu phải đang sở hữu vai trò cần gỡ. Nếu không → HTTP 400 ("User does not have role 'xxx'"). |
| **T55** | Quản trị viên không được phép tự gỡ vai trò Admin của chính mình. Nếu vi phạm → HTTP 400 ("Cannot remove Admin role from yourself"). |
| **T56** | Gỡ vai trò thành công trả về HTTP 200 OK. |

---

## 7. KHÓA / MỞ KHÓA TÀI KHOẢN (Admin Toggle Lock User)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T57** | Yêu cầu phải có quyền AdminOnly (Policy). |
| **T58** | Tài khoản mục tiêu phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 404. |
| **T59** | Quản trị viên không được phép tự khóa tài khoản của chính mình. Nếu vi phạm → HTTP 400 ("Không thể khóa tài khoản của chính mình"). |
| **T60** | Nếu tài khoản đang bị khóa (LockoutEnd > UtcNow) → hệ thống mở khóa (LockoutEnd = null). |
| **T61** | Nếu tài khoản đang mở → hệ thống khóa vĩnh viễn (LockoutEnd = UtcNow + 100 năm). |

---

## 8. ĐẶT LẠI MẬT KHẨU (Admin Reset User Password)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T62** | Yêu cầu phải có quyền AdminOnly (Policy). |
| **T63** | Mật khẩu mới (NewPassword) không được để trống. |
| **T64** | Mật khẩu mới phải có ít nhất 6 ký tự. |
| **T65** | Tài khoản mục tiêu phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 404. |
| **T66** | Hệ thống xóa mật khẩu cũ và thiết lập mật khẩu mới thông qua ASP.NET Core Identity. |

---

## 9. XÓA TÀI KHOẢN NGƯỜI DÙNG (Admin Delete User)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T67** | Yêu cầu phải có quyền AdminOnly (Policy). |
| **T68** | Tài khoản mục tiêu phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 404. |
| **T69** | Quản trị viên không được phép tự xóa tài khoản của chính mình. Nếu vi phạm → HTTP 400 ("Không thể xóa tài khoản của chính mình"). |
| **T70** | Xóa tài khoản thành công trả về HTTP 200 OK. |

---

## 10. CẬP NHẬT HỒ SƠ CÁ NHÂN (Update User Profile)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T71** | Yêu cầu phải đăng nhập (Authorize). |
| **T72** | Chỉ chủ tài khoản mới được phép cập nhật hồ sơ của mình (id route phải trùng với userId trong Token). |
| **T73** | Người khác cố cập nhật hồ sơ không phải của mình → HTTP 403 Forbidden ("You cannot update this user"). |
| **T74** | Họ tên (FullName) không được để trống. |
| **T75** | Họ tên phải có ít nhất 2 ký tự. |
| **T76** | Họ tên không được vượt quá 100 ký tự. |
| **T77** | ProfilePictureUrl (nếu có) phải là URL hợp lệ bắt đầu bằng http:// hoặc https://. |
| **T78** | Tiểu sử (Bio) không được vượt quá 500 ký tự. |
| **T79** | Cập nhật thành công, hệ thống phát sự kiện SignalR "UserProfileUpdated" đến tất cả client liên quan. |

---

## 11. TẢI LÊN ẢNH ĐẠI DIỆN (Upload Profile Picture)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T80** | Yêu cầu phải đăng nhập (Authorize). |
| **T81** | Chỉ chủ tài khoản mới được phép thay đổi ảnh đại diện của mình. |
| **T82** | File ảnh không được để trống (bắt buộc phải chọn file). Nếu trống → HTTP 400 ("Please select a file to upload"). |
| **T83** | Dung lượng file ảnh không được vượt quá 5MB (5,242,880 bytes). Nếu vượt → HTTP 400 ("File size must not exceed 5MB"). |
| **T84** | File ảnh phải thuộc một trong các định dạng MIME: image/jpeg, image/png, image/gif, image/webp. |
| **T85** | File ảnh có định dạng khác (ví dụ: BMP, TIFF, SVG) sẽ bị từ chối → HTTP 400 ("Only image files (JPEG, PNG, GIF, WebP) are allowed"). |
| **T86** | Hệ thống tự động xóa ảnh đại diện cũ trên ổ đĩa (nếu có) trước khi lưu ảnh mới. |
| **T87** | Ảnh mới được lưu tại thư mục /uploads/avatars/ với tên file là GUID duy nhất. |
| **T88** | Tải ảnh thành công, hệ thống phát sự kiện SignalR "UserProfileUpdated". |

---

## 12. TÌM KIẾM NGƯỜI DÙNG (Search Users)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T89** | Yêu cầu phải đăng nhập (Authorize). |
| **T90** | Từ khóa tìm kiếm được tự động cắt bỏ khoảng trắng đầu/cuối (trim). |
| **T91** | Từ khóa tìm kiếm bị cắt ngắn nếu vượt quá 100 ký tự. |
| **T92** | Từ khóa tìm kiếm được khử khuẩn tự động: loại bỏ các ký tự nguy hiểm ; ' " \ để chống SQL Injection và XSS. |
| **T93** | Tìm kiếm so khớp không phân biệt hoa thường trên các trường: FullName, UserName và Email. |
| **T94** | Kết quả tìm kiếm trả về tối đa 50 bản ghi. |
| **T95** | Kết quả tìm kiếm trả về UserResponseDto, tuyệt đối không chứa các trường nhạy cảm (PasswordHash, SecurityStamp). |

---

## 13. TẠO BÀI VIẾT (Create Post)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T96** | Yêu cầu phải đăng nhập (Authorize). |
| **T97** | Nội dung bài viết (Content) không được để trống. |
| **T98** | Nội dung bài viết phải có ít nhất 1 ký tự. |
| **T99** | Nội dung bài viết không được vượt quá 5000 ký tự. |
| **T100** | ImageUrl (nếu có) phải là URL hợp lệ bắt đầu bằng http:// hoặc https://. |
| **T101** | Ảnh đính kèm phải là định dạng hợp lệ: .jpg, .jpeg, .png, .gif hoặc .webp. |
| **T102** | Nếu đăng bài trong nhóm (GroupId có giá trị), nhóm phải tồn tại trong cơ sở dữ liệu. |
| **T103** | Nếu đăng bài trong nhóm, người dùng bắt buộc phải là thành viên hợp lệ của nhóm đó. |
| **T104** | Đăng bài thành công trả về HTTP 201 Created. |
| **T105** | Bài viết mới trên Feed phát sự kiện SignalR "PostCreated" đến group "feed". |
| **T106** | Bài viết mới trong Nhóm phát sự kiện SignalR "GroupPostCreated" đến group "group_{groupId}". |
| **T107** | Nếu bài viết là bài công khai (không thuộc nhóm, không phải chia sẻ), hệ thống gửi thông báo đến tất cả bạn bè đã Accepted. |

---

## 14. XEM BẢNG TIN (Get News Feed)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T108** | Yêu cầu phải đăng nhập (Authorize). |
| **T109** | Bảng tin chỉ hiển thị bài viết công khai không thuộc nhóm nào (GroupId == null). |
| **T110** | Bảng tin chỉ hiển thị bài viết chưa bị xóa (IsDeleted == false). |
| **T111** | Tham số page tự động điều chỉnh tối thiểu bằng 1 (page = Math.Max(1, page)). |
| **T112** | Tham số pageSize nằm trong khoảng 1 đến 100 (tự động điều chỉnh nếu nằm ngoài). |
| **T113** | Bài viết được sắp xếp theo thời gian tạo mới nhất lên đầu (CreatedAt giảm dần). |

---

## 15. CHIA SẺ BÀI VIẾT (Share Post)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T114** | Yêu cầu phải đăng nhập (Authorize). |
| **T115** | Bài viết gốc (postId) phải tồn tại trong cơ sở dữ liệu và chưa bị xóa. Nếu không → HTTP 404 ("Original post not found"). |
| **T116** | Caption chia sẻ là tùy chọn (có thể để trống, mặc định là chuỗi rỗng). |
| **T117** | Bài chia sẻ không tạo ảnh riêng (ImageUrl = null), chỉ tham chiếu đến bài gốc qua SharedPostId. |
| **T118** | Chia sẻ thành công trả về HTTP 201 Created. |
| **T119** | Chia sẻ thành công gửi thông báo đến tác giả bài gốc (nếu người chia sẻ khác tác giả gốc). |
| **T120** | Chia sẻ thành công phát sự kiện SignalR "PostCreated" hoặc "GroupPostCreated". |

---

## 16. XÓA BÀI VIẾT (Delete Post)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T121** | Yêu cầu phải đăng nhập (Authorize). |
| **T122** | Bài viết phải tồn tại trong cơ sở dữ liệu và chưa bị xóa. Nếu không → HTTP 404. |
| **T123** | Chỉ tác giả của bài viết mới được phép xóa (post.UserId == currentUserId). |
| **T124** | Người khác cố xóa bài viết không phải của mình → HTTP 403 Forbidden ("You cannot delete this post"). |
| **T125** | Xóa bài viết thực hiện Soft Delete (IsDeleted = true, UpdatedAt = UtcNow), không xóa vĩnh viễn khỏi database. |
| **T126** | Ảnh đính kèm bài viết (nếu có) bị xóa khỏi ổ đĩa máy chủ. |
| **T127** | Nếu bài viết thuộc nhóm, phát sự kiện SignalR "GroupPostDeleted". |
| **T128** | Hệ thống KHÔNG cung cấp API chỉnh sửa bài viết (không có PUT/PATCH) để bảo toàn tính toàn vẹn nội dung. |

---

## 17. TẠO BÌNH LUẬN (Create Comment)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T129** | Yêu cầu phải đăng nhập (Authorize). |
| **T130** | Nội dung bình luận (Content) không được để trống. |
| **T131** | Nội dung bình luận phải có ít nhất 1 ký tự. |
| **T132** | Nội dung bình luận không được vượt quá 1000 ký tự. |
| **T133** | PostId phải lớn hơn 0. |
| **T134** | PostId phải tồn tại trong cơ sở dữ liệu (bài viết chưa bị xóa). |
| **T135** | Bình luận mới trả về HTTP 201 Created. |
| **T136** | Bình luận mới phát sự kiện SignalR "ReceiveCommentCreated" đến nhóm "comments-{postId}" (CommentHub). |
| **T137** | Bình luận mới phát sự kiện SignalR "CommentAdded" đến nhóm "feed" (PostHub). |
| **T138** | Nếu bài viết thuộc nhóm, phát thêm sự kiện "GroupCommentAdded" đến "group_{groupId}". |
| **T139** | Gửi thông báo đến chủ bài viết (nếu người bình luận không phải chủ bài viết). Nội dung thông báo chứa 50 ký tự đầu tiên của bình luận. |

---

## 18. SỬA BÌNH LUẬN (Update Comment)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T140** | Yêu cầu phải đăng nhập (Authorize). |
| **T141** | Bình luận phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 404. |
| **T142** | Chỉ tác giả bình luận mới được phép sửa (comment.UserId == currentUserId). |
| **T143** | Người khác cố sửa bình luận không phải của mình → HTTP 403 Forbidden ("You cannot update this comment"). |
| **T144** | Nội dung sửa (Content) không được để trống. |
| **T145** | Nội dung sửa phải có ít nhất 1 ký tự. |
| **T146** | Nội dung sửa không được vượt quá 1000 ký tự. |
| **T147** | Sửa bình luận thành công phát sự kiện SignalR "ReceiveCommentUpdated", "CommentUpdated" và "GroupCommentUpdated" (nếu bài thuộc nhóm). |

---

## 19. XÓA BÌNH LUẬN (Delete Comment)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T148** | Yêu cầu phải đăng nhập (Authorize). |
| **T149** | Bình luận phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 404. |
| **T150** | Chỉ tác giả bình luận mới được phép xóa (comment.UserId == currentUserId). |
| **T151** | Người khác cố xóa bình luận không phải của mình → HTTP 403 Forbidden ("You cannot delete this comment"). |
| **T152** | Xóa bình luận là xóa vĩnh viễn (Hard Delete) khỏi cơ sở dữ liệu, không phải Soft Delete. |
| **T153** | Xóa bình luận phát sự kiện SignalR "ReceiveCommentDeleted", "CommentDeleted" và "GroupCommentDeleted" (nếu bài thuộc nhóm). |

---

## 20. THÍCH BÀI VIẾT (Like Post)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T154** | Yêu cầu phải đăng nhập (Authorize). |
| **T155** | PostId phải lớn hơn 0. |
| **T156** | Bài viết (PostId) phải tồn tại trong cơ sở dữ liệu. |
| **T157** | Mỗi người dùng chỉ được thích 1 bài viết đúng 1 lần (ràng buộc Unique Index trên cặp UserId + PostId). |
| **T158** | Nếu người dùng đã thích bài viết rồi mà thích lại → database từ chối (DbUpdateException). |
| **T159** | Thích bài viết thành công trả về HTTP 201 Created. |
| **T160** | Thích bài viết phát sự kiện SignalR "PostLiked" đến nhóm "feed". |
| **T161** | Nếu bài viết thuộc nhóm, phát thêm sự kiện "GroupPostLiked" đến "group_{groupId}". |
| **T162** | Gửi thông báo đến chủ bài viết (nếu người thích không phải chủ bài viết). |

---

## 21. BỎ THÍCH BÀI VIẾT (Unlike Post)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T163** | Yêu cầu phải đăng nhập (Authorize). |
| **T164** | Bản ghi Like phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 404 ("Like not found"). |
| **T165** | Chỉ người dùng đã thích mới được phép bỏ thích (like.UserId == currentUserId). |
| **T166** | Đối với API DELETE /api/Likes/post/{postId}/user/{userId}: userId trong URL phải trùng với currentUserId. |
| **T167** | Người khác cố bỏ thích thay sẽ bị từ chối → HTTP 403 Forbidden ("You cannot delete likes for other users"). |
| **T168** | Bỏ thích là xóa vĩnh viễn (Hard Delete) bản ghi Like khỏi cơ sở dữ liệu. |
| **T169** | Bỏ thích phát sự kiện SignalR "PostUnliked" đến nhóm "feed". |
| **T170** | Nếu bài viết thuộc nhóm, phát thêm sự kiện "GroupPostUnliked" đến "group_{groupId}". |

---

## 22. GỬI LỜI MỜI KẾT BẠN (Send Friend Request)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T171** | Yêu cầu phải đăng nhập (Authorize). |
| **T172** | FriendId không được để trống. |
| **T173** | Người dùng không thể tự gửi lời mời kết bạn cho chính mình (senderId != receiverId). Nếu vi phạm → HTTP 400 ("Không thể gửi lời mời kết bạn cho chính mình"). |
| **T174** | Không được gửi lời mời nếu đã tồn tại quan hệ (cả hai chiều) giữa hai người dùng. Nếu vi phạm → HTTP 400 ("Lời mời kết bạn đã tồn tại hoặc đã là bạn bè"). |
| **T175** | Lời mời kết bạn mới khởi tạo với trạng thái Pending (0). |
| **T176** | Gửi thành công trả về HTTP 201 Created. |
| **T177** | Gửi thông báo đến người nhận lời mời (NotificationType.FriendRequest). |
| **T178** | Phát sự kiện SignalR "FriendRequestReceived" đến nhóm "notifications-{friendId}". |

---

## 23. CHẤP NHẬN LỜI MỜI KẾT BẠN (Accept Friend Request)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T179** | Yêu cầu phải đăng nhập (Authorize). |
| **T180** | Lời mời kết bạn phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 400 ("Lời mời kết bạn không tồn tại"). |
| **T181** | Lời mời phải đang ở trạng thái Pending. Nếu không → HTTP 400 ("Lời mời kết bạn không ở trạng thái chờ xử lý"). |
| **T182** | Chỉ người nhận lời mời (FriendId) mới có quyền chấp nhận. Nếu vi phạm → HTTP 400 ("Bạn không có quyền chấp nhận lời mời này"). |
| **T183** | Chấp nhận thay đổi trạng thái sang Accepted (1) và cập nhật UpdatedAt. |
| **T184** | Gửi thông báo đến người gửi lời mời (NotificationType.FriendRequestAccepted). |
| **T185** | Phát sự kiện SignalR "FriendRequestAccepted" đến nhóm "notifications-{userId}". |

---

## 24. TỪ CHỐI LỜI MỜI KẾT BẠN (Decline Friend Request)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T186** | Yêu cầu phải đăng nhập (Authorize). |
| **T187** | Lời mời kết bạn phải tồn tại trong cơ sở dữ liệu. |
| **T188** | Lời mời phải đang ở trạng thái Pending. |
| **T189** | Chỉ người nhận lời mời (FriendId) mới có quyền từ chối. |
| **T190** | Từ chối thay đổi trạng thái sang Declined (2) và cập nhật UpdatedAt. |
| **T191** | Phát sự kiện SignalR "FriendRequestDeclined". |

---

## 25. XÓA BẠN BÈ (Remove Friend)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T192** | Yêu cầu phải đăng nhập (Authorize). |
| **T193** | Quan hệ bạn bè phải tồn tại giữa hai người dùng (kiểm tra cả hai chiều). |
| **T194** | Quan hệ phải đang ở trạng thái Accepted mới được phép xóa. |
| **T195** | Nếu không tồn tại hoặc không ở trạng thái Accepted → HTTP 404 ("Friend not found or not in accepted state"). |

---

## 26. CHẶN NGƯỜI DÙNG (Block User)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T196** | Yêu cầu phải đăng nhập (Authorize). |
| **T197** | Nếu đã có quan hệ trước đó giữa hai người → cập nhật trạng thái sang Blocked (3). |
| **T198** | Nếu chưa có quan hệ → tạo bản ghi mới với trạng thái Blocked (3). |
| **T199** | Khi bị chặn, mọi tương tác (gửi lời mời, nhắn tin, xem trạng thái) giữa hai người đều bị vô hiệu hóa. |

---

## 27. GỬI TIN NHẮN (Send Message)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T200** | Yêu cầu phải đăng nhập (Authorize). |
| **T201** | Nội dung tin nhắn (Content) không được để trống. Nếu trống → HTTP 400 ("Message content is required"). |
| **T202** | Tin nhắn cá nhân (không có GroupId): ReceiverId là bắt buộc. Nếu trống → HTTP 400 ("ReceiverId is required for personal messages"). |
| **T203** | Tin nhắn nhóm (có GroupId): Nhóm phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 404 ("Group not found"). |
| **T204** | Tin nhắn nhóm: Người gửi phải là thành viên hợp lệ của nhóm. Nếu không → HTTP 400 ("You are not a member of this group"). |
| **T205** | Gửi tin nhắn thành công trả về HTTP 201 Created. |
| **T206** | Tin nhắn cá nhân được phát qua SignalR "ReceiveMessage" đến cả 3 nhóm: conversation_{min}_{max}, user_{receiverId}, user_{senderId}. |
| **T207** | Tin nhắn nhóm được phát qua SignalR "ReceiveMessage" đến user_{memberId} của từng thành viên nhóm. |
| **T208** | Gửi thông báo đến người nhận nếu tin nhắn cá nhân và người gửi khác người nhận. Nội dung thông báo chứa 50 ký tự đầu tiên. |

---

## 28. XEM HỘI THOẠI (View Conversation)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T209** | Yêu cầu phải đăng nhập (Authorize). |
| **T210** | Không thể xem hội thoại với chính mình. Nếu vi phạm → HTTP 400 ("Cannot view conversation with yourself"). |
| **T211** | Tham số page tự động điều chỉnh tối thiểu bằng 1. |
| **T212** | Tham số pageSize nằm trong khoảng 1 đến 100. |

---

## 29. XEM TIN NHẮN NHÓM (View Group Messages)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T213** | Yêu cầu phải đăng nhập (Authorize). |
| **T214** | Nhóm phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 404 ("Group not found"). |
| **T215** | Người xem phải là thành viên của nhóm. Nếu không → HTTP 400 ("You are not a member of this group"). |

---

## 30. ĐÁNH DẤU TIN NHẮN ĐÃ ĐỌC (Mark Message as Read)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T216** | Yêu cầu phải đăng nhập (Authorize). |
| **T217** | Chỉ người nhận tin nhắn mới được đánh dấu đã đọc (message.ReceiverId == currentUserId). |
| **T218** | Nếu người dùng không phải người nhận → hệ thống bỏ qua âm thầm (không báo lỗi, không cập nhật). |

---

## 31. THAM GIA PHÒNG CHAT SIGNALR (MessageHub Join Rules)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T219** | Chat 1-1 (JoinConversation): Hai người dùng bắt buộc phải có quan hệ bạn bè ở trạng thái Accepted. |
| **T220** | Chat nhóm (JoinGroupConversation): Người dùng phải là thành viên hợp lệ của nhóm. |
| **T221** | Nếu không đủ điều kiện, hệ thống từ chối âm thầm (return, không gửi lỗi). |
| **T222** | Khi kết nối mới đầu tiên được thiết lập → phát "UserOnline" đến tất cả bạn bè. |
| **T223** | Khi kết nối cuối cùng bị ngắt → phát "UserOffline" kèm LastSeenAt đến tất cả bạn bè. |

---

## 32. TẠO NHÓM (Create Group)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T224** | Yêu cầu phải đăng nhập (Authorize). |
| **T225** | Tên nhóm (Name) không được để trống. Nếu trống → HTTP 400 ("Group name is required"). |
| **T226** | Hệ thống tự động tạo URL Slug duy nhất từ tên nhóm (chuyển thường, bỏ dấu, thay khoảng trắng bằng dấu gạch ngang). |
| **T227** | Nếu Slug đã tồn tại, hệ thống tự động thêm hậu tố số (-1, -2, -3...) cho đến khi duy nhất. |
| **T228** | Người tạo nhóm tự động được thêm làm thành viên đầu tiên (CreatorId) trong bảng GroupMemberships. |
| **T229** | Tất cả thành viên được mời (MemberIds) phải là bạn bè đã Accepted của người tạo. |
| **T230** | Nếu mời người không phải bạn bè → HTTP 400 ("User {memberId} is not a friend of the creator"). |
| **T231** | Tạo nhóm thành công trả về HTTP 201 Created. |
| **T232** | Phát sự kiện SignalR "GroupCreated" đến tất cả client (Clients.All). |

---

## 33. THAM GIA NHÓM (Join Group)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T233** | Yêu cầu phải đăng nhập (Authorize). |
| **T234** | Nhóm phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 404 ("Group not found"). |
| **T235** | Nếu đã là thành viên → thao tác vẫn thành công (idempotent, không tạo bản ghi trùng). |
| **T236** | Phát sự kiện SignalR "GroupMemberCountUpdated" kèm số thành viên mới. |

---

## 34. RỜI NHÓM (Leave Group)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T237** | Yêu cầu phải đăng nhập (Authorize). |
| **T238** | Người dùng phải đang là thành viên của nhóm mới được rời. |
| **T239** | Nếu không phải thành viên → HTTP 404 ("Group membership not found"). |
| **T240** | Phát sự kiện SignalR "GroupMemberCountUpdated" kèm số thành viên mới. |

---

## 35. TẠO KHOẢNH KHẮC 24H (Create Story)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T241** | Yêu cầu phải đăng nhập (Authorize). |
| **T242** | Story phải có ít nhất ImageUrl hoặc Content (không được trống cả hai). |
| **T243** | Content (nếu có) không được vượt quá 500 ký tự. |
| **T244** | ImageUrl (nếu có) phải là URL hợp lệ bắt đầu bằng http:// hoặc https://. |
| **T245** | Ảnh đính kèm phải thuộc định dạng hợp lệ: .jpg, .jpeg, .png, .gif hoặc .webp. |
| **T246** | ExpireAt phải là thời gian trong tương lai (> DateTime.Now). |
| **T247** | Thời hạn tồn tại mặc định của Story là đúng 24 giờ kể từ thời điểm tạo (ExpireAt = CreatedAt + 24h). |
| **T248** | Tạo Story thành công trả về HTTP 201 Created. |
| **T249** | Phát sự kiện SignalR "StoryCreated" đến tác giả và tất cả bạn bè đã Accepted (nhóm "story-user-{userId}"). |

---

## 36. XEM KHOẢNH KHẮC (View Stories)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T250** | Yêu cầu phải đăng nhập (Authorize). |
| **T251** | Chỉ được xem Story của chính mình hoặc bạn bè đã có trạng thái Accepted. |
| **T252** | Xem Story của người không phải bạn bè → HTTP 403 Forbidden ("You can only view stories from friends"). |
| **T253** | Story đã quá 24 giờ (ExpireAt < UtcNow) tự động bị ẩn khỏi danh sách hiển thị. |

---

## 37. XÓA KHOẢNH KHẮC (Delete Story)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T254** | Yêu cầu phải đăng nhập (Authorize). |
| **T255** | Story phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 404 ("Story not found"). |
| **T256** | Chỉ chủ sở hữu Story mới được phép xóa (story.UserId == currentUserId). |
| **T257** | Người khác cố xóa → HTTP 403 Forbidden. |
| **T258** | Ảnh đính kèm Story (nếu có) bị xóa khỏi ổ đĩa máy chủ (bao gồm kiểm tra path traversal). |
| **T259** | Xóa Story thành công trả về HTTP 204 No Content. |
| **T260** | Phát sự kiện SignalR "StoryDeleted" đến tác giả và bạn bè. |

---

## 38. TẠO HASHTAG (Create Hashtag)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T261** | Yêu cầu phải đăng nhập (Authorize). |
| **T262** | Tên hashtag (Name) không được để trống. |
| **T263** | Tên hashtag phải có ít nhất 1 ký tự. |
| **T264** | Tên hashtag không được vượt quá 100 ký tự. |
| **T265** | Tên hashtag chỉ được chứa chữ cái (a-z, A-Z), số (0-9) và dấu gạch dưới (_). |
| **T266** | Tên hashtag là duy nhất trong bảng Hashtags. |
| **T267** | Trên giao diện client, hashtag được render dưới dạng liên kết bấm được (Component HashtagContent). |

---

## 39. XEM THÔNG BÁO (View Notifications)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T268** | Yêu cầu phải đăng nhập (Authorize). |
| **T269** | Chỉ chủ tài khoản mới được xem thông báo của mình (userId route == currentUserId). |
| **T270** | Người khác cố truy cập thông báo không phải của mình → HTTP 403 Forbidden. |
| **T271** | Danh sách thông báo trả về tối đa 50 bản ghi, sắp xếp theo CreatedAt giảm dần (mới nhất lên đầu). |
| **T272** | Danh sách thông báo chưa đọc trả về tối đa 100 bản ghi, lọc theo IsRead == false. |

---

## 40. ĐÁNH DẤU THÔNG BÁO ĐÃ ĐỌC (Mark Notification as Read)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T273** | Yêu cầu phải đăng nhập (Authorize). |
| **T274** | Thông báo phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 404. |
| **T275** | Chỉ chủ sở hữu thông báo mới được đánh dấu đã đọc. Người khác → HTTP 403. |
| **T276** | Đánh dấu đã đọc từng thông báo: Cập nhật IsRead = true cho 1 thông báo cụ thể. |
| **T277** | Đánh dấu đã đọc tất cả (mark-all-read): Cập nhật toàn bộ thông báo chưa đọc của người dùng thành IsRead = true. |
| **T278** | Đánh dấu Feed đã đọc (mark-feed-read): Cập nhật tất cả thông báo chưa đọc thành IsRead = true, NGOẠI TRỪ thông báo loại Message (Type != Message), giữ nguyên badge tin nhắn. |

---

## 41. XÓA THÔNG BÁO (Delete Notifications)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T279** | Yêu cầu phải đăng nhập (Authorize). |
| **T280** | Thông báo phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 404. |
| **T281** | Chỉ chủ sở hữu thông báo mới được xóa. Người khác → HTTP 403. |
| **T282** | Xóa theo loại (DELETE /api/Notifications/user/{userId}/by-type/{type}): Type phải là giá trị Enum hợp lệ trong khoảng 0-10 (FriendRequest, FriendRequestAccepted, Like, Comment, CommentReply, Follow, PostReport, Message, System, FriendPublishedPost, PostShared). |
| **T283** | Type không hợp lệ → HTTP 400 Bad Request ("Invalid notification type: {type}"). |

---

## 42. BÁO CÁO BÀI VIẾT VI PHẠM (Create Post Report)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T284** | Yêu cầu phải đăng nhập (Authorize). |
| **T285** | PostId phải lớn hơn 0. |
| **T286** | Bài viết (PostId) phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 404 ("Post not found"). |
| **T287** | Người dùng không thể tự báo cáo bài viết của chính mình (post.UserId != currentUserId). Nếu vi phạm → HTTP 400 ("Không thể báo cáo bài viết của chính mình"). |
| **T288** | Một người dùng không được tạo nhiều hơn 1 báo cáo ở trạng thái Pending cho cùng một bài viết. Nếu vi phạm → HTTP 400 ("Bạn đã báo cáo bài viết này rồi"). |
| **T289** | Lý do báo cáo (Reason) phải thuộc 1 trong 7 giá trị Enum: HarmfulContent (0), Spam (1), Harassment (2), ViolentContent (3), AdultContent (4), FakeNews (5), Other (6). |
| **T290** | Chi tiết bổ sung (Detail) không được vượt quá 500 ký tự. |
| **T291** | Báo cáo mới tạo mặc định mang trạng thái Pending (0). |
| **T292** | Tạo báo cáo thành công trả về HTTP 201 Created với thông báo "Báo cáo thành công". |

---

## 43. PHÊ DUYỆT BÁO CÁO VI PHẠM (Admin Approve Report)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T293** | Yêu cầu phải có vai trò Admin (Authorize Roles = "Admin"). |
| **T294** | Báo cáo phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 404 ("Report not found"). |
| **T295** | Báo cáo phải đang ở trạng thái Pending (chưa được xử lý). |
| **T296** | Báo cáo đã được xử lý trước đó → HTTP 400 ("Report đã được xử lý rồi"). |
| **T297** | Phê duyệt thay đổi trạng thái báo cáo sang ApprovedViolation (1), ghi nhận ReviewedByAdminId và ReviewedAt. |
| **T298** | Hệ thống bắt buộc ghi vết kiểm toán vào bảng PostDeletionLog với đầy đủ các trường: PostId, UserId, Content, ImageUrl, GroupId, ReportId, Reason, DeletedAt, DeletedByAdminId. |
| **T299** | Hệ thống gửi thông báo hệ thống (NotificationType.System) đến tác giả bài viết: "Bài viết của bạn đã bị gỡ xuống vì: {Reason}". |
| **T300** | Hệ thống xóa bài viết vi phạm (Soft Delete: IsDeleted = true). |
| **T301** | Thứ tự thực hiện bắt buộc: (1) Cập nhật trạng thái báo cáo → (2) Ghi PostDeletionLog → (3) Gửi thông báo → (4) Xóa bài viết. |

---

## 44. TỪ CHỐI BÁO CÁO (Admin Reject Report)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T302** | Yêu cầu phải có vai trò Admin (Authorize Roles = "Admin"). |
| **T303** | Báo cáo phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 404 ("Report not found"). |
| **T304** | Báo cáo phải đang ở trạng thái Pending. |
| **T305** | Báo cáo đã được xử lý trước đó → HTTP 400 ("Report đã được xử lý rồi"). |
| **T306** | Từ chối thay đổi trạng thái báo cáo sang Rejected (2), ghi nhận ReviewedByAdminId và ReviewedAt. |
| **T307** | Bài viết bị báo cáo được giữ nguyên, hoàn toàn không bị ảnh hưởng. |

---

## 45. XÓA BÁO CÁO (Admin Delete Report)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T308** | Yêu cầu phải có vai trò Admin (Authorize Roles = "Admin"). |
| **T309** | Báo cáo phải tồn tại trong cơ sở dữ liệu. Nếu không → HTTP 404. |
| **T310** | Trước khi xóa báo cáo, hệ thống phải xóa tất cả PostDeletionLog liên quan (FK constraint) để tránh lỗi khóa ngoại. |
| **T311** | Sau khi xóa báo cáo, nếu bài viết liên quan không còn báo cáo nào khác và đã bị Soft Delete → hệ thống xóa vĩnh viễn bài viết (Hard Delete). |

---

## 46. KẾT NỐI THỜI GIAN THỰC & BẢO MẬT HẠ TẦNG (SignalR Infrastructure & Security)

| TRD# | Điều kiện ràng buộc kỹ thuật |
|:---:|---|
| **T312** | Mọi kết nối WebSocket đến các SignalR Hub bắt buộc phải truyền kèm JWT Token hợp lệ qua query string access_token hoặc Authorization header. |
| **T313** | Phía Client bắt buộc phải cấu hình cơ chế tự động kết nối lại (withAutomaticReconnect()) khi mất kết nối mạng tạm thời. |
| **T314** | Hệ thống cấu hình chính sách CORS nghiêm ngặt (AllowLocalhost), chỉ cho phép các Origin hợp lệ: localhost:3000, localhost:3001, localhost:5142. |
| **T315** | Phía giao diện Frontend bắt buộc áp dụng cơ chế Debounce tối thiểu 300ms trên tất cả ô nhập tìm kiếm để giảm tải request lên máy chủ. |
| **T316** | Hệ thống bảo vệ chống Path Traversal khi xóa ảnh: kiểm tra đường dẫn file nằm trong thư mục uploads/ trước khi xóa. |
| **T317** | Mọi ảnh tải lên (avatar, bài viết, story) đều được đặt tên bằng GUID duy nhất để tránh trùng lặp và đoán tên file. |

---

**Tổng cộng: 317 điều kiện ràng buộc kỹ thuật (T01 → T317) cho 46 nhóm chức năng.**
