import { Link, useLocation } from 'react-router-dom';
import '../styles/AdminSidebar.css';

const ADMIN_ROUTES = [
  { path: '/admin/reports', label: 'Báo cáo', icon: '📋' }
];

export default function AdminSidebar({ open, onClose }) {
  const location = useLocation();

  return (
    <>
      {/* Overlay for mobile */}
      {open && <div className="sidebar-overlay" onClick={onClose} />}

      <aside className={`admin-sidebar ${open ? 'open' : ''}`}>
        <div className="sidebar-header">
          <h2>Menu</h2>
          <button className="sidebar-close" onClick={onClose} aria-label="Close sidebar">
            ✕
          </button>
        </div>

        <nav className="sidebar-nav">
          {ADMIN_ROUTES.map(route => (
            <Link
              key={route.path}
              to={route.path}
              className={`sidebar-link ${location.pathname === route.path ? 'active' : ''}`}
              onClick={onClose}
            >
              <span className="sidebar-icon">{route.icon}</span>
              <span className="sidebar-label">{route.label}</span>
            </Link>
          ))}
        </nav>

        <div className="sidebar-footer">
          <p className="sidebar-version">v1.0 Admin</p>
        </div>
      </aside>
    </>
  );
}
