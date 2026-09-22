import { useState, useEffect, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { createGroup, getAcceptedFriends, getUser } from '../api';
import { useGroups } from '../contexts/GroupsContext';
import Header from '../components/Header';
import '../styles/CreateGroupPage.css';

export default function CreateGroupPage() {
  const [groupName, setGroupName] = useState('');
  const [description, setDescription] = useState('');
  const [coverImages, setCoverImages] = useState([]);
  const [currentUser, setCurrentUser] = useState(null);
  const [friends, setFriends] = useState([]);
  const [selectedFriendIds, setSelectedFriendIds] = useState([]);
  const [loadingFriends, setLoadingFriends] = useState(true);
  const [creating, setCreating] = useState(false);
  const navigate = useNavigate();
  const { refreshGroups } = useGroups();

  useEffect(() => {
    const userData = JSON.parse(localStorage.getItem('user') || 'null');
    setCurrentUser(userData);
    const loadFriends = async () => {
      if (!userData?.Id) {
        setFriends([]);
        setLoadingFriends(false);
        return;
      }
      try {
        const accepted = await getAcceptedFriends(userData.Id, 1, 100);
        const friendIds = (accepted || [])
          .map((f) => f.FriendId || f.friendId)
          .filter(Boolean);

        const users = await Promise.all(
          friendIds.map(async (id) => {
            try {
              return await getUser(id);
            } catch {
              return null;
            }
          })
        );
        setFriends(users.filter(Boolean));
      } catch (err) {
        console.error('Failed to load friends for group creation', err);
        setFriends([]);
      } finally {
        setLoadingFriends(false);
      }
    };

    loadFriends();
  }, []);

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.dispatchEvent(new Event('tokenUpdated'));
    navigate('/login');
  };

  const handleCreateGroup = async () => {
    if (!groupName.trim()) {
      alert('Vui lòng nhập tên nhóm');
      return;
    }

    try {
      setCreating(true);
      await createGroup({
        name: groupName.trim(),
        description: description.trim(),
        imageUrl: coverImages.length > 0 ? JSON.stringify(coverImages) : null,
        memberIds: selectedFriendIds
      });
      await refreshGroups();
      navigate('/group', { state: { tab: 'my-groups' } });
    } catch (err) {
      console.error('Failed to create group', err);
      alert('Tạo nhóm thất bại. Vui lòng thử lại.');
    } finally {
      setCreating(false);
    }
  };

  const toggleFriend = (friendId) => {
    setSelectedFriendIds((prev) =>
      prev.includes(friendId) ? prev.filter((id) => id !== friendId) : [...prev, friendId]
    );
  };

  const handleImageChange = (e) => {
    const files = Array.from(e.target.files);
    if (!files.length) return;
    
    if (coverImages.length + files.length > 3) {
      alert('Chỉ được chọn tối đa 3 ảnh bìa.');
      return;
    }

    files.forEach(file => {
      if (file.size > 5 * 1024 * 1024) {
        alert('Có file quá lớn. Vui lòng chọn ảnh dưới 5MB.');
        return;
      }
      const reader = new FileReader();
      reader.onloadend = () => {
        setCoverImages(prev => [...prev, reader.result]);
      };
      reader.readAsDataURL(file);
    });
    
    // Clear the input so same files can be selected again if removed
    e.target.value = null;
  };

  const removeCoverImage = (index) => {
    setCoverImages(prev => prev.filter((_, i) => i !== index));
  };

  const selectedCount = useMemo(() => selectedFriendIds.length + 1, [selectedFriendIds]);

  return (
    <div className="create-group-wrapper">
      <Header onLogout={handleLogout} />
      
      <div className="create-group-container">
        <aside className="create-group-sidebar">
          <button 
            onClick={() => navigate(-1)}
            style={{
              width: '100%',
              padding: '12px 16px',
              marginBottom: '16px',
              backgroundColor: '#6c757d',
              color: 'white',
              border: 'none',
              borderRadius: '8px',
              cursor: 'pointer',
              fontSize: '14px',
              fontWeight: '600',
              display: 'flex',
              alignItems: 'center',
              gap: '8px'
            }}
          >
            <i className="fa-solid fa-arrow-left"></i>
            <span>Quay lại</span>
          </button>
          
          <div className="sidebar-section">
            <h3 className="sidebar-title">Tạo Nhóm</h3>
          </div>

          <div className="member-item current-member">
            <div className="member-avatar"><i className="fa-solid fa-user"></i></div>
            <div className="member-info">
              <p className="member-name">{currentUser?.FullName || currentUser?.fullName || 'Người dùng'}</p>
              <p className="member-status">Quản lý nhóm</p>
            </div>
          </div>

          <input
            type="text"
            placeholder="Nhập tên nhóm"
            value={groupName}
            onChange={(e) => setGroupName(e.target.value)}
            className="group-name-input"
          />

          <textarea
            placeholder="Mô tả nhóm (tùy chọn)"
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            className="group-name-input"
            style={{ minHeight: '80px', resize: 'vertical' }}
          />

          <p className="member-status">Bạn có thể tạo nhóm với tên và mô tả, hoặc chọn thêm bạn bè.</p>
          <button className="btn-create-group" onClick={handleCreateGroup} disabled={creating}>
            {creating ? 'Đang tạo...' : 'Tạo'}
          </button>
        </aside>

        <main className="create-group-main">
          {/* Cover Image Section */}
          <div className="group-cover" style={{ height: 'auto', minHeight: '200px', display: 'flex', flexWrap: 'wrap', gap: '10px', padding: '10px', background: coverImages.length > 0 ? 'transparent' : '' }}>
            {coverImages.length > 0 ? (
              <div style={{ display: 'flex', width: '100%', gap: '10px', position: 'relative' }}>
                {coverImages.map((img, idx) => (
                  <div key={idx} style={{ flex: 1, position: 'relative', height: '200px' }}>
                    <img src={img} alt={`Cover Preview ${idx}`} style={{ width: '100%', height: '100%', objectFit: 'cover', borderRadius: '12px' }} />
                    <button onClick={() => removeCoverImage(idx)} className="cover-remove-button" style={{ position: 'absolute', top: '10px', right: '10px', padding: '6px 10px', fontSize: '12px' }}>Xóa</button>
                  </div>
                ))}

                {coverImages.length < 3 && (
                  <div style={{ position: 'absolute', top: '10px', left: '10px', zIndex: 10 }}>
                    <label className="cover-file-button small">
                      Thêm ảnh ({coverImages.length}/3)
                      <input type="file" accept="image/*" multiple onChange={handleImageChange} className="cover-file-input" />
                    </label>
                  </div>
                )}
              </div>
            ) : (
              <div className="cover-placeholder">
                <div className="cover-upload-controls">
                  <span style={{ color: '#666', fontSize: '14px', marginBottom: '8px' }}>Ảnh bìa nhóm (Tối đa 3 ảnh)</span>
                  <label className="cover-file-button">
                    Thêm ảnh bìa
                    <input type="file" accept="image/*" multiple onChange={handleImageChange} className="cover-file-input" />
                  </label>
                </div>
              </div>
            )}
          </div>

          <div className="group-info-section">
            <h2 className="group-display-name">{groupName || 'Tên nhóm'}</h2>
            {description && <p style={{ fontSize: '14px', color: '#555', marginTop: '0', marginBottom: '10px' }}>{description}</p>}
            <p className="group-member-count">{selectedCount} thành viên (bao gồm bạn)</p>

            <section className="create-post-section">
              <div className="create-post-header">
                <p className="create-post-prompt">Thêm bạn bè vào nhóm (tùy chọn)</p>
              </div>

              {loadingFriends ? (
                <p>Đang tải danh sách bạn bè...</p>
              ) : friends.length === 0 ? (
                <p>Bạn chưa có bạn bè nào để thêm.</p>
              ) : (
                <div>
                  {friends.map((friend) => {
                    const friendId = friend.Id || friend.id;
                    const checked = selectedFriendIds.includes(friendId);
                    return (
                      <label key={friendId} className="member-item" style={{ marginBottom: 10 }}>
                        <input
                          type="checkbox"
                          checked={checked}
                          onChange={() => toggleFriend(friendId)}
                          style={{ marginRight: 8 }}
                        />
                        <span className="member-name">
                          {friend.FullName || friend.fullName || friend.UserName || friend.userName}
                        </span>
                      </label>
                    );
                  })}
                </div>
              )}
            </section>
          </div>
        </main>
      </div>
    </div>
  );
}
