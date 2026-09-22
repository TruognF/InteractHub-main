# 📁 InteractHub - Giải Thích Chi Tiết Cấu Trúc Thư Mục

**Tài Liệu:** Hướng dẫn toàn bộ cấu trúc thư mục dự án  
**Cập nhật:** 12 Tháng 5, 2026  
**Đối tượng:** Developers, Maintainers

---

## 🌳 Cây Cấu Trúc Toàn Bộ

```
InteractHub/
│
├─ 📁 backend/                          ← C# .NET Core Backend
│  └─ 📁 InteractHub.API/               ← API Layer (Presentation)
│  └─ 📁 InteractHub.Application/       ← Application Layer (Business Logic)
│  └─ 📁 InteractHub.Infrastructure/    ← Infrastructure Layer (Data Access)
│  └─ 📁 InteractHub.Tests/             ← Unit Tests
│  └─ InteractHub.sln                   ← Solution file
│
├─ 📁 frontend/                         ← React Vite Frontend
│  ├─ 📁 src/                           ← Source code
│  ├─ 📁 public/                        ← Static assets
│  ├─ vite.config.js
│  ├─ package.json
│  └─ index.html
│
└─ 📁 root/                             ← Project root files
   ├─ docker-compose.yml
   ├─ package.json
   ├─ SYSTEM_ARCHITECTURE.md            ← Tài liệu kiến trúc chi tiết
   ├─ FOLDER_STRUCTURE_EXPLAINED.md     ← File này
   └─ Các file tài liệu khác
```

---

## 🔧 Backend Structure (`backend/`)

### 📍 Location: `backend/`

Backend được chia thành **4 projects .NET Core** với mục đích riêng biệt:

```
backend/
├─ InteractHub.API/                 ← Controllers, DTOs, Middleware
├─ InteractHub.Application/         ← Entities, Interfaces, Constants
├─ InteractHub.Infrastructure/      ← Services, Database, Hubs
├─ InteractHub.Tests/               ← Unit Tests
└─ InteractHub.sln                  ← Visual Studio Solution
```

---

## 🎯 Backend Project Details

### 1. **InteractHub.API/** - Presentation Layer (API Endpoints)

**Chức Năng:** Xử lý HTTP requests từ frontend, validate input, trả về responses

**Cấu Trúc:**
```
InteractHub.API/
│
├─ 📁 Controllers/                      (14+ controllers)
│  ├─ AuthController.cs                 - User đăng ký, đăng nhập
│  ├─ AdminAuthController.cs            - Admin đăng nhập
│  ├─ PostsController.cs                - CRUD bài viết
│  ├─ PostReportsController.cs          - Báo cáo bài viết (user) + admin duyệt
│  ├─ UsersController.cs                - Quản lý user profile
│  ├─ CommentsController.cs             - Bình luận
│  ├─ LikesController.cs                - Like/Unlike
│  ├─ StoriesController.cs              - Truyện/Stories
│  ├─ MessagesController.cs             - Tin nhắn (private + group)
│  ├─ GroupsController.cs               - Nhóm
│  ├─ FriendshipsController.cs          - Bạn bè
│  ├─ NotificationsController.cs        - Thông báo
│  ├─ HashtagsController.cs             - Hashtag
│  └─ AdminController.cs                - Quản lý quyền hạn
│
├─ 📁 DTOs/                             (Data Transfer Objects)
│  ├─ AuthDto.cs                        - RegisterDto, LoginDto
│  ├─ UserDto.cs                        - UserResponseDto
│  ├─ PostDto.cs                        - PostResponseDto, CreatePostDto
│  ├─ CommentDto.cs                     - CommentResponseDto
│  ├─ LikeDto.cs                        - LikeResponseDto
│  ├─ StoryDto.cs                       - StoryResponseDto
│  ├─ MessageDto.cs                     - MessageResponseDto
│  ├─ NotificationDto.cs                - NotificationResponseDto
│  ├─ PostReportDto.cs                  - PostReportResponseDto
│  ├─ FriendshipDto.cs                  - FriendshipResponseDto
│  ├─ GroupDto.cs                       - GroupResponseDto
│  ├─ HashtagDto.cs                     - HashtagResponseDto
│  ├─ RoleDto.cs                        - AssignRoleDto, RemoveRoleDto
│  └─ 📁 Response/
│     └─ ApiResponse.cs                 - Standardized API response format
│
├─ 📁 Middleware/                       (HTTP Pipeline Processing)
│  └─ ExceptionHandlingMiddleware.cs    - Global error handling
│
├─ 📁 Extensions/                       (Helper Extensions)
│  ├─ DateTimeExtensions.cs             - Ngày giờ helper
│  ├─ EnumExtensions.cs                 - Enum helper
│  ├─ ErrorHelper.cs                    - Tạo error responses
│  ├─ ResponseExtensions.cs             - Helper tạo API responses
│  └─ StringExtensions.cs               - String utilities
│
├─ 📁 Mapping/                          (Object Mapping)
│  └─ PostReportProfile.cs              - AutoMapper profiles
│
├─ 📁 Serialization/                    (JSON Config)
│  └─ Custom JSON serialization setup
│
├─ 📁 Properties/                       (Project metadata)
│
├─ Program.cs                           ⭐ Entry point, DI setup
│  ├─ DbContext configuration
│  ├─ JWT authentication setup
│  ├─ Service registration
│  ├─ CORS configuration
│  └─ SignalR setup
│
├─ appsettings.json                    (Configuration: DB, JWT, Logging)
├─ appsettings.Development.json        (Development-specific config)
├─ InteractHub.API.csproj              (Project file)
└─ InteractHub.API.http                (REST Client test file)
```

**Chức Năng Chính:**
- ✅ Tiếp nhận HTTP requests (GET, POST, PUT, DELETE)
- ✅ Validate input data (DTOs)
- ✅ Gọi services từ Application layer
- ✅ Trả về standardized API responses
- ✅ Handle errors globally via middleware
- ✅ JWT authentication & role authorization

**Ví dụ Flow:**
```
1. Frontend: POST /api/posts (với token)
2. PostsController.Create() nhận request
3. Validate CreatePostDto
4. Gọi PostService.CreateAsync()
5. Trả về PostResponseDto
6. Response middleware format output
7. Frontend nhận { success: true, data: {...} }
```

---

### 2. **InteractHub.Application/** - Application/Business Logic Layer

**Chức Năng:** Định nghĩa business logic, entities, interfaces, constants

**Cấu Trúc:**
```
InteractHub.Application/
│
├─ 📁 Entities/                         (Domain Models - Map to DB)
│  ├─ User.cs                           - Kế thừa IdentityUser (asp.net core)
│  ├─ Post.cs                           - Bài viết
│  ├─ PostReport.cs                     - Báo cáo bài viết
│  ├─ PostDeletionLog.cs                - Lịch sử xóa bài viết [NEW]
│  ├─ Comment.cs                        - Bình luận
│  ├─ Like.cs                           - Like
│  ├─ Story.cs                          - Truyện
│  ├─ Message.cs                        - Tin nhắn
│  ├─ Notification.cs                   - Thông báo
│  ├─ Friendship.cs                     - Quan hệ bạn bè
│  ├─ Group.cs                          - Nhóm
│  ├─ GroupMembership.cs                - Thành viên nhóm
│  ├─ Hashtag.cs                        - Hashtag
│  ├─ PostHashtag.cs                    - Liên kết Post ↔ Hashtag
│  │
│  └─ 📁 Enums/                         (Enumeration types)
│     ├─ FriendshipStatus.cs            - Pending, Accepted, Declined, Blocked
│     ├─ ReportStatus.cs                - Pending, ApprovedViolation, Rejected
│     ├─ ReportReason.cs                - Spam, Harassment, Inappropriate, Other
│     └─ NotificationType.cs            - Like, Comment, Message, FriendRequest, System
│
├─ 📁 Interfaces/                       (Service Contracts)
│  ├─ IPostService.cs                   - CRUD posts interface
│  ├─ IPostReportService.cs             - CRUD reports interface
│  ├─ IPostDeletionLogService.cs        - CRUD deletion logs interface [NEW]
│  ├─ INotificationService.cs           - Notification operations interface
│  ├─ IUserService.cs                   - User operations interface
│  ├─ ICommentService.cs                - Comment operations interface
│  ├─ ILikeService.cs                   - Like operations interface
│  ├─ IStoryService.cs                  - Story operations interface
│  ├─ IMessageService.cs                - Message operations interface
│  ├─ IFriendshipService.cs             - Friendship operations interface
│  ├─ IGroupService.cs                  - Group operations interface
│  ├─ IHashtagService.cs                - Hashtag operations interface
│  ├─ IFileService.cs                   - File upload interface
│  └─ IUserPresenceService.cs           - Online status tracking interface
│
├─ 📁 Constants/                        (Application-wide Constants)
│  ├─ RoleConstants.cs                  - User = "User", Admin = "Admin"
│  └─ AppErrorCodes.cs                  - Error code definitions
│
├─ 📁 Helpers/                          (Utility Functions)
│  ├─ ValidationHelper.cs               - Email, password, username validation
│  ├─ QueryHelper.cs                    - Query filtering & sanitization
│  └─ PaginationHelper.cs               - Pagination parameter validation
│
└─ InteractHub.Application.csproj       (Project file)
```

**Chức Năng Chính:**
- ✅ Define domain models (Entities) - đại diện cho bảng database
- ✅ Define service interfaces - hợp đồng giữa layers
- ✅ Define enums - kiểu dữ liệu giới hạn
- ✅ Define constants - giá trị không đổi
- ✅ Provide validation helpers
- ✅ Define error codes

**Tại sao cần tách layer này?**
- Độc lập với database implementation
- Độc lập với API implementation
- Dễ testing (mock interfaces)
- Dễ bảo trì (business logic tập trung)

---

### 3. **InteractHub.Infrastructure/** - Infrastructure/Data Access Layer

**Chức Năng:** Triển khai services, kết nối database, SignalR hubs, file storage

**Cấu Trúc:**
```
InteractHub.Infrastructure/
│
├─ 📁 Services/                         (Service Implementations)
│  ├─ PostService.cs                    - Implement IPostService
│  ├─ PostReportService.cs              - Implement IPostReportService
│  ├─ PostDeletionLogService.cs         - Implement IPostDeletionLogService [NEW]
│  ├─ NotificationService.cs            - Implement INotificationService
│  ├─ UserService.cs                    - Implement IUserService
│  ├─ CommentService.cs                 - Implement ICommentService
│  ├─ LikeService.cs                    - Implement ILikeService
│  ├─ StoryService.cs                   - Implement IStoryService
│  ├─ MessageService.cs                 - Implement IMessageService
│  ├─ FriendshipService.cs              - Implement IFriendshipService
│  ├─ GroupService.cs                   - Implement IGroupService
│  ├─ HashtagService.cs                 - Implement IHashtagService
│  ├─ UserPresenceService.cs            - Implement IUserPresenceService
│  └─ CloudinaryService.cs              - Implement IFileService (file upload)
│
├─ 📁 Data/                             (Database Context)
│  ├─ AppDbContext.cs                   ⭐ Entity Framework DbContext
│  │  ├─ DbSet<Post> Posts
│  │  ├─ DbSet<PostReport> PostReports
│  │  ├─ DbSet<PostDeletionLog> PostDeletionLogs [NEW]
│  │  ├─ DbSet<Comment> Comments
│  │  ├─ DbSet<Like> Likes
│  │  ├─ DbSet<Story> Stories
│  │  ├─ DbSet<Message> Messages
│  │  ├─ DbSet<Notification> Notifications
│  │  ├─ DbSet<Friendship> Friendships
│  │  ├─ DbSet<Group> Groups
│  │  ├─ DbSet<GroupMembership> GroupMemberships
│  │  ├─ DbSet<Hashtag> Hashtags
│  │  └─ DbSet<PostHashtag> PostHashtags
│  │
│  └─ OnModelCreating()                - Configure relationships, constraints
│
├─ 📁 Migrations/                       (Database Schema Changes)
│  ├─ 20260511190000_InitialCreate.cs  - Tạo bảng ban đầu
│  ├─ ...
│  └─ 20260511190555_AddPostDeletionLog.cs [NEW] - Thêm PostDeletionLogs
│     ├─ Tạo table PostDeletionLogs
│     ├─ FK: UserId → AspNetUsers (CASCADE)
│     ├─ FK: DeletedByAdminId → AspNetUsers (NO ACTION)
│     └─ FK: ReportId → PostReports (NO ACTION)
│
├─ 📁 Hubs/                             (SignalR Real-time Hubs)
│  ├─ PostHub.cs                        - Post-related real-time events
│  │  └─ Events: PostCreated, PostDeleted, PostLiked, PostUnliked
│  ├─ StoryHub.cs                       - Story-related events
│  ├─ MessageHub.cs                     - Message real-time delivery
│  ├─ NotificationHub.cs                - Notification push
│  ├─ UserHub.cs                        - User profile updates
│  ├─ CommentHub.cs                     - Comment events
│  └─ GroupHub.cs                       - Group-related events
│
├─ 📁 Configurations/                   (EF Core Configurations)
│  └─ (Entity mapping configurations)
│
└─ InteractHub.Infrastructure.csproj    (Project file)
```

**Chức Năng Chính:**
- ✅ Implement all service interfaces
- ✅ Quản lý database connection qua Entity Framework Core
- ✅ Tạo queries để truy xuất data
- ✅ Quản lý database migrations
- ✅ Setup SignalR hubs cho real-time communication
- ✅ File upload functionality

**Ví dụ PostService:**
```csharp
public class PostService : IPostService
{
    private readonly AppDbContext _context;
    
    public async Task<List<Post>> GetAllAsync()
    {
        return await _context.Posts
            .Include(p => p.User)           // Eager load user
            .Include(p => p.Likes)          // Eager load likes
            .Include(p => p.Comments)       // Eager load comments
            .ToListAsync();                 // Execute SQL query
    }
}
```

---

### 4. **InteractHub.Tests/** - Unit Tests

**Chức Năng:** Kiểm thử các service, helper functions

**Cấu Trúc:**
```
InteractHub.Tests/
│
├─ 📁 Unit/                             (Unit Tests)
│  ├─ PostServiceTests.cs               - Test PostService methods
│  ├─ UserServiceTests.cs               - Test UserService methods
│  └─ (Other service tests)
│
├─ 📁 Common/                           (Test Helpers)
│  └─ (Test fixtures, mocks, utilities)
│
└─ InteractHub.Tests.csproj             (Test project file)
```

**Chức Năng Chính:**
- ✅ Unit tests cho services
- ✅ Validation helpers test
- ✅ Mock database access
- ✅ Integration test setup

---

## 💻 Frontend Structure (`frontend/`)

### 📍 Location: `frontend/`

Frontend là React Vite SPA (Single Page Application)

```
frontend/
│
├─ 📁 src/                              ← Source code (main folder)
│  ├─ 📁 pages/                         ← Top-level pages (routes)
│  ├─ 📁 components/                    ← Reusable UI components
│  ├─ 📁 contexts/                      ← React context (state)
│  ├─ 📁 utils/                         ← Utility functions & API
│  ├─ 📁 styles/                        ← CSS stylesheets
│  ├─ 📁 assets/                        ← Images, icons, fonts
│  ├─ App.jsx                           ← Main app component + router
│  ├─ main.jsx                          ← Entry point
│  └─ App.css                           ← Global styles
│
├─ 📁 public/                           ← Static files (served as-is)
│
├─ index.html                           ← HTML template
├─ vite.config.js                       ← Vite build configuration
├─ package.json                         ← Dependencies & scripts
└─ README.md                            ← Frontend documentation
```

---

## 📄 Frontend Folder Details

### 1. **src/pages/** - Top-level Pages (Routes)

**Chức Năng:** Mỗi file = 1 route/page (điều hướng chính)

```
src/pages/
│
├─ LoginPage.jsx                        - 🔓 User login page
│  └─ Route: /login
│  └─ Components: Login form, validation
│
├─ RegisterPage.jsx                    - 📝 User registration
│  └─ Route: /register
│  └─ Components: Register form, validation
│
├─ HomePage.jsx                        - 🏠 Main feed (posts)
│  └─ Route: / (protected)
│  └─ Features: List posts, real-time updates
│
├─ GroupPage.jsx                       - 👥 Groups list
│  └─ Route: /groups
│  └─ Features: Create group, join/leave
│
├─ GroupDetailPage.jsx                - 🔍 Single group detail
│  └─ Route: /groups/:slug
│  └─ Features: Group posts, members
│
├─ CreateGroupPage.jsx                - ✏️ Create new group
│  └─ Route: /groups/create
│  └─ Components: Form, member selector
│
├─ MessagePage.jsx                    - 💬 Messaging (DMs + groups)
│  └─ Route: /messages
│  └─ Features: Conversations, real-time chat
│
├─ ProfilePage.jsx                    - 👤 Current user profile
│  └─ Route: /profile
│  └─ Features: User info, edit profile, user's posts
│
├─ UserProfilePage.jsx                - 👤 Other user's profile
│  └─ Route: /users/:userId
│  └─ Features: View user info, posts, follow
│
├─ PostDetailPage.jsx                 - 📰 Single post detail
│  └─ Route: /posts/:postId
│  └─ Features: Post content, comments, likes
│
├─ StoryPage.jsx                      - 📖 Stories (temporary posts)
│  └─ Route: /stories
│  └─ Features: View stories, create story
│
├─ AdminLoginPage.jsx                 - 🔐 Admin login
│  └─ Route: /admin/login
│  └─ Features: Admin authentication
│
└─ AdminReportsPage.jsx               - 📋 Admin: Report management
   └─ Route: /admin/reports
   └─ Features: View pending reports, approve/reject
   └─ Components: ReportDetailModal for actions
```

**Page Hierarchy:**
```
Public (No Auth Required):
├─ /login
├─ /register
└─ /admin/login

Protected (User Auth):
├─ /
├─ /groups
├─ /groups/create
├─ /groups/:slug
├─ /messages
├─ /profile
├─ /users/:userId
├─ /posts/:postId
└─ /stories

Admin Protected (Admin Auth):
└─ /admin/reports
```

---

### 2. **src/components/** - Reusable UI Components

**Chức Năng:** Tái sử dụng components, không phải toàn trang

```
src/components/
│
├─ ReportDetailModal.jsx              ⭐ [NEW] Modal for report details
│  └─ Props:
│     - report: PostReportResponseDto
│     - isOpen: boolean
│     - onClose, onApprove, onReject callbacks
│  └─ Features:
│     - Load & display post content
│     - Show report details
│     - Approve/Reject buttons
│     - Error handling
│
├─ AdminProtectedRoute.jsx            - Guard for /admin/* routes
│  └─ Checks: adminToken in localStorage
│  └─ Redirects to /admin/login if not authenticated
│
├─ AdminSidebar.jsx                   - Admin navigation sidebar
│  └─ Routes:
│     - /admin/reports (📋 Báo cáo)
│  └─ [REMOVED] Dashboard
│
├─ NotificationHubBridge.jsx          - SignalR integration component
│  └─ Connects to NotificationHub
│  └─ Listens for real-time notifications
│
├─ PostCard.jsx                       - Individual post display
│  └─ Props: post object
│  └─ Features: Like, comment, share, report buttons
│
├─ CommentSection.jsx                 - Comments on a post
│  └─ Props: postId
│  └─ Features: List comments, add comment
│
├─ UserAvatar.jsx                     - User profile picture
│  └─ Props: user object, size
│
├─ LoadingSpinner.jsx                 - Loading indicator
│  └─ Props: size, text
│
└─ ErrorMessage.jsx                   - Error display
   └─ Props: message, onDismiss
```

**Component Hierarchy Example:**
```
HomePage (page)
├─ PostCard (component)
│  ├─ UserAvatar (component)
│  ├─ LikeButton
│  └─ CommentSection (component)
│     └─ CommentItem (component)
└─ LoadingSpinner (component)
```

---

### 3. **src/contexts/** - State Management (React Context)

**Chức Năng:** Global state management sử dụng React Context API

```
src/contexts/
│
├─ AdminAuthContext.jsx               ⭐ Admin authentication state
│  └─ State:
│     - adminToken: string (from localStorage)
│     - adminUser: object
│     - isLoggedIn: boolean
│  └─ Functions:
│     - login(username, password)
│     - logout()
│     - getAdminToken()
│  └─ Usage: Wrap in <AdminAuthProvider> in main.jsx
│
├─ AuthContext.jsx                    - User authentication state
│  └─ State: token, user, isAuthenticated
│  └─ Functions: login(), logout(), register()
│
├─ GroupsContext.jsx                  - Groups list state
│  └─ State: groups[], loading, error
│  └─ Functions: fetchGroups(), createGroup(), joinGroup()
│
└─ (Other context files as needed)
```

**Context Usage Pattern:**
```jsx
// App.jsx - Wrap providers
<AdminAuthProvider>
  <AuthProvider>
    <GroupsProvider>
      <Router>
        {/* Routes */}
      </Router>
    </GroupsProvider>
  </AuthProvider>
</AdminAuthProvider>

// In component - Use context
const { adminToken, adminUser } = useContext(AdminAuthContext);
```

---

### 4. **src/utils/** - Utility Functions & API

**Chức Năng:** API calls, helpers, WebSocket connections

```
src/utils/
│
├─ api.js                              ⭐ Centralized API communication
│  ├─ Non-Admin Functions:
│  │  ├─ login(userName, password)
│  │  ├─ register(userData)
│  │  ├─ getPosts(page, pageSize)
│  │  ├─ getPostById(postId)
│  │  ├─ createPost(content, imageUrl, groupId)
│  │  ├─ deletePost(postId)
│  │  ├─ likePost(postId)
│  │  ├─ unlikePost(postId)
│  │  ├─ getNotifications(userId)
│  │  ├─ getMessages(conversationId)
│  │  └─ sendMessage(content, receiverId)
│  │
│  ├─ Admin Functions:
│  │  ├─ adminLogin(userName, password)
│  │  ├─ getPendingReports()
│  │  ├─ getAllReports(page, pageSize)
│  │  ├─ getReportById(reportId)
│  │  ├─ getPostByIdAsAdmin(postId)
│  │  ├─ approveReport(reportId)
│  │  └─ rejectReport(reportId)
│  │
│  └─ handleResponse(response)        - Universal error handler
│
├─ postHubConnection.js               - PostHub WebSocket (real-time posts)
│  └─ Events: PostCreated, PostLiked, CommentAdded, PostDeleted
│  └─ Functions: startConnection(), stopConnection()
│
├─ storyHubConnection.js              - StoryHub WebSocket
│  └─ Events: StoryCreated, StoryDeleted
│
├─ notificationHubConnection.js       - NotificationHub WebSocket
│  └─ Events: ReceiveNotification
│
├─ messageHubConnection.js            - MessageHub WebSocket
│  └─ Events: ReceiveMessage
│
├─ userHubConnection.js               - UserHub WebSocket
│  └─ Events: UserProfileUpdated
│
├─ groupHubConnection.js              - GroupHub WebSocket
│  └─ Events: GroupCreated, GroupMemberCountUpdated
│
└─ (Other utility files)
```

**API Call Pattern:**
```javascript
// In component
useEffect(() => {
  const fetchData = async () => {
    try {
      const data = await api.getPosts(1, 20);
      setData(data);
    } catch (error) {
      setError(error.message);
    }
  };
  fetchData();
}, []);
```

---

### 5. **src/styles/** - CSS Stylesheets

**Chức Năng:** Component-specific styles (CSS Modules)

```
src/styles/
│
├─ ReportDetailModal.css              ⭐ [NEW] Modal styling
│  ├─ .report-modal-overlay           - Overlay backdrop
│  ├─ .report-modal                   - Modal container
│  ├─ .report-modal-header            - Title bar
│  ├─ .report-modal-body              - Content area
│  ├─ .report-modal-footer            - Buttons area
│  └─ Media queries for responsiveness
│
├─ HomePage.css                       - Home page styles
├─ PostCard.css                       - Post card styles
├─ LoginPage.css                      - Login form styles
│
└─ App.css                            - Global application styles
```

**CSS Module Pattern:**
```jsx
// Component
import styles from './ReportDetailModal.css';

function ReportDetailModal() {
  return (
    <div className={styles['report-modal-overlay']}>
      <div className={styles['report-modal']}>
        {/* Content */}
      </div>
    </div>
  );
}
```

---

### 6. **src/assets/** - Static Files

**Chức Năng:** Images, icons, fonts (không thay đổi)

```
src/assets/
│
├─ 📁 images/
│  ├─ logo.png
│  ├─ default-avatar.png
│  └─ (Other images)
│
├─ 📁 icons/
│  ├─ heart.svg
│  ├─ comment.svg
│  ├─ share.svg
│  └─ (Other icons)
│
└─ 📁 fonts/
   └─ (Custom fonts if needed)
```

---

### 7. **Main Entry Points**

#### **App.jsx** - Main Application Component
```jsx
// Chức năng:
// 1. Setup React Router with all routes
// 2. Check authentication
// 3. Start WebSocket connections
// 4. Token validation

// Routes structure:
// Public:
//   /login, /register, /admin/login
// Protected (user auth required):
//   /, /groups, /messages, /profile, etc.
// Admin Protected (admin auth required):
//   /admin/reports
```

#### **main.jsx** - React Entry Point
```jsx
// Chức năng:
// 1. Wrap app with providers
// 2. Render to DOM

ReactDOM.createRoot(document.getElementById('root')).render(
  <React.StrictMode>
    <AdminAuthProvider>
      <App />
    </AdminAuthProvider>
  </React.StrictMode>
);
```

#### **index.html** - HTML Template
```html
<!-- Contains: -->
<!-- 1. <div id="root"></div> - React mount point -->
<!-- 2. <script> tags -->
<!-- 3. Meta tags -->
```

---

## 📦 Root Level Files & Folders

### 📍 Location: `InteractHub/` (project root)

```
InteractHub/
│
├─ 📁 backend/                         - C# backend
├─ 📁 frontend/                        - React frontend
│
├─ docker-compose.yml                  - Docker container setup
│  └─ Services: SQL Server, API, Frontend
│
├─ package.json                        - Root dependencies
│  └─ Scripts: install, start, dev, build
│
├─ 📄 SYSTEM_ARCHITECTURE.md          ⭐ Architecture documentation
│  └─ Detailed system design
│  └─ API endpoints
│  └─ Workflows
│  └─ Deployment guide
│
├─ 📄 FOLDER_STRUCTURE_EXPLAINED.md   ⭐ This file
│  └─ Folder-by-folder guide
│
├─ 📄 ADMIN_AUTH_SETUP.md             - Admin setup guide
├─ 📄 ADMIN_SYSTEM_GUIDE.md           - Admin operations guide
├─ 📄 CRITICAL_FIXES_REQUIRED.md      - Known issues
├─ 📄 DEPLOY_AZURE.md                 - Azure deployment
│
├─ 📄 TESTING_GUIDE_AND_FIXES.md      - Testing procedures
├─ 📄 UNIT_TEST.md                    - Unit test guide
├─ 📄 DOCKER_QUICK_START.md           - Docker commands
│
├─ 📄 MESSAGING_DEBUG_GUIDE.md        - Message debugging
├─ 📄 SEED_DATA_GUIDE.md              - Database seeding
│
├─ .gitignore                          - Git ignore rules
├─ README.md                           - Project overview
└─ .git/                               - Git repository
```

---

## 🔄 Data Flow: Request ↔ Response

### Example: Admin Approves Report

```
┌─────────────────────────────────────────────────────────┐
│ FRONTEND (React)                                        │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ AdminReportsPage                                        │
│   ├─ Display: List of reports                          │
│   ├─ User Click: "👁️ Xem Trước" button                │
│   │                                                    │
│   ├─ ReportDetailModal opens                           │
│   │   ├─ useEffect: loadPostDetails()                 │
│   │   │  └─ API: getPostByIdAsAdmin(report.PostId)   │
│   │   ├─ Display: Post + Report details               │
│   │   └─ User Click: "Duyệt" button                   │
│   │                                                    │
│   └─ handleApprove()                                   │
│       ├─ API: approveReport(report.Id)                │
│       ├─ Wait for response                            │
│       └─ Update UI on success                         │
│                                                         │
└─────────────────────┬───────────────────────────────────┘
                      │ HTTP POST
                      ↓
┌─────────────────────────────────────────────────────────┐
│ BACKEND (ASP.NET Core)                                  │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ PostReportsController.ApproveReport()                   │
│   ├─ Check: Admin authorization ✅                     │
│   ├─ Load: Report + Post data                          │
│   ├─ Update: Report.Status → ApprovedViolation         │
│   ├─ Save to DB                                        │
│   │                                                    │
│   ├─ Service Layer (Business Logic)                    │
│   │   ├─ PostReportService.UpdateAsync(report)        │
│   │   ├─ PostDeletionLogService.CreateAsync(log)      │
│   │   ├─ NotificationService.CreateAsync(notify)      │
│   │   └─ PostService.DeleteAsync(post)                │
│   │                                                    │
│   ├─ Infrastructure Layer (Data Access)                │
│   │   ├─ Database: Save deletion log                  │
│   │   ├─ Database: Create notification                │
│   │   ├─ Database: Delete post                        │
│   │   └─ DbContext.SaveChangesAsync()                 │
│   │                                                    │
│   ├─ Return: SuccessResponse                          │
│   └─ Status: 200 OK                                    │
│                                                         │
└─────────────────────┬───────────────────────────────────┘
                      │ HTTP Response + JSON
                      ↓
┌─────────────────────────────────────────────────────────┐
│ FRONTEND (React) - Update                               │
├─────────────────────────────────────────────────────────┤
│                                                         │
│ if (response.success)                                   │
│   ├─ Remove report from list                          │
│   ├─ Close modal                                       │
│   ├─ Show toast "Báo cáo được duyệt"                 │
│   └─ Refresh reports count                            │
│                                                         │
│ else                                                    │
│   ├─ Show error message                               │
│   └─ Keep modal open                                  │
│                                                         │
└─────────────────────────────────────────────────────────┘
```

---

## 📊 Dependency Overview

### Frontend → Backend Dependencies
```
Frontend (React)
    ↓ HTTP Calls
API Layer (Controllers)
    ↓ Calls
Service Layer (Business Logic)
    ↓ Uses
Data Layer (EF Core + Database)
    ↓ Queries
Database (SQL Server)
```

### Dependency Injection (Backend)
```
Program.cs
    ├─ AddDbContext<AppDbContext>
    ├─ AddScoped<IPostService, PostService>
    ├─ AddScoped<IPostReportService, PostReportService>
    ├─ AddScoped<IPostDeletionLogService, PostDeletionLogService>
    ├─ AddScoped<INotificationService, NotificationService>
    └─ (14+ more services)
    
Controllers
    ├─ Inject: IPostService
    ├─ Inject: IPostReportService
    ├─ Inject: INotificationService
    └─ Use them in methods
```

---

## 🚀 File Organization Best Practices

### ✅ DO's
- ✅ Keep related files together in folders
- ✅ Use meaningful file names (UserService, PostController)
- ✅ One main class per file
- ✅ Group similar DTOs in DTOs folder
- ✅ Put utilities in utils folder
- ✅ Use interfaces for abstraction

### ❌ DON'Ts
- ❌ Create files in random locations
- ❌ Mix business logic with API logic
- ❌ Put everything in root
- ❌ Name files vaguely (Helper.cs, Service.cs)
- ❌ Skip folder organization

---

## 📝 Adding New Feature: Step-by-Step

### Example: Create New Feature (e.g., "Post Tags")

**Step 1: Backend**
1. Create Entity: `InteractHub.Application/Entities/Tag.cs`
2. Create DTO: `InteractHub.API/DTOs/TagDto.cs`
3. Create Interface: `InteractHub.Application/Interfaces/ITagService.cs`
4. Create Service: `InteractHub.Infrastructure/Services/TagService.cs`
5. Create Controller: `InteractHub.API/Controllers/TagsController.cs`
6. Register in Program.cs: `builder.Services.AddScoped<ITagService, TagService>();`
7. Create Migration: `dotnet ef migrations add AddTagEntity`

**Step 2: Frontend**
1. Add API function: `src/utils/api.js` - `getTags()`, `createTag()`
2. Create component: `src/components/TagSelector.jsx`
3. Create page: `src/pages/TagsPage.jsx`
4. Add route: `App.jsx`
5. Create styles: `src/styles/TagsPage.css`
6. Add context if needed: `src/contexts/TagsContext.jsx`

---

## 🎯 Summary

| Layer | Files | Purpose |
|-------|-------|---------|
| **Backend API** | Controllers, DTOs, Middleware | HTTP endpoints, validation |
| **Business Logic** | Services, Entities, Interfaces | Business rules, operations |
| **Data Access** | DbContext, Migrations, Repositories | Database queries |
| **Frontend Pages** | .jsx files | User-facing screens |
| **Frontend Components** | Reusable .jsx | UI building blocks |
| **State Management** | Contexts | Global state |
| **API Communication** | utils/api.js | HTTP calls |
| **Real-time** | WebSocket connections | Live updates |
| **Styling** | CSS files | Visual appearance |

---

**Document Status:** ✅ Complete  
**Last Updated:** May 12, 2026  
**Target Audience:** All developers

