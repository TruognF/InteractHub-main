import { useState, useEffect } from 'react';
import { createPostReport } from '../api';
import '../styles/ReportPostModal.css';

const REPORT_REASONS = {
  HarmfulContent: 'Nội dung gây hại',
  Spam: 'Thư rác',
  Harassment: 'Quấy rối/Tấn công',
  ViolentContent: 'Nội dung bạo lực',
  AdultContent: 'Nội dung người lớn',
  FakeNews: 'Tin giả/Sai lệch',
  Other: 'Khác'
};

export default function ReportPostModal({ open, postId, postAuthorId, currentUserId, onClose, onSuccess }) {
  const [reason, setReason] = useState('');
  const [detail, setDetail] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');

    if (!reason) {
      setError('Vui lòng chọn lý do báo cáo');
      return;
    }

    if (detail.trim().length < 10) {
      setError('Chi tiết báo cáo phải có ít nhất 10 ký tự');
      return;
    }

    setLoading(true);

    try {
      const numericalReason = Object.keys(REPORT_REASONS).indexOf(reason);
      await createPostReport(postId, numericalReason, detail.trim());
      onSuccess?.();
      onClose();
    } catch (err) {
      setError(err.message || 'Báo cáo thất bại, vui lòng thử lại');
    } finally {
      setLoading(false);
    }
  };

  const handleOverlayClick = (e) => {
    if (e.target.classList.contains('report-modal-overlay')) {
      onClose();
    }
  };

  const handleEscapeKey = (e) => {
    if (e.key === 'Escape') {
      onClose();
    }
  };

  // Clear form state when modal opens
  useEffect(() => {
    if (open) {
      setReason('');
      setDetail('');
      setError('');
    }
  }, [open]);

  // Prevent body scroll when modal is open
  useEffect(() => {
    const handleEscape = handleEscapeKey;
    document.addEventListener('keydown', handleEscape);
    return () => document.removeEventListener('keydown', handleEscape);
  }, []);

  return !open ? null : (
    <div className="report-modal-overlay" onClick={handleOverlayClick}>
      <div className="report-modal">
        <div className="report-modal-header">
          <h3>Báo cáo bài viết</h3>
          <button
            className="report-modal-close"
            onClick={onClose}
            disabled={loading}
            aria-label="Close modal"
          >
            ✕
          </button>
        </div>

        <form onSubmit={handleSubmit} className="report-form">
          {error && <div className="report-error">{error}</div>}

          <div className="form-group">
            <label htmlFor="reason">Lý do báo cáo:</label>
            <select
              id="reason"
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              className="form-input report-select"
              disabled={loading}
              required
            >
              <option value="">-- Chọn lý do --</option>
              {Object.entries(REPORT_REASONS).map(([key, label]) => (
                <option key={key} value={key}>
                  {label}
                </option>
              ))}
            </select>
          </div>

          <div className="form-group">
            <label htmlFor="detail">
              Chi tiết báo cáo (tối thiểu 10 ký tự):
            </label>
            <textarea
              id="detail"
              value={detail}
              onChange={(e) => setDetail(e.target.value)}
              placeholder="Mô tả chi tiết lý do báo cáo bài viết này..."
              className="form-input report-textarea"
              rows="4"
              disabled={loading}
              required
              minLength="10"
              maxLength="500"
            />
            <div className="char-count">
              {detail.length}/500
            </div>
          </div>

          <div className="report-modal-footer">
            <button
              type="button"
              onClick={onClose}
              className="btn-cancel"
              disabled={loading}
            >
              Hủy
            </button>
            <button
              type="submit"
              className="btn-submit"
              disabled={loading || !reason || detail.trim().length < 10}
            >
              {loading ? 'Đang gửi...' : 'Gửi báo cáo'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
