import { useAdminAuth } from '../contexts/AdminAuthContext';
import '../styles/AdminNavbar.css';

export default function AdminNavbar({ onMenuClick, onLogout }) {
  const { admin } = useAdminAuth();

  return (
    <nav className="admin-navbar">
      <div className="admin-navbar-content">
        <div className="admin-navbar-left">
          <button className="navbar-menu-btn" onClick={onMenuClick} aria-label="Toggle sidebar">
            ☰
          </button>
          <div className="navbar-brand">
            <span className="navbar-icon">⚙️</span>
            <h1>Admin Dashboard</h1>
          </div>
        </div>

        <div className="admin-navbar-right">
          <div className="admin-info">
            <span className="admin-name">{admin?.FullName || 'Admin'}</span>
            <span className="admin-badge">Admin</span>
          </div>
          <button className="navbar-logout-btn" onClick={onLogout} title="Logout">
            🚪
          </button>
        </div>
      </div>
    </nav>
  );
}
