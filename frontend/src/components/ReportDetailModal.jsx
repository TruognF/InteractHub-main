import { useState, useEffect } from 'react';
import { getPostByIdAsAdmin, approveReport, rejectReport } from '../api';
import '../styles/ReportDetailModal.css';

export default function ReportDetailModal({ report, onClose, onApprove, onReject }) {
  const [post, setPost] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [actioning, setActioning] = useState(false);

  useEffect(() => {
    loadPostDetails();
  }, [report.PostId]);

  const loadPostDetails = async () => {
    try {
      setLoading(true);
      const postData = await getPostByIdAsAdmin(report.PostId);
      setPost(postData);
    } catch (err) {
      setError('Không thể tải nội dung bài viết: ' + err.message);
    } finally {
      setLoading(false);
    }
  };

  const handleApprove = async () => {
    try {
      setActioning(true);
      await approveReport(report.Id);
      onApprove(report.Id);
      onClose();
    } catch (err) {
      setError('Lỗi khi duyệt báo cáo: ' + err.message);
    } finally {
      setActioning(false);
    }
  };

  const handleReject = async () => {
    try {
      setActioning(true);
      await rejectReport(report.Id);
      onReject(report.Id);
      onClose();
    } catch (err) {
      setError('Lỗi khi từ chối báo cáo: ' + err.message);
    } finally {
      setActioning(false);
    }
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
    <div className="report-modal-overlay" onClick={onClose}>
      <div className="report-modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="report-modal-header">
          <h2>Chi tiết báo cáo #{report.Id}</h2>
          <button className="btn-close" onClick={onClose}>✕</button>
        </div>

        {error && <div className="report-modal-error">{error}</div>}

        <div className="report-modal-body">
          {/* Post Content Section */}
          <div className="report-section">
            <h3 className="section-title">📝 Nội dung bài viết</h3>
            {loading ? (
              <div className="loading-skeleton">
                <div className="skeleton-line" style={{ width: '80%' }} />
                <div className="skeleton-line" style={{ width: '100%' }} />
                <div className="skeleton-line" style={{ width: '90%' }} />
              </div>
            ) : post ? (
              <div className="post-preview">
                <div className="post-header">
                  <div className="post-author">
                    <strong>{post.UserName || 'Người dùng'}</strong>
                  </div>
                  <div className="post-date">
                    {new Date(post.CreatedAt).toLocaleDateString('vi-VN', {
                      day: '2-digit',
                      month: '2-digit',
                      year: 'numeric',
                      hour: '2-digit',
                      minute: '2-digit'
                    })}
                  </div>
                </div>
                <div className="post-content">
                  {post.Content}
                </div>
                {post.ImageUrl && (
                  <div className="post-image">
                    <img src={post.ImageUrl} alt="Post content" />
                  </div>
                )}
              </div>
            ) : (
              <div className="post-not-found">
                ⚠️ Không thể tải bài viết (có thể đã bị xóa)
              </div>
            )}
          </div>

          {/* Report Details Section */}
          <div className="report-section">
            <h3 className="section-title">🚨 Chi tiết báo cáo</h3>
            <div className="report-details">
              <div className="detail-row">
                <span className="detail-label">Lý do:</span>
                <span className="detail-value">{ReasonMap[report.Reason] || 'Unknown'}</span>
              </div>
              <div className="detail-row">
                <span className="detail-label">Chi tiết:</span>
                <span className="detail-value report-detail-text">{report.Detail || 'Không có chi tiết'}</span>
              </div>
              <div className="detail-row">
                <span className="detail-label">Ngày báo cáo:</span>
                <span className="detail-value">
                  {new Date(report.CreatedAt).toLocaleDateString('vi-VN')}
                </span>
              </div>
            </div>
          </div>

          {/* Action Buttons Section */}
          <div className="report-section">
            <h3 className="section-title">⚙️ Hành động</h3>
            <div className="report-actions-modal">
              <button
                className="btn-approve-modal"
                onClick={handleApprove}
                disabled={actioning}
              >
                {actioning ? '⏳ Đang xử lý...' : '✓ Duyệt báo cáo (Xóa bài viết)'}
              </button>
              <button
                className="btn-reject-modal"
                onClick={handleReject}
                disabled={actioning}
              >
                {actioning ? '⏳ Đang xử lý...' : '✕ Từ chối báo cáo'}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
