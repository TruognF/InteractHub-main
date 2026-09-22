import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getAllUsers } from '../utils/userDataManager';
import Header from '../components/Header';
import '../styles/DebugPage.css';

export default function DebugPage() {
  const [allUsers, setAllUsers] = useState([]);
  const [currentUser, setCurrentUser] = useState(null);
  const [userDetails, setUserDetails] = useState({});
  const navigate = useNavigate();

  useEffect(() => {
    // Get all users
    const users = getAllUsers();
    setAllUsers(users);

    // Get current logged in user
    const current = JSON.parse(localStorage.getItem('user') || 'null');
    setCurrentUser(current);

    // Get details for each user
    const details = {};
    users.forEach(user => {
      const userData = JSON.parse(localStorage.getItem(`user_data_${user.Id}`) || '{}');
      details[user.Id] = {
        posts: userData.posts?.length || 0,
        friends: userData.friends?.length || 0,
        likedPosts: Object.keys(userData.likedPosts || {}).length,
        messages: Object.keys(userData.messages || {}).length
      };
    });
    setUserDetails(details);
  }, []);

  const handleClearAll = () => {
    if (window.confirm('Xóa tất cả dữ liệu? (Không thể hoàn tác)')) {
      localStorage.clear();
      window.location.reload();
    }
  };

  const handleLoginAs = (user) => {
    const userData = JSON.parse(localStorage.getItem(`user_data_${user.Id}`) || '{}');
    if (!userData.posts || !userData.friends) {
      const mockFriends = [
        { id: 'friend1', name: 'Nguyễn Văn A', fullName: 'Nguyễn Văn A', avatar: <i className="fa-solid fa-user"></i>, isActive: true },
        { id: 'friend2', name: 'Trần Thị B', fullName: 'Trần Thị B', avatar: <i className="fa-solid fa-user"></i>, isActive: false },
      ];
      const mockPosts = [
        {
          id: `post_${Date.now()}_1`,
          userId: user.Id,
          username: 'Tôi',
          userName: user.fullName,
          content: 'Hôm nay thời tiết đẹp quá!',
          imageUrl: 'https://via.placeholder.com/300',
          createdAt: new Date().toISOString(),
          likesCount: 10,
          commentsCount: 3,
          likedBy: []
        },
      ];
      userData.friends = mockFriends;
      userData.posts = mockPosts;
      userData.messages = userData.messages || {};
      userData.groupMemberships = userData.groupMemberships || [];
      userData.likedPosts = userData.likedPosts || {};
      localStorage.setItem(`user_data_${user.Id}`, JSON.stringify(userData));
    }
    
    localStorage.setItem('token', `mock_token_${user.Id}`);
    localStorage.setItem('user', JSON.stringify(user));
    navigate('/home');
  };

  return (
    <div className="debug-page">
      <Header onLogout={() => navigate('/login')} showControls={false} />
      
      <div className="debug-container">
        <h1>🐛 Debug - Quản Lý Tài Khoản</h1>

        {/* Current User */}
        {currentUser && (
          <section className="debug-section current-user">
            <h2><i className="fa-solid fa-user"></i> Tài khoản hiện tại:</h2>
            <div className="user-card highlighted">
              <p><strong>Tên:</strong> {currentUser.fullName}</p>
              <p><strong>Username:</strong> {currentUser.userName}</p>
              <p><strong>Email:</strong> {currentUser.email}</p>
              <p><strong>ID:</strong> {currentUser.Id}</p>
              <button className="btn-primary" onClick={() => navigate('/home')}>
                → Về Home
              </button>
            </div>
          </section>
        )}

        {/* All Users */}
        <section className="debug-section">
          <h2>📋 Tất cả tài khoản ({allUsers.length}):</h2>
          
          {allUsers.length === 0 ? (
            <p className="no-data">Chưa có tài khoản nào</p>
          ) : (
            <div className="users-list">
              {allUsers.map(user => (
                <div key={user.Id} className="user-card\">
                  <div className="user-info">
                    <p><strong><i className="fa-solid fa-user"></i> Tên:</strong> {user.fullName}</p>
                    <p><strong>📧 Username:</strong> {user.userName}</p>
                    <p><strong>📨 Email:</strong> {user.email}</p>
                    <p><strong>🔑 Password:</strong> {user.password}</p>
                    <p><strong>ID:</strong> <code>{user.Id}</code></p>
                    <p><strong>Tạo lúc:</strong> {new Date(user.createdAt).toLocaleString('vi-VN')}</p>
                  </div>
                  
                  <div className="user-stats">
                    <h4>📊 Thống kê:</h4>
                    <ul>
                      <li>Posts: {userDetails[user.Id]?.posts || 0}</li>
                      <li>Friends: {userDetails[user.Id]?.friends || 0}</li>
                      <li>Liked Posts: {userDetails[user.Id]?.likedPosts || 0}</li>
                      <li>Messages: {userDetails[user.Id]?.messages || 0}</li>
                    </ul>
                  </div>

                  <div className="user-actions">
                    <button 
                      className="btn-login"
                      onClick={() => handleLoginAs(user)}
                    >
                      🔓 Đăng nhập
                    </button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>

        {/* LocalStorage Info */}
        <section className="debug-section">
          <h2>💾 LocalStorage Keys:</h2>
          <div className="storage-info">
            {Object.keys(localStorage).map(key => (
              <div key={key} className="storage-item">
                <p><strong>{key}:</strong></p>
                <code>{JSON.stringify(JSON.parse(localStorage.getItem(key))).substring(0, 100)}...</code>
              </div>
            ))}
          </div>
        </section>

        {/* Actions */}
        <section className="debug-section actions">
          <h2>🎮 Hành động:</h2>
          <button className="btn-danger" onClick={handleClearAll}>
            🗑️ Xóa toàn bộ dữ liệu
          </button>
          <button className="btn-secondary" onClick={() => navigate('/register')}>
            ➕ Tạo tài khoản mới
          </button>
          <button className="btn-secondary" onClick={() => navigate('/login')}>
            🔑 Đăng nhập
          </button>
        </section>
      </div>
    </div>
  );
}
