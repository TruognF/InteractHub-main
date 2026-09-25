import { useState, useEffect } from 'react';
import { getPostByIdAsAdmin, approveReport, rejectReport, acceptReportAppeal, rejectReportAppeal } from '../api';
import '../styles/ReportDetailModal.css';

const DEFAULT_AVATAR = 'https://ui-avatars.com/api/?background=667eea&color=fff&size=80&name=';

export default function ReportDetailModal({ report, onClose, onApprove, onReject, onAcceptAppeal, onRejectAppeal, onDelete }) {
  const [post, setPost] = useState(report.Post || null);
  const [loading, setLoading] = useState(!report.Post);
  const [error, setError] = useState('');
  const [actioning, setActioning] = useState(false);

  // Phân tích nội dung chi tiết và kháng cáo nếu có
  const appealMarker = '[KHÁNG CÁO]:';
  const hasAppeal = report.Detail && report.Detail.includes(appealMarker);
  let cleanDetail = report.Detail || '';
  let appealReason = '';
  if (hasAppeal) {
    const parts = report.Detail.split(appealMarker);
    cleanDetail = parts[0]?.trim() || '';
    appealReason = parts[1]?.trim() || '';
  }

  useEffect(() => {
    // Only fetch if post data not already included in report
    if (!report.Post) {
      loadPostDetails();
    }
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

  const handleAcceptAppealAction = async () => {
    try {
      setActioning(true);
      await acceptReportAppeal(report.Id);
      if (onAcceptAppeal) onAcceptAppeal(report.Id);
      onClose();
    } catch (err) {
      setError('Lỗi khi chấp thuận kháng cáo: ' + err.message);
    } finally {
      setActioning(false);
    }
  };

  const handleRejectAppealAction = async () => {
    try {
      setActioning(true);
      await rejectReportAppeal(report.Id);
      if (onRejectAppeal) onRejectAppeal(report.Id);
      onClose();
    } catch (err) {
      setError('Lỗi khi từ chối kháng cáo: ' + err.message);
    } finally {
      setActioning(false);
    }
  };

  const handleDelete = async () => {
    if (onDelete) {
      await onDelete(report.Id);
      onClose();
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

  const reporter = report.ReporterUser;

  return (
    <div className="report-modal-overlay" onClick={onClose}>
      <div className="report-modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="report-modal-header">
          <h2>Chi tiết báo cáo #{report.Id}</h2>
          <button className="btn-close" onClick={onClose}>✕</button>
        </div>

        {error && <div className="report-modal-error">{error}</div>}

        <div className="report-modal-body">
          {/* Section 1: Post Content */}
          <div className="report-section">
            <h3 className="section-title">📝 Nội dung bài viết bị báo cáo</h3>
            {loading ? (
              <div className="loading-skeleton">
                <div className="skeleton-line" style={{ width: '80%' }} />
                <div className="skeleton-line" style={{ width: '100%' }} />
                <div className="skeleton-line" style={{ width: '90%' }} />
              </div>
            ) : post ? (
              <div className="post-preview">
                {/* Post Author Header */}
                <div className="post-header">
                  <div className="post-author-info">
                    <img
                      className="post-author-avatar"
                      src={post.UserProfilePictureUrl || `${DEFAULT_AVATAR}${encodeURIComponent(post.UserFullName || post.UserName || 'U')}`}
                      alt="Avatar"
                      onError={(e) => { e.target.src = `${DEFAULT_AVATAR}U`; }}
                    />
                    <div className="post-author-details">
                      <strong className="post-author-name">{post.UserFullName || post.UserName || 'Người dùng'}</strong>
                      {post.UserName && (
                        <span className="post-author-username">@{post.UserName}</span>
                      )}
                    </div>
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

                {/* Post Content */}
                <div className="post-content">
                  {post.Content}
                </div>

                {/* Post Image */}
                {post.ImageUrl && (
                  <div className="post-image">
                    <img src={post.ImageUrl} alt="Post content" />
                  </div>
                )}

                {/* Post Stats Bar */}
                <div className="post-stats-bar">
                  <span className="post-stat" title="Lượt thích">
                    👍 {post.LikesCount ?? 0} lượt thích
                  </span>
                  <span className="post-stat" title="Bình luận">
                    💬 {post.CommentsCount ?? 0} bình luận
                  </span>
                  {post.GroupId && (
                    <span className="post-stat post-stat-group" title="Bài viết trong nhóm">
                      📂 Nhóm #{post.GroupId}
                    </span>
                  )}
                  {post.IsShared && (
                    <span className="post-stat post-stat-shared" title="Bài chia sẻ">
                      🔄 Bài chia sẻ
                    </span>
                  )}
                </div>
              </div>
            ) : (
              <div className="post-not-found">
                ⚠️ Không thể tải bài viết (có thể đã bị xóa)
              </div>
            )}
          </div>

          {/* Section 2: Reporter Info */}
          <div className="report-section">
            <h3 className="section-title">🚩 Người báo cáo</h3>
            {reporter ? (
              <div className="user-info-card">
                <img
                  className="user-info-avatar"
                  src={reporter.ProfilePictureUrl || `${DEFAULT_AVATAR}${encodeURIComponent(reporter.FullName || reporter.UserName || 'U')}`}
                  alt="Reporter avatar"
                  onError={(e) => { e.target.src = `${DEFAULT_AVATAR}U`; }}
                />
                <div className="user-info-details">
                  <strong className="user-info-name">{reporter.FullName || reporter.UserName || 'Người dùng'}</strong>
                  {reporter.UserName && (
                    <span className="user-info-username">@{reporter.UserName}</span>
                  )}
                  {reporter.Email && (
                    <span className="user-info-email">{reporter.Email}</span>
                  )}
                </div>
              </div>
            ) : (
              <div className="user-info-card user-info-unknown">
                <span>Không có thông tin người báo cáo</span>
              </div>
            )}
          </div>

          {/* Section 3: Report Details */}
          <div className="report-section">
            <h3 className="section-title">🚨 Chi tiết báo cáo</h3>
            <div className="report-details">
              <div className="detail-row">
                <span className="detail-label">Lý do:</span>
                <span className="detail-value">{ReasonMap[report.Reason] || 'Unknown'}</span>
              </div>
              <div className="detail-row">
                <span className="detail-label">Chi tiết:</span>
                <span className="detail-value report-detail-text">{cleanDetail || 'Không có chi tiết'}</span>
              </div>
              <div className="detail-row">
                <span className="detail-label">Ngày báo cáo:</span>
                <span className="detail-value">
                  {new Date(report.CreatedAt).toLocaleDateString('vi-VN', {
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric',
                    hour: '2-digit',
                    minute: '2-digit'
                  })}
                </span>
              </div>
            </div>
          </div>

          {/* Section 3.5: Appeal Information */}
          {(hasAppeal || report.Status === 3 || report.Status === 4 || report.Status === 5) && (
            <div className="report-section appeal-highlight-section">
              <h3 className="section-title">⚖️ Thông tin kháng cáo từ tác giả bài viết</h3>
              <div className="appeal-box">
                <p className="appeal-text">
                  <strong>Lý do kháng cáo:</strong> {appealReason || "Tác giả bài viết yêu cầu xem xét lại việc gỡ bài."}
                </p>
                <span className="appeal-status-tag">
                  {report.Status === 3 && "⏳ Đang chờ Quản trị viên xem xét"}
                  {report.Status === 4 && "✅ Đã chấp thuận (Bài viết đã được khôi phục)"}
                  {report.Status === 5 && "❌ Đã bị bác bỏ (Tiếp tục gỡ bài)"}
                  {report.Status === 1 && "⚠️ Đã ghi nhận kháng cáo"}
                </span>
              </div>
            </div>
          )}

          {/* Section 4: Action Buttons */}
          <div className="report-section">
            <h3 className="section-title">⚙️ Hành động</h3>

            {report.Status === 1 && (
              <div className="report-status-notice approved-notice">
                <span>✅ Báo cáo này đã được duyệt. Bài viết vi phạm đã bị gỡ xuống.</span>
              </div>
            )}

            {report.Status === 2 && (
              <div className="report-status-notice rejected-notice">
                <span>❌ Báo cáo này đã bị từ chối.</span>
              </div>
            )}

            {report.Status === 3 && (
              <div className="report-status-notice" style={{ background: '#fff4e6', color: '#d9480f', border: '1px solid #ffd8a8' }}>
                <span>⚖️ Tác giả bài viết đã gửi đơn kháng cáo. Vui lòng xem xét khôi phục hoặc bác bỏ.</span>
              </div>
            )}

            {report.Status === 4 && (
              <div className="report-status-notice" style={{ background: '#e6fcf5', color: '#0ca678', border: '1px solid #b2f2bb' }}>
                <span>🎉 Kháng cáo đã được chấp thuận! Bài viết đã được khôi phục trên hệ thống.</span>
              </div>
            )}

            {report.Status === 5 && (
              <div className="report-status-notice rejected-notice">
                <span>🚫 Kháng cáo của người dùng đã bị bác bỏ. Bài viết tiếp tục bị ẩn.</span>
              </div>
            )}

            <div className="report-actions-modal">
              {report.Status === 0 && (
                <>
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
                </>
              )}

              {report.Status === 3 && (
                <>
                  <button
                    className="btn-approve-modal btn-appeal-accept"
                    onClick={handleAcceptAppealAction}
                    disabled={actioning}
                  >
                    {actioning ? '⏳ Đang xử lý...' : '✓ Chấp thuận kháng cáo (Khôi phục bài)'}
                  </button>
                  <button
                    className="btn-reject-modal btn-appeal-reject"
                    onClick={handleRejectAppealAction}
                    disabled={actioning}
                  >
                    {actioning ? '⏳ Đang xử lý...' : '✕ Bác bỏ kháng cáo'}
                  </button>
                </>
              )}

              {onDelete && (
                <button
                  className="btn-delete-modal"
                  onClick={handleDelete}
                  disabled={actioning}
                >
                  🗑️ Xóa báo cáo này
                </button>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}
