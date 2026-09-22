import { useState, useEffect } from 'react';
import { getAllReports } from '../api';
import AdminLayout from '../components/AdminLayout';
import '../styles/AdminDashboard.css';

export default function AdminDashboard() {
  const [stats, setStats] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    loadStats();
    // Set up interval to refresh stats every 30 seconds
    const interval = setInterval(loadStats, 30000);

    return () => clearInterval(interval);
  }, []);

  const loadStats = async () => {
    try {
      setLoading(true);
      // Fetch all reports to calculate statistics
      const allReports = await getAllReports(1, 1000); // Get up to 1000 reports
      
      const totalPending = allReports.filter(r => r.Status === 0).length;
      const totalApproved = allReports.filter(r => r.Status === 1).length;
      const totalRejected = allReports.filter(r => r.Status === 2).length;

      setStats({
        totalPending,
        totalApproved,
        totalRejected,
        lastUpdate: new Date().toLocaleTimeString('vi-VN')
      });
    } catch (err) {
      setError(err.message || 'Không thể tải thống kê');
    } finally {
      setLoading(false);
    }
  };

  return (
    <AdminLayout>
      <div className="admin-dashboard">
        <h2>Dashboard</h2>

        {error && <div className="dashboard-error">{error}</div>}

        {loading ? (
          <div className="dashboard-grid">
            {[1, 2, 3].map(i => (
              <div key={i} className="stat-card skeleton">
                <div className="skeleton-content" />
              </div>
            ))}
          </div>
        ) : (
          <div className="dashboard-grid">
            <div className="stat-card pending">
              <div className="stat-icon">⏳</div>
              <div className="stat-content">
                <h3>Chờ Xử Lý</h3>
                <p className="stat-value">{stats?.totalPending || 0}</p>
                <p className="stat-subtext">báo cáo chưa xử lý</p>
              </div>
            </div>

            <div className="stat-card approved">
              <div className="stat-icon">✅</div>
              <div className="stat-content">
                <h3>Đã Duyệt</h3>
                <p className="stat-value">{stats?.totalApproved || 0}</p>
                <p className="stat-subtext">báo cáo được duyệt</p>
              </div>
            </div>

            <div className="stat-card rejected">
              <div className="stat-icon">❌</div>
              <div className="stat-content">
                <h3>Từ Chối</h3>
                <p className="stat-value">{stats?.totalRejected || 0}</p>
                <p className="stat-subtext">báo cáo bị từ chối</p>
              </div>
            </div>
          </div>
        )}

        <div className="dashboard-info">
          <div className="info-card">
            <h3>ℹ️ Hệ thống báo cáo nội dung</h3>
            <ul>
              <li>Xem tất cả báo cáo từ người dùng</li>
              <li>Duyệt hoặc từ chối báo cáo</li>
              <li>Gỡ xuống nội dung vi phạm tự động</li>
              <li>Gửi thông báo cho chủ bài viết</li>
            </ul>
          </div>

          <div className="info-card">
            <h3>🔍 Lý do báo cáo phổ biến</h3>
            <ul>
              <li>Nội dung gây hại</li>
              <li>Thư rác</li>
              <li>Qu騷rối/Tấn công cá nhân</li>
              <li>Nội dung bạo lực</li>
              <li>Nội dung người lớn</li>
              <li>Tin giả/Sai lệch thông tin</li>
            </ul>
          </div>
        </div>

        {stats && (
          <p className="dashboard-timestamp">Cập nhật lần cuối: {stats.lastUpdate}</p>
        )}
      </div>
    </AdminLayout>
  );
}
