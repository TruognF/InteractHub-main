import { useState, useEffect } from 'react';
import { getReportById, submitReportAppeal } from '../api';
import '../styles/ReportDetailModal.css';

const DEFAULT_AVATAR = 'https://ui-avatars.com/api/?background=667eea&color=fff&size=80&name=';

const REASON_LABELS = {
  0: 'Nội dung gây hại',
  1: 'Thư rác',
  2: 'Quấy rối/Tấn công',
  3: 'Nội dung bạo lực',
  4: 'Nội dung người lớn',
  5: 'Tin giả/Sai lệch',
  6: 'Khác'
};

export default function UserAppealModal({ reportId, onClose, onSuccess }) {
  const [report, setReport] = useState(null);
  const [loading, setLoading] = useState(true);
  const [appealReason, setAppealReason] = useState('');
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState('');
  const [successMsg, setSuccessMsg] = useState('');

  useEffect(() => {
    if (reportId) {
      loadReport();
    }
  }, [reportId]);

  const loadReport = async () => {
    try {
      setLoading(true);
      setError('');
      const data = await getReportById(reportId);
      setReport(data);
    } catch (err) {
      setError(err.message || 'Không thể tải thông tin báo cáo');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    if (!appealReason.trim()) {
      setError('Vui lòng nhập lý do kháng cáo');
      return;
    }
    if (appealReason.trim().length < 10) {
      setError('Lý do kháng cáo phải có ít nhất 10 ký tự để Quản trị viên đối chiếu');
      return;
    }

    try {
      setSubmitting(true);
      setError('');
      const res = await submitReportAppeal(reportId, appealReason.trim());
      setSuccessMsg('Đã gửi kháng cáo thành công! Quản trị viên sẽ xem xét lại bài viết của bạn.');
      setReport(res || { ...report, Status: 3 });
      if (onSuccess) onSuccess();
    } catch (err) {
      setError(err.message || 'Không thể gửi đơn kháng cáo');
    } finally {
      setSubmitting(false);
    }
  };

  const appealMarker = '[KHÁNG CÁO]:';
  const hasExistingAppeal = report?.Detail && report.Detail.includes(appealMarker);
  let cleanDetail = report?.Detail || '';
  let existingAppealText = '';
  if (hasExistingAppeal) {
    const parts = report.Detail.split(appealMarker);
    cleanDetail = parts[0]?.trim() || '';
    existingAppealText = parts[1]?.trim() || '';
  }

  const post = report?.Post;

  return (
    <div className="report-modal-overlay" onClick={onClose}>
      <div className="report-modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="report-modal-header" style={{ background: 'linear-gradient(135deg, #e67e22 0%, #d35400 100%)' }}>
          <h2>⚖️ Kháng Cáo Bài Viết Bị Gỡ #{reportId}</h2>
          <button className="btn-close" onClick={onClose}>✕</button>
        </div>

        <div className="report-modal-body">
          {error && <div className="report-modal-error">{error}</div>}
          {successMsg && (
            <div style={{ color: '#155724', background: '#d4edda', border: '1px solid #c3e6cb', padding: '12px 16px', borderRadius: '8px', marginBottom: '16px', fontWeight: '500' }}>
              {successMsg}
            </div>
          )}

          {loading ? (
            <div className="loading-skeleton" style={{ padding: '30px' }}>
              <div className="skeleton-line" style={{ width: '80%' }} />
              <div className="skeleton-line" style={{ width: '100%' }} />
              <div className="skeleton-line" style={{ width: '90%' }} />
            </div>
          ) : report ? (
            <>
              {/* Section 1: Post Content (Giống giao diện bên Admin Report) */}
              <div className="report-section">
                <h3 className="section-title">📝 Nội dung bài viết bị báo cáo</h3>
                {post ? (
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
                    {post.Content && (
                      <div className="post-content">
                        {post.Content}
                      </div>
                    )}

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
                    ⚠️ Không thể tải chi tiết bài viết (có thể bài viết đã bị xóa khỏi hệ thống)
                  </div>
                )}
              </div>

              {/* Section 2: Lý do vi phạm */}
              <div className="report-section">
                <h3 className="section-title">🚨 Lý do bị gỡ bài</h3>
                <div className="report-details">
                  <div className="detail-row">
                    <span className="detail-label">Lý do vi phạm:</span>
                    <span className="detail-value" style={{ color: '#e74c3c', fontWeight: 'bold' }}>
                      {REASON_LABELS[report.Reason] || 'Vi phạm tiêu chuẩn cộng đồng'}
                    </span>
                  </div>
                  {cleanDetail && (
                    <div className="detail-row">
                      <span className="detail-label">Chi tiết phản ánh:</span>
                      <span className="detail-value report-detail-text">{cleanDetail}</span>
                    </div>
                  )}
                  <div className="detail-row">
                    <span className="detail-label">Ngày xử lý gỡ bài:</span>
                    <span className="detail-value">
                      {new Date(report.ReviewedAt || report.CreatedAt).toLocaleDateString('vi-VN', {
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

              {/* Section 3: Trạng thái Kháng cáo */}
              {report.Status === 3 && (
                <div className="report-section appeal-highlight-section">
                  <h3 className="section-title">⏳ Đơn kháng cáo đang được xem xét</h3>
                  <div className="appeal-box">
                    <p className="appeal-text">
                      <strong>Lý do bạn đã gửi:</strong> {existingAppealText || 'Đã gửi yêu cầu xem xét lại bài viết.'}
                    </p>
                    <span className="appeal-status-tag">
                      Đang chờ Quản trị viên đối chiếu và xử lý
                    </span>
                  </div>
                </div>
              )}

              {report.Status === 4 && (
                <div className="report-section appeal-highlight-section" style={{ background: '#e6fcf5 !important', borderLeftColor: '#0ca678 !important' }}>
                  <h3 className="section-title" style={{ color: '#0ca678' }}>🎉 Kháng cáo đã được chấp thuận!</h3>
                  <div className="appeal-box" style={{ borderColor: '#b2f2bb' }}>
                    <p className="appeal-text">
                      Quản trị viên đã xem xét và khôi phục bài viết của bạn về Bảng tin.
                    </p>
                  </div>
                </div>
              )}

              {report.Status === 5 && (
                <div className="report-section appeal-highlight-section" style={{ background: '#fdf2f2 !important', borderLeftColor: '#e03131 !important' }}>
                  <h3 className="section-title" style={{ color: '#e03131' }}>❌ Kháng cáo đã bị từ chối</h3>
                  <div className="appeal-box" style={{ borderColor: '#ffc9c9' }}>
                    <p className="appeal-text">
                      Sau khi đối chiếu lại, Quản trị viên nhận thấy bài viết thực sự vi phạm tiêu chuẩn nên không thể khôi phục.
                    </p>
                  </div>
                </div>
              )}

              {/* Section 4: Form gửi kháng cáo */}
              {(report.Status === 1 || !report.Status) && !successMsg && (
                <div className="report-section">
                  <h3 className="section-title">✍️ Đơn kháng cáo bài viết</h3>
                  <form onSubmit={handleSubmit}>
                    <div style={{ marginBottom: '15px' }}>
                      <label style={{ display: 'block', fontSize: '14px', fontWeight: '600', marginBottom: '6px', color: '#2c3e50' }}>
                        Lý do bạn cho rằng bài viết không vi phạm:
                      </label>
                      <textarea
                        rows="4"
                        value={appealReason}
                        onChange={(e) => setAppealReason(e.target.value)}
                        placeholder="Mô tả chi tiết lý do tại sao bài viết này hợp lệ, không vi phạm tiêu chuẩn cộng đồng để Admin xem xét lại..."
                        style={{
                          width: '100%',
                          padding: '12px',
                          borderRadius: '8px',
                          border: '1px solid #ced4da',
                          fontSize: '14px',
                          boxSizing: 'border-box',
                          resize: 'vertical',
                          lineHeight: '1.5'
                        }}
                        disabled={submitting}
                      />
                      <small style={{ color: '#7f8c8d', fontSize: '12px', marginTop: '4px', display: 'block' }}>
                        Tối thiểu 10 ký tự. Vui lòng trình bày rõ ràng, văn minh.
                      </small>
                    </div>

                    <div style={{ display: 'flex', gap: '10px', justifyContent: 'flex-end' }}>
                      <button
                        type="button"
                        onClick={onClose}
                        className="btn-reject-modal"
                        style={{ width: 'auto', padding: '10px 20px', background: '#e9ecef', color: '#495057' }}
                        disabled={submitting}
                      >
                        Đóng
                      </button>
                      <button
                        type="submit"
                        disabled={submitting}
                        className="btn-approve-modal"
                        style={{ width: 'auto', padding: '10px 24px', background: '#e67e22' }}
                      >
                        {submitting ? '⏳ Đang gửi...' : 'Gửi đơn kháng cáo'}
                      </button>
                    </div>
                  </form>
                </div>
              )}

              {(report.Status === 3 || report.Status === 4 || report.Status === 5 || successMsg) && (
                <div style={{ display: 'flex', justifyContent: 'flex-end', marginTop: '16px' }}>
                  <button
                    type="button"
                    onClick={onClose}
                    style={{
                      padding: '10px 24px',
                      borderRadius: '8px',
                      border: '1px solid #ced4da',
                      background: '#fff',
                      cursor: 'pointer',
                      fontWeight: '600'
                    }}
                  >
                    Đóng
                  </button>
                </div>
              )}
            </>
          ) : (
            <div style={{ textAlign: 'center', padding: '30px', color: '#e74c3c' }}>
              Không tìm thấy thông tin báo cáo tương ứng.
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
