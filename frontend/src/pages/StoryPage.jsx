import { useEffect, useState } from 'react';
import { useNavigate, useParams, useLocation } from 'react-router-dom';
import Header from '../components/Header';
import { getStoryById, getStories, deleteStory, createStory } from '../api';
import '../styles/StoryPage.css';
import { normalizeStoryPayload, isStoryActive } from '../utils/storyRealtime';

export default function StoryPage() {
  const [story, setStory] = useState(null);
  const [storiesList, setStoriesList] = useState([]);
  const sortStoriesByNewest = (list = []) => {
    return [...list].sort(
      (a, b) =>
        new Date(b?.CreatedAt ?? b?.createdAt ?? 0) -
        new Date(a?.CreatedAt ?? a?.createdAt ?? 0)
    );
  };
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [messageInput, setMessageInput] = useState('');
  const [newStoryContent, setNewStoryContent] = useState('');
  const [newStoryFile, setNewStoryFile] = useState(null);
  const [storyImagePreview, setStoryImagePreview] = useState('');
  const [creatingStory, setCreatingStory] = useState(false);
  const [showCreateStoryModal, setShowCreateStoryModal] = useState(false);
  const [currentUser, setCurrentUser] = useState(null);
  const [showStoryOptions, setShowStoryOptions] = useState(false);
  const { storyId, userId } = useParams();
  const navigate = useNavigate();
  const location = useLocation();

  useEffect(() => {
    const userData = JSON.parse(localStorage.getItem('user'));
    setCurrentUser(userData);
  }, []);

  useEffect(() => {
    const loadStories = async () => {
      try {
        // Luôn dùng feed giống Home (bản thân + bạn đã kết bạn). Không dùng chỉ getStoriesByUserId — route /story/user/:id sẽ chỉ có 1 user trong list và phải bấm sang /story/:id mới refetch full feed.
        const storiesData = await getStories();
        let activeStories = (storiesData || []).filter((s) => {
          return !s.ExpireAt || new Date(s.ExpireAt) > new Date();
        });
        activeStories = sortStoriesByNewest(activeStories);
        setStoriesList(activeStories);

        // URL /story/user/:userId (không có storyId): mặc định xem tin mới nhất — trừ khi vừa xóa tin (state.skipAutoStory).
        if (userId && !storyId && !location.state?.skipAutoStory) {
          const uid = String(userId);
          const theirs = activeStories.filter(
            (s) => String(s.UserId ?? s.userId) === uid
          );
          if (theirs.length > 0) {
            setStory(theirs[0]);
          }
        }
      } catch (err) {
        console.error('Error loading story list:', err);
      }
    };

    loadStories();
  }, [userId]);

  useEffect(() => {
    const loadStory = async () => {
      setLoading(true);
      setError('');

      try {
        if (!storyId || userId) {
          setLoading(false);
          return;
        }

        const storyData = await getStoryById(storyId);
        if (!storyData) {
          setError('Không tìm thấy story');
        } else {
          setStory(storyData);
        }
      } catch (err) {
        console.error('Error loading story:', err);
        setError(err.message || 'Lỗi khi tải story');
      } finally {
        setLoading(false);
      }
    };

    loadStory();
  }, [storyId, userId]);

  useEffect(() => {
    const refetchAllStories = () => {
      getStories()
        .then((data) => {
          const activeStories = (data || []).filter((s) => {
            return !s.ExpireAt || new Date(s.ExpireAt) > new Date();
          });
          setStoriesList(sortStoriesByNewest(activeStories));
        })
        .catch(() => {});
    };

    const onStoryCreated = (e) => {
      const s = normalizeStoryPayload(e.detail);
      if (!s || !isStoryActive(s)) return;

      if (userId) {
        if (String(s.UserId) !== String(userId)) return;
        setStoriesList((prev) => {
          const sid = s.Id ?? s.id;
          if (prev.some((x) => (x.Id ?? x.id) === sid)) return prev;
          return sortStoriesByNewest([s, ...prev]);
        });
        return;
      }

      refetchAllStories();
    };

    const onStoryDeleted = (e) => {
      const sid = e.detail?.storyId ?? e.detail?.StoryId;
      if (sid == null) return;

      setStoriesList((prev) => prev.filter((item) => (item.Id ?? item.id) !== sid));
      setStory((cur) => {
        const cid = cur?.Id ?? cur?.id;
        if (cid == null || cid !== sid) return cur;
        return null;
      });

      if (storyId != null && Number(storyId) === Number(sid) && currentUser?.Id) {
        navigate(`/story/user/${currentUser.Id}`, {
          replace: true,
          state: { skipAutoStory: true }
        });
      }
    };

    window.addEventListener('signalr:story-created', onStoryCreated);
    window.addEventListener('signalr:story-deleted', onStoryDeleted);
    return () => {
      window.removeEventListener('signalr:story-created', onStoryCreated);
      window.removeEventListener('signalr:story-deleted', onStoryDeleted);
    };
  }, [userId, storyId, navigate, currentUser]);

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.dispatchEvent(new Event('tokenUpdated'));
    navigate('/login');
  };

  const handleDeleteStory = async () => {
    if (!story || currentUser?.Id !== story.UserId) return;
    if (!window.confirm('Bạn có chắc chắn muốn xóa tin này?')) return;

    try {
      await deleteStory(story.Id);
      const sid = story.Id;
      setStoriesList((prev) => prev.filter((item) => (item.Id ?? item.id) !== sid));
      setStory(null);
      setShowStoryOptions(false);
      setError('');
      const uid = currentUser?.Id;
      if (uid) {
        navigate(`/story/user/${uid}`, { replace: true, state: { skipAutoStory: true } });
      }
    } catch (err) {
      console.error('Error deleting story:', err);
      setError(`Lỗi xóa tin: ${err.message}`);
    }
  };

  const handleStoryFileChange = (e) => {
    const file = e.target.files?.[0];
    if (!file) return;

    const validTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
    const maxSize = 5 * 1024 * 1024;

    if (file.size > maxSize) {
      setError('Kích thước hình ảnh không được vượt quá 5MB');
      return;
    }

    if (!validTypes.includes(file.type)) {
      setError('Chỉ hỗ trợ các định dạng: JPEG, PNG, GIF, WebP');
      return;
    }

    const reader = new FileReader();
    reader.onload = (event) => {
      setNewStoryFile(file);
      setStoryImagePreview(event.target?.result || '');
      setError('');
    };
    reader.onerror = () => {
      setError('Lỗi đọc tệp hình ảnh');
    };
    reader.readAsDataURL(file);
  };

  const handleCreateStory = async (e) => {
    e.preventDefault();
    if (!newStoryContent.trim() && !newStoryFile) {
      setError('Vui lòng nhập nội dung hoặc chọn hình ảnh cho tin');
      return;
    }

    setCreatingStory(true);
    try {
      let imageBase64 = null;
      if (newStoryFile) {
        imageBase64 = storyImagePreview;
      }

      const expireAt = new Date(Date.now() + 24 * 60 * 60 * 1000).toISOString();
      const createdStory = await createStory({
        content: newStoryContent,
        imageUrl: imageBase64,
        expireAt
      });

      if (!createdStory) {
        throw new Error('Tạo tin thất bại');
      }

      setStoriesList((prev) => sortStoriesByNewest([createdStory, ...prev]));
      setStory(createdStory);
      setNewStoryContent('');
      setNewStoryFile(null);
      setStoryImagePreview('');
      setShowCreateStoryModal(false);
      setError('');
      navigate(`/story/${createdStory.Id}`);
    } catch (err) {
      console.error('Error creating story:', err);
      setError(err.message || 'Lỗi khi tạo tin');
    } finally {
      setCreatingStory(false);
    }
  };

  return (
    <div className="story-page-wrapper">
      <Header onLogout={handleLogout} />

      <div className="story-page-container">
        <aside className="story-sidebar">
          <div className="sidebar-top">
            <div className="sidebar-title-row">
              <h3>Tin</h3>
              <span className="sidebar-subtitle">Kho lưu trữ</span>
            </div>
            <div className="story-card-mini create-story-card-mini" onClick={() => setShowCreateStoryModal(true)}>
              <div className="story-icon-btn">+</div>
              <div>
                <p className="story-mini-title">Tạo tin</p>
              </div>
            </div>
          </div>

          <div className="stories-list-section">
            <p className="stories-list-heading">
              {userId ? `Tin của ${storiesList[0]?.UserName || 'người dùng'}` : 'Tất cả tin'}
            </p>
            <div className="stories-list">
              {storiesList.map((item) => (
                <div
                  key={item.Id}
                  className={`story-item ${item.Id.toString() === storyId ? 'active' : ''}`}
                  onClick={() => navigate(`/story/${item.Id}`)}
                >
                  <div className="story-avatar">
                    {item.UserProfilePictureUrl ? (
                      <img src={item.UserProfilePictureUrl} alt="Avatar" />
                    ) : (
                      <i className="fa-solid fa-user"></i>
                    )}
                  </div>
                  <div className="story-name-group">
                    <p className="story-name">{item.UserName || 'Người dùng'}</p>
                    <span className="story-time-small">{new Date(item.CreatedAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })}</span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </aside>

        <main className="story-main">
          {loading ? (
            <div className="story-empty">
              <p>Đang tải story...</p>
            </div>
          ) : error ? (
            <div className="story-empty">
              <p>{error}</p>
            </div>
          ) : story ? (
            <div className="story-viewer story-page-viewer">
              <div className="story-header">
                <div className="story-user-info">
                  <div className="story-user-avatar">
                    {story.UserProfilePictureUrl ? (
                      <img src={story.UserProfilePictureUrl} alt="Avatar" />
                    ) : (
                      <i className="fa-solid fa-user"></i>
                    )}
                  </div>
                  <div className="story-user-details">
                    <p className="story-username">{story.UserName || 'Người dùng'}</p>
                    <p className="story-time">
                      {new Date(story.CreatedAt).toLocaleString('vi-VN', {
                        hour: '2-digit',
                        minute: '2-digit',
                        day: '2-digit',
                        month: '2-digit',
                        year: 'numeric'
                      })}
                    </p>
                  </div>
                </div>
                {currentUser?.Id === story.UserId && (
                  <div className="story-options">
                    <button
                      type="button"
                      className="story-options-dot"
                      onClick={(e) => {
                        e.stopPropagation();
                        setShowStoryOptions((current) => !current);
                      }}
                      aria-label="Tùy chọn tin"
                    >
                      <i className="fa-solid fa-ellipsis-vertical"></i>
                    </button>
                    {showStoryOptions && (
                      <div className="story-options-dropdown">
                        <button
                          type="button"
                          className="menu-item delete"
                          onClick={(e) => {
                            e.stopPropagation();
                            handleDeleteStory();
                          }}
                        >
                          <i className="fa-solid fa-trash"></i> Xóa tin
                        </button>
                      </div>
                    )}
                  </div>
                )}
              </div>

              <div className="story-content-area">
                {story.ImageUrl ? (
                  <img src={story.ImageUrl} alt="Story" className="story-image-content" />
                ) : (
                  <div className="story-viewer-text-card">
                    <p>{story.Content || 'Không có nội dung.'}</p>
                  </div>
                )}
                {story.Content && story.ImageUrl && (
                  <p className="story-viewer-caption">{story.Content}</p>
                )}
              </div>
            </div>
          ) : storiesList.length > 0 ? (
            <div className="story-empty">
              <p>Chọn một tin trong danh sách bên trái để xem.</p>
            </div>
          ) : (
            <div className="story-empty">
              <p>Không có tin để hiển thị.</p>
            </div>
          )}
        </main>
      </div>

      {showCreateStoryModal && (
        <div className="modal-overlay" onClick={() => setShowCreateStoryModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Tạo tin</h2>
              <button className="modal-close-btn" onClick={() => setShowCreateStoryModal(false)}>
                ✕
              </button>
            </div>

            <form onSubmit={handleCreateStory} className="create-story-form">
              <div className="create-story-container">
                <div className="story-preview">
                  {storyImagePreview && (
                    <img src={storyImagePreview} alt="Preview" className="story-preview-image" />
                  )}
                  <div className="story-content-display">
                    <p className="story-text">{newStoryContent}</p>
                  </div>
                </div>

                <div className="story-form-inputs">
                  <textarea
                    value={newStoryContent}
                    onChange={(e) => setNewStoryContent(e.target.value)}
                    placeholder="Chia sẻ tin của bạn..."
                    className="story-textarea"
                    rows="4"
                  />

                  <div className="story-form-bottom">
                    <div className="file-input-wrapper">
                      <label htmlFor="story-image-input" className="file-input-label">
                        <i className="fa-solid fa-image"></i>
                        <span>Thêm hình ảnh</span>
                      </label>
                      <input
                        id="story-image-input"
                        type="file"
                        accept="image/jpeg,image/png,image/gif,image/webp"
                        onChange={handleStoryFileChange}
                        className="post-file-input"
                      />
                      {newStoryFile && (
                        <span className="file-selected">✓ {newStoryFile.name}</span>
                      )}
                    </div>

                    <button type="submit" className="btn-post" disabled={creatingStory}>
                      {creatingStory ? 'Đang đăng...' : 'Đăng tin'}
                    </button>
                  </div>
                </div>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
