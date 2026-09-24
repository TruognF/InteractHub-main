import { useState, useEffect } from 'react';
import { getAllUsersWithRoles, toggleLockUser, deleteUser } from '../api';
import AdminLayout from '../components/AdminLayout';
import UserDetailModal from '../components/UserDetailModal';
import '../styles/AdminUsersPage.css';

const DEFAULT_AVATAR = 'https://ui-avatars.com/api/?background=667eea&color=fff&size=80&name=';

export default function AdminUsersPage() {
  const [users, setUsers] = useState([]);
  const [filteredUsers, setFilteredUsers] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  
  // Filters
  const [searchTerm, setSearchTerm] = useState('');
  const [roleFilter, setRoleFilter] = useState('all');
  const [statusFilter, setStatusFilter] = useState('all');
  
  // Pagination
  const [page, setPage] = useState(1);
  const pageSize = 10;
  
  // Modal
  const [selectedUser, setSelectedUser] = useState(null);

  // Get current admin info from localStorage
  const currentAdmin = (() => {
    try {
      const raw = localStorage.getItem('admin');
      return raw ? JSON.parse(raw) : null;
    } catch {
      return null;
    }
  })();
  const currentAdminId = currentAdmin?.Id || currentAdmin?.id;

  useEffect(() => {
    loadUsers();
  }, []);

  const loadUsers = async () => {
    try {
      setLoading(true);
      setError('');
      const data = await getAllUsersWithRoles();
      setUsers(data);
      applyAllFilters(data, searchTerm, roleFilter, statusFilter);
    } catch (err) {
      setError(err.message || 'Không thể tải danh sách người dùng');
    } finally {
      setLoading(false);
    }
  };

  const applyAllFilters = (userList, search, role, status) => {
    let result = [...userList];

    // Search term
    if (search.trim()) {
      const q = search.toLowerCase().trim();
      result = result.filter(u =>
        (u.UserName && u.UserName.toLowerCase().includes(q)) ||
        (u.FullName && u.FullName.toLowerCase().includes(q)) ||
        (u.Email && u.Email.toLowerCase().includes(q))
      );
    }

    // Role filter
    if (role !== 'all') {
      result = result.filter(u => u.Roles && u.Roles.includes(role));
    }

    // Status filter
    if (status === 'active') {
      result = result.filter(u => !u.IsLocked);
    } else if (status === 'locked') {
      result = result.filter(u => u.IsLocked);
    }

    setFilteredUsers(result);
    setPage(1); // Reset to page 1 on filter
  };

  const handleSearchChange = (e) => {
    const val = e.target.value;
    setSearchTerm(val);
    applyAllFilters(users, val, roleFilter, statusFilter);
  };

  const handleRoleFilterChange = (e) => {
    const val = e.target.value;
    setRoleFilter(val);
    applyAllFilters(users, searchTerm, val, statusFilter);
  };

  const handleStatusFilterChange = (e) => {
    const val = e.target.value;
    setStatusFilter(val);
    applyAllFilters(users, searchTerm, roleFilter, val);
  };

  // Quick toggle lock
  const handleQuickToggleLock = async (user) => {
    if (user.Id === currentAdminId) {
      alert('Không thể khóa tài khoản của chính mình');
      return;
    }

    const actionText = user.IsLocked ? 'mở khóa' : 'khóa';
    if (!window.confirm(`Bạn có chắc muốn ${actionText} tài khoản "${user.UserName}"?`)) return;

    try {
      const res = await toggleLockUser(user.Id);
      const isLocked = res?.Data?.isLocked ?? !user.IsLocked;
      const updatedList = users.map(u => u.Id === user.Id ? { ...u, IsLocked: isLocked } : u);
      setUsers(updatedList);
      applyAllFilters(updatedList, searchTerm, roleFilter, statusFilter);
    } catch (err) {
      alert('Lỗi: ' + (err.message || 'Không thể thay đổi trạng thái'));
    }
  };

  // Quick delete
  const handleQuickDelete = async (user) => {
    if (user.Id === currentAdminId) {
      alert('Không thể xóa tài khoản của chính mình');
      return;
    }

    if (!window.confirm(`⚠️ CẢNH BÁO: Bạn có chắc chắn muốn XÓA tài khoản "${user.UserName}"? Hành động này không thể hoàn tác!`)) return;

    try {
      await deleteUser(user.Id);
      const updatedList = users.filter(u => u.Id !== user.Id);
      setUsers(updatedList);
      applyAllFilters(updatedList, searchTerm, roleFilter, statusFilter);
    } catch (err) {
      alert('Lỗi khi xóa: ' + (err.message || 'Không thể xóa'));
    }
  };

  // Modal callbacks
  const handleUserUpdated = (updatedUser) => {
    const updatedList = users.map(u => u.Id === updatedUser.Id ? updatedUser : u);
    setUsers(updatedList);
    applyAllFilters(updatedList, searchTerm, roleFilter, statusFilter);
    setSelectedUser(updatedUser);
  };

  const handleUserDeleted = (deletedUserId) => {
    const updatedList = users.filter(u => u.Id !== deletedUserId);
    setUsers(updatedList);
    applyAllFilters(updatedList, searchTerm, roleFilter, statusFilter);
    setSelectedUser(null);
  };

  // Pagination slice
  const paginatedUsers = filteredUsers.slice((page - 1) * pageSize, page * pageSize);
  const totalPages = Math.ceil(filteredUsers.length / pageSize) || 1;

  // Stats
  const totalUsersCount = users.length;
  const adminCount = users.filter(u => u.Roles?.includes('Admin')).length;
  const lockedCount = users.filter(u => u.IsLocked).length;

  return (
    <AdminLayout>
      <div className="admin-users-page">
        {/* Header & Stats */}
        <div className="users-page-header">
          <div>
            <h2>Quản Lý Người Dùng</h2>
            <p className="page-subtitle">Xem, phân quyền và kiểm soát tài khoản trong hệ thống</p>
          </div>

          <div className="users-stats-strip">
            <div className="stat-pill">
              <span className="stat-num">{totalUsersCount}</span>
              <span className="stat-label">Tổng Users</span>
            </div>
            <div className="stat-pill admin">
              <span className="stat-num">{adminCount}</span>
              <span className="stat-label">Admins</span>
            </div>
            <div className="stat-pill locked">
              <span className="stat-num">{lockedCount}</span>
              <span className="stat-label">Bị Khóa</span>
            </div>
          </div>
        </div>

        {error && <div className="users-error-banner">❌ {error}</div>}

        {/* Filter Controls */}
        <div className="users-filter-bar">
          <div className="search-box">
            <span className="search-icon">🔍</span>
            <input
              type="text"
              placeholder="Tìm theo username, họ tên, email..."
              value={searchTerm}
              onChange={handleSearchChange}
              className="search-input"
            />
            {searchTerm && (
              <button
                className="clear-search-btn"
                onClick={() => {
                  setSearchTerm('');
                  applyAllFilters(users, '', roleFilter, statusFilter);
                }}
              >
                ✕
              </button>
            )}
          </div>

          <div className="filters-right">
            <select
              value={roleFilter}
              onChange={handleRoleFilterChange}
              className="filter-select-users"
            >
              <option value="all">🛡️ Tất cả vai trò</option>
              <option value="Admin">Admin</option>
              <option value="Moderator">Moderator</option>
              <option value="User">User</option>
            </select>

            <select
              value={statusFilter}
              onChange={handleStatusFilterChange}
              className="filter-select-users"
            >
              <option value="all">⚡ Tất cả trạng thái</option>
              <option value="active">🟢 Đang hoạt động</option>
              <option value="locked">🔒 Đang bị khóa</option>
            </select>
          </div>
        </div>

        {/* User Table */}
        {loading ? (
          <div className="users-table-skeleton">
            {[1, 2, 3, 4, 5].map(i => (
              <div key={i} className="skeleton-row-user" />
            ))}
          </div>
        ) : filteredUsers.length === 0 ? (
          <div className="users-empty-state">
            <div className="empty-icon">👥</div>
            <p>Không tìm thấy người dùng nào phù hợp</p>
          </div>
        ) : (
          <div className="users-table-container">
            <table className="users-table">
              <thead>
                <tr>
                  <th>Người dùng</th>
                  <th>Email</th>
                  <th>Vai trò</th>
                  <th>Trạng thái</th>
                  <th>Hành động</th>
                </tr>
              </thead>
              <tbody>
                {paginatedUsers.map(user => {
                  const isSelf = user.Id === currentAdminId;
                  return (
                    <tr key={user.Id} className={user.IsLocked ? 'row-locked' : ''}>
                      <td className="user-cell">
                        <img
                          src={user.ProfilePictureUrl || `${DEFAULT_AVATAR}${encodeURIComponent(user.FullName || user.UserName || 'U')}`}
                          alt="Avatar"
                          className="user-table-avatar"
                          onError={(e) => { e.target.src = `${DEFAULT_AVATAR}U`; }}
                        />
                        <div className="user-name-group">
                          <strong className="user-table-fullname">{user.FullName || 'Chưa đặt tên'}</strong>
                          <span className="user-table-username">
                            @{user.UserName}
                            {isSelf && <span className="self-tag-small">Bạn</span>}
                          </span>
                        </div>
                      </td>

                      <td className="user-email-cell">
                        {user.Email || '—'}
                      </td>

                      <td>
                        <div className="roles-badge-container">
                          {user.Roles && user.Roles.length > 0 ? (
                            user.Roles.map(role => (
                              <span key={role} className={`role-badge ${role.toLowerCase()}`}>
                                {role}
                              </span>
                            ))
                          ) : (
                            <span className="role-badge none">Không có</span>
                          )}
                        </div>
                      </td>

                      <td>
                        <span className={`status-badge-user ${user.IsLocked ? 'locked' : 'active'}`}>
                          {user.IsLocked ? '🔒 Bị khóa' : '🟢 Hoạt động'}
                        </span>
                      </td>

                      <td className="user-actions-cell">
                        <button
                          className="btn-user-action preview"
                          onClick={() => setSelectedUser(user)}
                          title="Xem chi tiết & quản lý tài khoản"
                        >
                          👁️ Chi tiết
                        </button>

                        <button
                          className={`btn-user-action lock ${user.IsLocked ? 'unlock' : ''}`}
                          onClick={() => handleQuickToggleLock(user)}
                          disabled={isSelf}
                          title={isSelf ? 'Không thể khóa tài khoản của chính mình' : user.IsLocked ? 'Mở khóa tài khoản' : 'Khóa tài khoản'}
                        >
                          {user.IsLocked ? '🔓 Mở' : '🔒 Khóa'}
                        </button>

                        {/* <button
                          // className="btn-user-action delete"
                          onClick={() => handleQuickDelete(user)}
                          disabled={isSelf}
                          title={isSelf ? 'Không thể xóa chính mình' : 'Xóa tài khoản'}
                        >
                          🗑️
                        </button> */}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}

        {/* Pagination */}
        {!loading && filteredUsers.length > pageSize && (
          <div className="users-pagination">
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
              className="btn-paging"
            >
              ← Trước
            </button>
            <span className="page-info">Trang {page} / {totalPages}</span>
            <button
              onClick={() => setPage(p => Math.min(totalPages, p + 1))}
              disabled={page === totalPages}
              className="btn-paging"
            >
              Tiếp →
            </button>
          </div>
        )}

        {/* User Detail Modal */}
        {selectedUser && (
          <UserDetailModal
            user={selectedUser}
            currentAdminId={currentAdminId}
            onClose={() => setSelectedUser(null)}
            onUserUpdated={handleUserUpdated}
            onUserDeleted={handleUserDeleted}
          />
        )}
      </div>
    </AdminLayout>
  );
}
