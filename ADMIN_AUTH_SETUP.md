# Admin Authentication System - Setup Guide

## Overview
Admin users have a **completely separate login system** from regular users:
- Regular users login at `POST /api/auth/login` 
- Admin users login at `POST /api/admin/login`
- Admin users **cannot** use regular login endpoint

---

## How It Works

### 1. Admin User Creation (Automatic on Database Init)
When the database is first created, a default admin user is automatically seeded:
- **Username**: `admin`
- **Email**: `admin@interacthub.com`
- **Password**: `Admin@123456`
- **Role**: `Admin` (only)

Location: `InteractHub.Infrastructure/Data/DbInitializer.cs` → `SeedRolesAndAdmin()`

### 2. Regular User Login (User endpoint)
```
POST /api/auth/login
Body: { userName: "john", password: "password123" }

Response:
{
  "success": true,
  "message": "Login successful",
  "data": {
    "token": "eyJhbGc...",
    "user": {
      "id": "user-123",
      "userName": "john",
      "email": "john@example.com",
      "fullName": "John Doe",
      "profilePictureUrl": null
    }
  }
}
```

**Security**: If user has Admin role, login is **REJECTED** with error message: "Admin users must use admin login endpoint"

### 3. Admin User Login (Admin endpoint)
```
POST /api/admin/login
Body: { userName: "admin", password: "Admin@123456" }

Response:
{
  "success": true,
  "message": "Admin login successful",
  "data": {
    "token": "eyJhbGc...",
    "admin": {
      "id": "admin-123",
      "userName": "admin",
      "email": "admin@interacthub.com",
      "fullName": "System Administrator",
      "profilePictureUrl": null,
      "isAdmin": true
    }
  }
}
```

**Security**: If user does **NOT** have Admin role, login is **REJECTED** with error: "Only admin users can access this endpoint" (403 Forbidden)

### 4. Admin Verification Endpoint
```
GET /api/admin/me
Headers: Authorization: Bearer <admin-token>

Response:
{
  "success": true,
  "data": {
    "id": "admin-123",
    "userName": "admin",
    "email": "admin@interacthub.com",
    "fullName": "System Administrator",
    "isAdmin": true
  }
}
```

### 5. Admin Token Refresh
```
POST /api/admin/refresh-token
Headers: Authorization: Bearer <admin-token>

Response:
{
  "success": true,
  "message": "Token refreshed successfully",
  "data": {
    "token": "eyJhbGc..."
  }
}
```

---

## Security Features

✅ **Role-Based Separation**
- Regular users have role: `User`
- Admin users have role: `Admin`
- No user can have both roles simultaneously

✅ **Endpoint Protection**
- Regular login rejects admin users
- Admin login rejects non-admin users
- All admin endpoints require `[Authorize(Roles = "Admin")]`

✅ **Audit Logging**
- Admin login success/failure logged to console
- Non-admin attempts to access admin login logged as warning

✅ **Token Claims**
- Admin tokens include claim: `IsAdmin = true`
- Regular tokens do NOT have this claim
- Frontend can check this to determine which interface to show

---

## Backend Implementation Details

### AdminAuthController.cs
Location: `InteractHub.API/Controllers/AdminAuthController.cs`

**Endpoints**:
1. `POST /api/admin/login` - Admin login only
2. `GET /api/admin/me` - Current admin info (requires Auth + Admin role)
3. `POST /api/admin/refresh-token` - Refresh admin token (requires Auth + Admin role)

**Key Logic**:
```csharp
// Only allow Admin role users
var roles = await _userManager.GetRolesAsync(user);
if (!roles.Contains(RoleConstants.Admin))
{
    return this.ForbiddenResponse("Only admin users can access this endpoint");
}
```

### AuthController.cs (Modified)
**Login endpoint now checks**:
```csharp
// Prevent admin users from using regular login
var roles = await _userManager.GetRolesAsync(user);
if (roles.Contains(RoleConstants.Admin))
    return this.UnauthorizedResponse("Admin users must use admin login endpoint");
```

---

## Frontend Integration

### User Login (Regular)
```javascript
// User login - use regular endpoint
const response = await fetch('http://localhost:5000/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ 
    userName: 'john', 
    password: 'password123' 
  })
});

const { data } = await response.json();
localStorage.setItem('userToken', data.token);
// Navigate to /home
```

### Admin Login
```javascript
// Admin login - use separate endpoint
const response = await fetch('http://localhost:5000/api/admin/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ 
    userName: 'admin', 
    password: 'Admin@123456' 
  })
});

const { data } = await response.json();
localStorage.setItem('adminToken', data.token);
// Navigate to /admin/dashboard
```

### Token Validation
```javascript
// Check if token belongs to admin
const token = localStorage.getItem('adminToken');
const response = await fetch('http://localhost:5000/api/admin/me', {
  headers: { 'Authorization': `Bearer ${token}` }
});

if (response.status === 403) {
  // Token invalid or user not admin
  // Redirect to admin login
}
```

---

## Routes Structure

### Public Routes
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration
- `POST /api/admin/login` - **Admin login** (new)

### Protected Routes (User)
- `GET /api/users/me` - Current user info
- `GET /api/posts` - Get posts
- etc.

### Protected Routes (Admin Only)
- `GET /api/admin/me` - Admin info
- `GET /api/postreports/pending` - View pending reports
- `GET /api/postreports` - View all reports
- `POST /api/postreports/{id}/approve` - Approve report
- `POST /api/postreports/{id}/reject` - Reject report
- `GET /api/users` - View all users (future)
- etc.

---

## Database Structure

### AspNetRoles (Identity)
```
Id: "admin-role"
Name: "Admin"
NormalizedName: "ADMIN"

Id: "user-role"
Name: "User"
NormalizedName: "USER"

Id: "moderator-role"
Name: "Moderator"
NormalizedName: "MODERATOR"
```

### AspNetUserRoles (Identity)
```
UserId: "admin-123"
RoleId: "admin-role"

UserId: "user-456"
RoleId: "user-role"
```

### AspNetUsers (Identity)
```
Id: "admin-123"
UserName: "admin"
Email: "admin@interacthub.com"
FullName: "System Administrator"
SecurityStamp: [hash]
PasswordHash: [hashed Admin@123456]
```

---

## Testing

### Test Admin Login
```bash
# 1. Start the server
dotnet run

# 2. Login with admin credentials
curl -X POST http://localhost:5000/api/admin/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@123456"}'

# Expected response (200 OK):
# {
#   "success": true,
#   "message": "Admin login successful",
#   "data": { "token": "...", "admin": {...} }
# }
```

### Test Admin Can't Use Regular Login
```bash
# Try to login as admin on regular endpoint
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"admin","password":"Admin@123456"}'

# Expected response (401 Unauthorized):
# {
#   "success": false,
#   "message": "Admin users must use admin login endpoint"
# }
```

### Test Non-Admin Can't Use Admin Login
```bash
# Try to login as regular user on admin endpoint
curl -X POST http://localhost:5000/api/admin/login \
  -H "Content-Type: application/json" \
  -d '{"userName":"john","password":"password123"}'

# Expected response (403 Forbidden):
# {
#   "success": false,
#   "message": "Only admin users can access this endpoint"
# }
```

---

## Changing Admin Password

Update in `DbInitializer.cs` → `SeedRolesAndAdmin()`:
```csharp
var createResult = await userManager.CreateAsync(adminUser, "NEW_PASSWORD_HERE");
```

Or programmatically:
```csharp
var admin = await userManager.FindByNameAsync("admin");
await userManager.ChangePasswordAsync(admin, "Admin@123456", "NewPassword@123456");
```

---

## Creating Additional Admins

```csharp
var newAdmin = new User
{
    UserName = "admin2",
    Email = "admin2@interacthub.com",
    FullName = "Second Administrator"
};

var result = await userManager.CreateAsync(newAdmin, "NewAdmin@123456");
if (result.Succeeded)
{
    await userManager.AddToRoleAsync(newAdmin, RoleConstants.Admin);
}
```

---

## Important Files

1. **AdminAuthController.cs** - Admin login endpoints
2. **AuthController.cs** - Regular login (modified to reject admins)
3. **DbInitializer.cs** - Seeds admin user on database creation
4. **RoleConstants.cs** - Defines role names
5. **Program.cs** - Registers services and calls seeding

---

## Key Points ✅

- ✅ Admin users created via `DbInitializer.SeedRolesAndAdmin()`
- ✅ Admin login at `POST /api/admin/login` (separate endpoint)
- ✅ Regular login rejects admin users
- ✅ Admin endpoints require `[Authorize(Roles = "Admin")]`
- ✅ All requests are logged for audit trail
- ✅ Tokens include role claims for authorization checks

**Status: Production Ready** 🚀
