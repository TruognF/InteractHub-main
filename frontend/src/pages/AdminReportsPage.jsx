import { useState, useEffect } from 'react';
import { getAllReports } from '../api';
import AdminLayout from '../components/AdminLayout';
import ReportDetailModal from '../components/ReportDetailModal';
import '../styles/AdminReportsPage.css';

const ReportStatusBadge = ({ status }) => {
  const statusMap = {
    0: { label: 'Chờ xử lý', class: 'pending' },
    1: { label: 'Đã duyệt', class: 'approved' },
    2: { label: 'Từ chối', class: 'rejected' }
  };
  const s = statusMap[status] || { label: 'Unknown', class: 'unknown' };
  return <span className={`status-badge ${s.class}`}>{s.label}</span>;
};

export default function AdminReportsPage() {
  const [reports, setReports] = useState([]);
  const [filteredReports, setFilteredReports] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [statusFilter, setStatusFilter] = useState('all');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(10);
  const [selectedReport, setSelectedReport] = useState(null);

  useEffect(() => {
    loadReports();
  }, [page]);

  const loadReports = async () => {
    try {
      setLoading(true);
      const data = await getAllReports(page, pageSize);
      setReports(data);
      applyFilter(data, statusFilter);
    } catch (err) {
      setError(err.message || 'Không thể tải báo cáo');
    } finally {
      setLoading(false);
    }
  };

  const applyFilter = (reportsList, filterValue) => {
    if (filterValue === 'all') {
      setFilteredReports(reportsList);
    } else {
      const statusMap = { pending: 0, approved: 1, rejected: 2 };
      const filtered = reportsList.filter(r => r.Status === statusMap[filterValue]);
      setFilteredReports(filtered);
    }
  };

  const handleFilterChange = (value) => {
    setStatusFilter(value);
    applyFilter(reports, value);
  };

  const handleApprove = (reportId) => {
    // Remove the report from the list when approved
    setReports(reports.filter(r => r.Id !== reportId));
    applyFilter(reports.filter(r => r.Id !== reportId), statusFilter);
  };

  const handleReject = (reportId) => {
    // Update the report status to rejected (2)
    setReports(reports.map(r => r.Id === reportId ? { ...r, Status: 2 } : r));
    applyFilter(reports.map(r => r.Id === reportId ? { ...r, Status: 2 } : r), statusFilter);
  };

  const ReasonMap = {
    0: 'Nội dung gây hại',
    1: 'Thư rác',
    2: 'Quấy rối/Tấn công',
    3: 'Nội dung bạo lực',
    4: 'Nội dung người lớn',
    5: 'Tin giả',
    6: 'Khác'
  };

  return (
    <AdminLayout>
      <div className="admin-reports">
        <div className="reports-header">
          <h2>Quản Lý Báo Cáo</h2>
          <div className="filter-group">
            <select
              value={statusFilter}
              onChange={(e) => handleFilterChange(e.target.value)}
              className="filter-select"
            >
              <option value="all">Tất cả</option>
              <option value="pending">Chờ xử lý</option>
              <option value="approved">Đã duyệt</option>
              <option value="rejected">Từ chối</option>
            </select>
          </div>
        </div>

        {error && <div className="reports-error">{error}</div>}

        {loading ? (
          <div className="reports-skeleton">
            {[1, 2, 3, 4, 5].map(i => (
              <div key={i} className="skeleton-row" />
            ))}
          </div>
        ) : filteredReports.length === 0 ? (
          <div className="reports-empty">
            <div className="empty-icon">📋</div>
            <p>Không có báo cáo nào</p>
          </div>
        ) : (
          <div className="reports-table-wrapper">
            <table className="reports-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Lý do</th>
                  <th>Chi tiết</th>
                  <th>Trạng thái</th>
                  <th>Ngày tạo</th>
                  <th>Hành động</th>
                </tr>
              </thead>
              <tbody>
                {filteredReports.map(report => (
                  <tr key={report.Id}>
                    <td className="report-id">#{report.Id}</td>
                    <td>{ReasonMap[report.Reason] || 'Unknown'}</td>
                    <td className="report-detail" title={report.Detail}>
                      {report.Detail?.substring(0, 50) || 'N/A'}
                      {report.Detail?.length > 50 ? '...' : ''}
                    </td>
                    <td>
                      <ReportStatusBadge status={report.Status} />
                    </td>
                    <td className="report-date">
                      {new Date(report.CreatedAt).toLocaleDateString('vi-VN')}
                    </td>
                    <td className="report-actions">
                      {report.Status === 0 && (
                        <button
                          className="btn-preview"
                          onClick={() => setSelectedReport(report)}
                          title="Xem chi tiết báo cáo"
                        >
                          👁️ Xem trước
                        </button>
                      )}
                      {report.Status !== 0 && (
                        <span className="action-processed">Đã xử lý</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {!loading && filteredReports.length > 0 && (
          <div className="reports-pagination">
            <button
              onClick={() => setPage(p => Math.max(1, p - 1))}
              disabled={page === 1}
              className="btn-paging"
            >
              ← Trước
            </button>
            <span className="page-info">Trang {page}</span>
            <button
              onClick={() => setPage(p => p + 1)}
              disabled={filteredReports.length < pageSize}
              className="btn-paging"
            >
              Tiếp →
            </button>
          </div>
        )}

        {selectedReport && (
          <ReportDetailModal
            report={selectedReport}
            onClose={() => setSelectedReport(null)}
            onApprove={handleApprove}
            onReject={handleReject}
          />
        )}
      </div>
    </AdminLayout>
  );
}
