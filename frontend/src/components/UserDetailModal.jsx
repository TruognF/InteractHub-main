import { useState } from 'react';
import { assignRoleToUser, removeRoleFromUser, toggleLockUser, deleteUser } from '../api';
import '../styles/UserDetailModal.css';

const DEFAULT_AVATAR = 'https://ui-avatars.com/api/?background=667eea&color=fff&size=80&name=';
const AVAILABLE_ROLES = ['Admin', 'Moderator', 'User'];

export default function UserDetailModal({ user, currentAdminId, onClose, onUserUpdated, onUserDeleted }) {
  const [currentUser, setCurrentUser] = useState(user);
  const [loadingAction, setLoadingAction] = useState('');
  const [message, setMessage] = useState({ text: '', type: '' });

  const isSelf = currentUser.Id === currentAdminId;

  const showMsg = (text, type = 'success') => {
    setMessage({ text, type });
    setTimeout(() => setMessage({ text: '', type: '' }), 4000);
  };

  // Toggle lock / unlock
  const handleToggleLock = async () => {
    if (isSelf) {
      showMsg('Không thể khóa tài khoản của chính mình', 'error');
      return;
    }

    const actionText = currentUser.IsLocked ? 'mở khóa' : 'khóa';
    if (!window.confirm(`Bạn có chắc muốn ${actionText} tài khoản "${currentUser.UserName}"?`)) return;

    try {
      setLoadingAction('lock');
      const res = await toggleLockUser(currentUser.Id);
      const isLocked = res?.Data?.isLocked ?? !currentUser.IsLocked;
      const updated = { ...currentUser, IsLocked: isLocked };
      setCurrentUser(updated);
      onUserUpdated(updated);
      showMsg(res?.Message || `Đã ${actionText} tài khoản thành công!`, 'success');
    } catch (err) {
      showMsg(err.message || `Lỗi khi ${actionText} tài khoản`, 'error');
    } finally {
      setLoadingAction('');
    }
  };

  // Role toggle
  const handleRoleToggle = async (role) => {
    const hasRole = currentUser.Roles?.includes(role);

    if (hasRole && role === 'Admin' && isSelf) {
      showMsg('Không thể tự gỡ quyền Admin của chính mình', 'error');
      return;
    }

    try {
      setLoadingAction(`role-${role}`);
      if (hasRole) {
        await removeRoleFromUser(currentUser.Id, role);
        const updatedRoles = currentUser.Roles.filter(r => r !== role);
        const updated = { ...currentUser, Roles: updatedRoles };
        setCurrentUser(updated);
        onUserUpdated(updated);
        showMsg(`Đã gỡ vai trò ${role}`, 'success');
      } else {
        await assignRoleToUser(currentUser.Id, role);
        const updatedRoles = [...(currentUser.Roles || []), role];
        const updated = { ...currentUser, Roles: updatedRoles };
        setCurrentUser(updated);
        onUserUpdated(updated);
        showMsg(`Đã gán vai trò ${role}`, 'success');
      }
    } catch (err) {
      showMsg(err.message || 'Lỗi khi cập nhật vai trò', 'error');
    } finally {
      setLoadingAction('');
    }
  };

  // Delete user
  const handleDeleteUser = async () => {
    if (isSelf) {
      showMsg('Không thể xóa tài khoản của chính mình', 'error');
      return;
    }

    if (!window.confirm(`⚠️ CẢNH BÁO: Bạn có chắc chắn muốn XÓA VĨNH VIỄN tài khoản "${currentUser.UserName}"? Hành động này không thể hoàn tác!`)) return;

    try {
      setLoadingAction('delete');
      await deleteUser(currentUser.Id);
      onUserDeleted(currentUser.Id);
      onClose();
    } catch (err) {
      showMsg(err.message || 'Lỗi khi xóa tài khoản', 'error');
      setLoadingAction('');
    }
  };

  return (
    <div className="user-modal-overlay" onClick={onClose}>
      <div className="user-modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="user-modal-header">
          <h2>Chi tiết tài khoản @{currentUser.UserName}</h2>
          <button className="btn-close" onClick={onClose}>✕</button>
        </div>

        {message.text && (
          <div className={`user-modal-alert ${message.type}`}>
            {message.type === 'success' ? '✅ ' : '❌ '}
            {message.text}
          </div>
        )}

        <div className="user-modal-body">
          {/* User Info Card */}
          <div className="user-profile-header">
            <img
              className="user-profile-avatar"
              src={currentUser.ProfilePictureUrl || `${DEFAULT_AVATAR}${encodeURIComponent(currentUser.FullName || currentUser.UserName || 'U')}`}
              alt="Avatar"
              onError={(e) => { e.target.src = `${DEFAULT_AVATAR}U`; }}
            />
            <div className="user-profile-meta">
              <h3 className="user-profile-name">{currentUser.FullName || 'Chưa cập nhật tên'}</h3>
              <p className="user-profile-handle">@{currentUser.UserName}</p>
              <p className="user-profile-email">✉️ {currentUser.Email || 'Không có email'}</p>
              {currentUser.Bio && <p className="user-profile-bio">💬 "{currentUser.Bio}"</p>}
              
              <div className="user-badges-row">
                <span className={`status-pill ${currentUser.IsLocked ? 'locked' : 'active'}`}>
                  {currentUser.IsLocked ? '🔒 Đang bị khóa' : '🟢 Đang hoạt động'}
                </span>
                {isSelf && <span className="status-pill self-tag">👤 Bạn</span>}
              </div>
            </div>
          </div>

          {/* Role Management */}
          <div className="user-modal-section">
            <h4 className="section-heading">🛡️ Phân quyền tài khoản</h4>
            <div className="roles-toggle-group">
              {AVAILABLE_ROLES.map(role => {
                const hasRole = currentUser.Roles?.includes(role);
                const isLoading = loadingAction === `role-${role}`;
                return (
                  <button
                    key={role}
                    type="button"
                    className={`role-toggle-btn ${role.toLowerCase()} ${hasRole ? 'active' : ''}`}
                    onClick={() => handleRoleToggle(role)}
                    disabled={isLoading || (hasRole && role === 'Admin' && isSelf)}
                    title={hasRole ? `Nhấp để gỡ quyền ${role}` : `Nhấp để gán quyền ${role}`}
                  >
                    {isLoading ? '⏳...' : hasRole ? `✓ ${role}` : `+ ${role}`}
                  </button>
                );
              })}
            </div>
            {isSelf && <p className="note-text">Lưu ý: Bạn không thể tự gỡ quyền Admin của chính mình.</p>}
          </div>

          {/* Danger Zone */}
          <div className="user-modal-section danger-section">
            <h4 className="section-heading">⚠️ Tác vụ quản trị</h4>
            <div className="danger-actions-row">
              <button
                type="button"
                className={`btn-lock-toggle ${currentUser.IsLocked ? 'btn-unlock' : 'btn-lock'}`}
                onClick={handleToggleLock}
                disabled={loadingAction === 'lock' || isSelf}
              >
                {loadingAction === 'lock'
                  ? '⏳ Đang xử lý...'
                  : currentUser.IsLocked
                    ? '🔓 Mở khóa tài khoản'
                    : '🔒 Khóa tài khoản'}
              </button>

              {/* Tạm thời ẩn chức năng xóa tài khoản theo yêu cầu
              <button
                type="button"
                className="btn-delete-account"
                onClick={handleDeleteUser}
                disabled={loadingAction === 'delete' || isSelf}
              >
                {loadingAction === 'delete' ? '⏳ Đang xóa...' : '🗑️ Xóa vĩnh viễn tài khoản'}
              </button>
              */}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
