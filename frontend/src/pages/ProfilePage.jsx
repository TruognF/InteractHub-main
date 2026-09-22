import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { getUserPosts, likePost, unlikePost, getUser, updateUser, uploadProfilePicture, deletePost, getCommentsByPost, createComment, updateComment, deleteComment, getPostById } from '../api';
import Header from '../components/Header';
import CommentSection from '../components/CommentSection';
import HashtagContent from '../components/HashtagContent';
import '../styles/ProfilePage.css';
import { mergeCommentIntoList } from '../utils/commentNormalize';
import { resizeImage } from '../utils/resizeImage';

export default function ProfilePage() {
  const [currentUser, setCurrentUser] = useState(null);
  const [posts, setPosts] = useState([]);
  const [activeCommentPostId, setActiveCommentPostId] = useState(null);
  const [commentsByPost, setCommentsByPost] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [isEditMode, setIsEditMode] = useState(false);
  const [likedPosts, setLikedPosts] = useState(new Set());
  const [activePostMenuId, setActivePostMenuId] = useState(null);
  const [editFormData, setEditFormData] = useState({
    fullName: '',
    bio: ''
  });
  const [selectedFile, setSelectedFile] = useState(null);
  const [filePreview, setFilePreview] = useState(null);
  const [isSaving, setIsSaving] = useState(false);
  const navigate = useNavigate();

  const loadPosts = async (userId) => {
    try {
      console.log('loadPosts called with userId:', userId);
      if (!userId) {
        console.warn('No userId provided to loadPosts');
        setPosts([]);
        return;
      }
      
      const postsData = await getUserPosts(userId);
      console.log('Posts data received for userId:', userId, postsData);
      
      setPosts(postsData || []);
      
      // Initialize liked posts from response data
      const userDataJson = localStorage.getItem('user');
      if (userDataJson) {
        const userData = JSON.parse(userDataJson);
        const userLikedPostIds = new Set();
        (postsData || []).forEach((post) => {
          if (post.LikedByUserIds?.includes(userData.Id)) {
            userLikedPostIds.add(post.Id);
          }
          if (post.SharedPost?.LikedByUserIds?.includes(userData.Id)) {
            userLikedPostIds.add(post.SharedPost.Id);
          }
        });
        setLikedPosts(new Set(userLikedPostIds));
      }
    } catch (postsErr) {
      console.error('Error loading posts:', postsErr);
      setError(`Failed to load posts: ${postsErr.message}`);
    }
  };

  useEffect(() => {
    const loadData = async () => {
      try {
        const userDataJson = localStorage.getItem('user');
        console.log('ProfilePage - userDataJson from localStorage:', userDataJson);
        
        if (!userDataJson) {
          console.error('No user data found in localStorage');
          setCurrentUser(null);
          setError('Không thể tải thông tin người dùng');
          setLoading(false);
          return;
        }

        const userData = JSON.parse(userDataJson);
        console.log('ProfilePage - userData parsed:', userData);
        console.log('ProfilePage - userData.Id:', userData.Id);
        console.log('ProfilePage - userData.id:', userData.id);
        
        // Fetch full user data from backend
        try {
          const userId = userData.Id || userData.id;
          if (!userId) {
            throw new Error('User ID not found in localStorage');
          }
          
          const fullUserData = await getUser(userId);
          setCurrentUser(fullUserData);
          setEditFormData({
            fullName: fullUserData.FullName || '',
            bio: fullUserData.Bio || ''
          });
        } catch (userErr) {
          console.error('Error loading user from backend:', userErr);
          // Fall back to localStorage data if backend fails
          setCurrentUser(userData);
          setEditFormData({
            fullName: userData.FullName || userData.fullName || '',
            bio: userData.Bio || userData.bio || ''
          });
        }

        // Load user's posts
        const userIdToUse = userData.Id || userData.id;
        console.log('ProfilePage - loading posts for userId:', userIdToUse);
        
        if (!userIdToUse) {
          console.warn('ProfilePage - userId is empty, cannot load posts');
          setError('Cannot determine user ID to load posts');
          setLoading(false);
          return;
        }
        
        await loadPosts(userIdToUse);
      } catch (err) {
        console.error('Error loading profile:', err);
        setError(`Error loading profile: ${err.message}`);
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, []);

  useEffect(() => {
    if (!activeCommentPostId) {
      return;
    }

    const loadComments = async () => {
      try {
        const comments = await getCommentsByPost(activeCommentPostId);
        setCommentsByPost((prev) => ({
          ...prev,
          [activeCommentPostId]: comments
        }));
      } catch (err) {
        console.error('Error loading comments for post:', activeCommentPostId, err);
      }
    };

    loadComments();
  }, [activeCommentPostId]);

  // Listen for user updates from other tabs/windows to reload posts with updated user name
  useEffect(() => {
    const handleUserUpdate = async () => {
      console.log('User updated in ProfilePage - reloading posts with new user name');
      const userDataJson = localStorage.getItem('user');
      if (userDataJson) {
        const userData = JSON.parse(userDataJson);
        await loadPosts(userData.Id || userData.id);
      }
    };

    const handlePostShared = async () => {
      console.log('Post shared - reloading ProfilePage posts');
      const userDataJson = localStorage.getItem('user');
      if (userDataJson) {
        const userData = JSON.parse(userDataJson);
        await loadPosts(userData.Id || userData.id);
      }
    };

    window.addEventListener('userUpdated', handleUserUpdate);
    window.addEventListener('postShared', handlePostShared);
    return () => {
      window.removeEventListener('userUpdated', handleUserUpdate);
      window.removeEventListener('postShared', handlePostShared);
    };
  }, []);

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.dispatchEvent(new Event('tokenUpdated'));
    navigate('/login');
  };

  const handleEditClick = () => {
    setIsEditMode(true);
  };

  const handleCancelEdit = () => {
    setIsEditMode(false);
    setEditFormData({
      fullName: currentUser?.FullName || currentUser?.fullName || '',
      bio: currentUser?.Bio || currentUser?.bio || ''
    });
    setSelectedFile(null);
    setFilePreview(null);
  };

  const handleEditInputChange = (e) => {
    const { name, value } = e.target;
    setEditFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleFileChange = (e) => {
    const file = e.target.files?.[0];
    if (file) {
      // Validate file size
      const maxSize = 5 * 1024 * 1024; // 5MB
      if (file.size > maxSize) {
        setError('File size must not exceed 5MB');
        return;
      }

      // Validate file type
      const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
      if (!allowedTypes.includes(file.type)) {
        setError('Only image files (JPEG, PNG, GIF, WebP) are allowed');
        return;
      }

      setSelectedFile(file);
      
      // Create preview
      const reader = new FileReader();
      reader.onloadend = () => {
        setFilePreview(reader.result);
      };
      reader.readAsDataURL(file);
      setError('');
    }
  };

  const handleSaveProfile = async () => {
    if (!currentUser) return;
    
    setIsSaving(true);
    try {
      const userId = currentUser.Id || currentUser.id;
      let updatedProfilePictureUrl = currentUser.ProfilePictureUrl || currentUser.profilePictureUrl || '';
      
      // First, upload profile picture if selected
      if (selectedFile) {
        try {
          // Compress avatar before upload to keep base64 payload small
          const compressedBase64 = await resizeImage(selectedFile, 256, 0.85);
          const blob = await (await fetch(compressedBase64)).blob();
          const compressedFile = new File([blob], 'avatar.jpg', { type: 'image/jpeg' });
          const uploadedUser = await uploadProfilePicture(userId, compressedFile);
          updatedProfilePictureUrl = uploadedUser.ProfilePictureUrl;
          console.log('Profile picture uploaded:', uploadedUser);
        } catch (uploadErr) {
          console.error('Error uploading profile picture:', uploadErr);
          setError(`Failed to upload profile picture: ${uploadErr.message}`);
          setIsSaving(false);
          return;
        }
      }

      // Then, update other user info (fullName, bio)
      await updateUser(userId, {
        fullName: editFormData.fullName,
        bio: editFormData.bio,
        profilePictureUrl: updatedProfilePictureUrl
      });
      
      // Force reload user data from backend to ensure everything is synced
      try {
        const freshUserData = await getUser(userId);
        console.log('Fresh user data from backend:', freshUserData);
        
        const updatedUserState = {
          ...freshUserData,
          FullName: freshUserData.FullName || editFormData.fullName,
          fullName: freshUserData.FullName || editFormData.fullName,
          Bio: freshUserData.Bio || editFormData.bio,
          bio: freshUserData.Bio || editFormData.bio
        };
        
        setCurrentUser(updatedUserState);
        
        // Update localStorage with error handling
        try {
          localStorage.setItem('user', JSON.stringify(updatedUserState));
          // Dispatch event to notify Header and other components about user update
          window.dispatchEvent(new Event('userUpdated'));
        } catch (storageErr) {
          console.warn('localStorage save error (size limit?):', storageErr);
          // Even if localStorage fails, state is updated, so component will still display
        }
      } catch (reloadErr) {
        console.error('Error reloading user data:', reloadErr);
        // Fallback: use the local updated state
        const updatedUserState = {
          ...currentUser,
          FullName: editFormData.fullName,
          fullName: editFormData.fullName,
          Bio: editFormData.bio,
          bio: editFormData.bio,
          ProfilePictureUrl: updatedProfilePictureUrl,
          profilePictureUrl: updatedProfilePictureUrl
        };
        
        setCurrentUser(updatedUserState);
        
        try {
          localStorage.setItem('user', JSON.stringify(updatedUserState));
          // Dispatch event to notify Header and other components about user update
          window.dispatchEvent(new Event('userUpdated'));
        } catch (storageErr) {
          console.warn('localStorage save error (size limit?):', storageErr);
        }
      }
      
      setIsEditMode(false);
      setSelectedFile(null);
      setFilePreview(null);
      setError('');
      
      // Reload posts to reflect the new user name
      await loadPosts(userId);
    } catch (err) {
      console.error('Error saving profile:', err);
      setError(`Failed to save profile: ${err.message}`);
    } finally {
      setIsSaving(false);
    }
  };

  const handleLike = async (post) => {
    if (!currentUser) {
      console.warn('No current user');
      return;
    }
    
    try {
      const userId = currentUser.Id || currentUser.id;
      const isLiked = likedPosts.has(post.Id);
      console.log('Like status:', { postId: post.Id, userId, isLiked });
      
      if (isLiked) {
        // Unlike
        await unlikePost(post.Id, userId);
        setLikedPosts(prev => {
          const newSet = new Set(prev);
          newSet.delete(post.Id);
          return newSet;
        });
      } else {
        // Like
        await likePost(post.Id, userId);
        setLikedPosts(prev => new Set(prev).add(post.Id));
      }
      
      // Fetch updated post to get new like count
      const updatedPost = await getPostById(post.Id);
      if (updatedPost) {
        // Update this post AND any posts that share it
        setPosts(prev => 
          prev.map(p => {
            // If this is the liked/unliked post
            if (p.Id === post.Id) {
              return updatedPost;
            }
            // If this post shares the liked/unliked post, update the shared post data
            if (p.SharedPost?.Id === post.Id) {
              return {
                ...p,
                SharedPost: updatedPost
              };
            }
            return p;
          })
        );
      }
      console.log('Post like count updated');
    } catch (err) {
      console.error('Error liking post:', err);
    }
  };

  const handleToggleComments = (post) => {
    setActiveCommentPostId((current) => (current === post.Id ? null : post.Id));
  };

  const handleToggleSharedComments = (sharedPost) => {
    if (!sharedPost) return;
    setActiveCommentPostId((current) => (current === sharedPost.Id ? null : sharedPost.Id));
  };

  const handleAddComment = async (postId, content) => {
    try {
      const createdComment = await createComment(postId, content);
      setCommentsByPost((prev) => ({
        ...prev,
        [postId]: mergeCommentIntoList(prev[postId] || [], createdComment, { prepend: true })
      }));
      
      // Fetch updated post to sync shared post data
      const updatedPost = await getPostById(postId);
      if (updatedPost) {
        setPosts((prev) => 
          prev.map((post) => {
            if (post.Id === postId) {
              return updatedPost;
            }
            // If this post has a shared post that matches postId, update it too
            if (post.SharedPost?.Id === postId) {
              return {
                ...post,
                SharedPost: updatedPost
              };
            }
            return post;
          })
        );
      }
    } catch (err) {
      console.error('Error creating comment:', err);
    }
  };

  const handleDeleteComment = async (postId, commentId) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa bình luận này?')) {
      return;
    }

    try {
      await deleteComment(commentId);
      setCommentsByPost((prev) => ({
        ...prev,
        [postId]: (prev[postId] || []).filter(comment => comment.id !== commentId)
      }));
      
      // Fetch updated post to sync shared post data
      const updatedPost = await getPostById(postId);
      if (updatedPost) {
        setPosts((prev) => 
          prev.map((post) => {
            if (post.Id === postId) {
              return updatedPost;
            }
            // If this post has a shared post that matches postId, update it too
            if (post.SharedPost?.Id === postId) {
              return {
                ...post,
                SharedPost: updatedPost
              };
            }
            return post;
          })
        );
      }
    } catch (err) {
      console.error('Error deleting comment:', err);
    }
  };

  const handleEditComment = async (postId, commentId, newContent) => {
    try {
      await updateComment(commentId, newContent);
      setCommentsByPost((prev) => ({
        ...prev,
        [postId]: (prev[postId] || []).map(comment =>
          comment.id === commentId
            ? { ...comment, content: newContent }
            : comment
        )
      }));
    } catch (err) {
      console.error('Error updating comment:', err);
    }
  };

  const handleDeletePost = async (post) => {
    if (post.UserId !== currentUser?.Id) {
      setError('Bạn chỉ có thể xóa bài viết của chính mình');
      return;
    }

    if (!window.confirm('Bạn có chắc chắn muốn xóa bài viết này?')) {
      return;
    }

    try {
      await deletePost(post.Id);
      setPosts(posts.filter(p => p.Id !== post.Id));
      setActivePostMenuId(null);
      setError('');
    } catch (err) {
      console.error('Error deleting post:', err);
      setError(`Lỗi xóa bài viết: ${err.message}`);
    }
  };

  const handleHashtagClick = (hashtag) => {
    const slug = hashtag.startsWith('#') ? hashtag.slice(1) : hashtag;
    navigate(`/home?hashtag=${encodeURIComponent(slug)}`);
  };

  if (loading) {
    return <div className="profile-wrapper"><p style={{padding: '20px', textAlign: 'center'}}>Đang tải...</p></div>;
  }

  if (!currentUser) {
    return (
      <div className="profile-wrapper">
        <Header onLogout={handleLogout} />
        <div className="profile-container">
          <div style={{background: 'white', padding: '40px', borderRadius: '12px', textAlign: 'center', color: '#d32f2f'}}>
            <p style={{margin: '0 0 20px 0', fontSize: '16px'}}>Không thể tải thông tin người dùng. Vui lòng đăng nhập lại.</p>
            <button onClick={() => {
              localStorage.removeItem('token');
              localStorage.removeItem('user');
              window.dispatchEvent(new Event('tokenUpdated'));
              navigate('/login');
            }} style={{padding: '10px 20px', backgroundColor: '#d32f2f', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer', fontWeight: '600'}}>
              Đăng nhập lại
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="profile-wrapper">
      <Header onLogout={handleLogout} />
      <div className="profile-container">
        {/* Profile Header */}
        <div className="profile-header">
          <div className="profile-cover"></div>
          
          <div className="profile-info-section">
            <div className="profile-avatar-large">
              {currentUser?.ProfilePictureUrl || currentUser?.profilePictureUrl ? (
                <img src={currentUser.ProfilePictureUrl || currentUser.profilePictureUrl} alt="Avatar" style={{width: '100%', height: '100%', objectFit: 'cover'}} />
              ) : (
                <i className="fa-solid fa-user"></i>
              )}
            </div>
            
            <div className="profile-user-info">
              <h1 className="profile-username">{currentUser?.FullName || currentUser?.fullName || currentUser?.UserName || currentUser?.userName}</h1>
              
              <div className="profile-bio">
                {currentUser?.Bio || currentUser?.bio ? (
                  <p>{currentUser.Bio || currentUser.bio}</p>
                ) : (
                  <p style={{color: '#999'}}>Chưa có tiểu sử</p>
                )}
              </div>

              <button className="profile-edit-btn" onClick={handleEditClick}>
                <span><i className="fa-solid fa-pen-nib"></i></span> Chỉnh sửa
              </button>
            </div>
          </div>


        </div>

        {/* Edit Profile Modal */}
        {isEditMode && (
          <div className="modal-overlay" onClick={handleCancelEdit}>
            <div className="modal-content" onClick={(e) => e.stopPropagation()}>
              <div className="modal-header">
                <h2>Chỉnh sửa Hồ sơ</h2>
                <button className="modal-close" onClick={handleCancelEdit}>✕</button>
              </div>

              <div className="modal-body">
                <div className="form-group">
                  <label htmlFor="fullName" className="form-label">Họ và Tên:</label>
                  <input
                    id="fullName"
                    type="text"
                    name="fullName"
                    value={editFormData.fullName}
                    onChange={handleEditInputChange}
                    placeholder="Nhập họ và tên"
                    className="form-input"
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="bio" className="form-label">Tiểu sử:</label>
                  <textarea
                    id="bio"
                    name="bio"
                    value={editFormData.bio}
                    onChange={handleEditInputChange}
                    placeholder="Nói giới thiệu về bản thân"
                    className="form-input"
                    rows="4"
                  />
                </div>

                <div className="form-group">
                  <label htmlFor="profilePicture" className="form-label">Ảnh Đại Diện:</label>
                  <input
                    id="profilePicture"
                    type="file"
                    accept="image/*"
                    onChange={handleFileChange}
                    className="form-input-file"
                  />
                  <p style={{fontSize: '12px', color: '#999', marginTop: '8px'}}>
                    Chọn ảnh JPEG, PNG, GIF hoặc WebP (tối đa 5MB)
                  </p>
                </div>

                {filePreview && (
                  <div className="form-group">
                    <label>Xem trước ảnh:</label>
                    <img 
                      src={filePreview} 
                      alt="Preview" 
                      style={{maxWidth: '120px', maxHeight: '120px', borderRadius: '50%', objectFit: 'cover', border: '2px solid #6b4fc7'}}
                    />
                  </div>
                )}

                {error && <div className="error-message" style={{marginTop: '10px'}}>{error}</div>}
              </div>

              <div className="modal-footer">
                <button 
                  className="btn btn-cancel" 
                  onClick={handleCancelEdit}
                  disabled={isSaving}
                >
                  Hủy
                </button>
                <button 
                  className="btn btn-primary" 
                  onClick={handleSaveProfile}
                  disabled={isSaving}
                >
                  {isSaving ? 'Đang lưu...' : 'Lưu'}
                </button>
              </div>
            </div>
          </div>
        )}

        {/* Profile Posts */}
        <main className="profile-main-content">
          <div className="profile-posts">
            {posts.length === 0 ? (
              <p className="no-posts">Chưa có bài viết nào</p>
            ) : (
              posts.map((post) => (
                <div key={post.Id} className="profile-post-card">
                  <div className="post-header-profile">
                    <div className="post-user-info-profile">
                      <div className="post-avatar-profile">
                        {post.UserProfilePictureUrl ? (
                          <img 
                            src={post.UserProfilePictureUrl} 
                            alt="User Avatar"
                            style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                          />
                        ) : (
                          <i className="fa-solid fa-user"></i>
                        )}
                      </div>
                      <div className="post-user-details-profile">
                        <p className="post-username-profile">{post.UserFullName || post.UserName || 'Người dùng'}</p>
                        <p className="post-time-profile">
                          {new Date(post.CreatedAt).toLocaleDateString('vi-VN')}
                        </p>
                      </div>
                    </div>
                    <div className="post-menu-container">
                      <button 
                        className="post-menu-btn"
                        onClick={() => setActivePostMenuId(activePostMenuId === post.Id ? null : post.Id)}
                      >
                        ⋯
                      </button>
                      {activePostMenuId === post.Id && currentUser?.Id === post.UserId && (
                        <div className="post-menu-dropdown">
                          <button 
                            className="menu-item"
                            onClick={() => handleDeletePost(post)}
                          >
                            <i className="fa-solid fa-trash"></i> Xóa bài viết
                          </button>
                        </div>
                      )}
                    </div>
                  </div>

                  <div className="post-content-profile">
                    <p>
                      <HashtagContent 
                        content={post.Content} 
                        onHashtagClick={handleHashtagClick}
                      />
                    </p>
                  </div>

                  {post.ImageUrl && (
                    <div className="post-images-profile">
                      <img src={post.ImageUrl} alt="Post" className="post-image-profile" />
                    </div>
                  )}

                  {/* Shared Post Display */}
                  {post.IsShared && post.SharedPost && (
                    <div className="shared-post-container" style={{
                      marginTop: '12px',
                      padding: '12px',
                      backgroundColor: '#f0f2f5',
                      borderRadius: '8px',
                      border: '1px solid #e5e7eb'
                    }}>
                      <div style={{
                        display: 'flex',
                        alignItems: 'center',
                        gap: '8px',
                        marginBottom: '10px',
                        color: '#65676b',
                        fontSize: '13px'
                      }}>
                        <i className="fa-solid fa-share"></i>
                        <span>{post.UserFullName || post.UserName} đã chia sẻ</span>
                      </div>
                      
                      <div
                        className="original-post"
                        style={{
                          backgroundColor: '#fff',
                          padding: '10px',
                          borderRadius: '6px',
                          border: '1px solid #ddd',
                          cursor: 'pointer'
                        }}
                        onClick={() => navigate('/post/' + post.SharedPost.Id)}
                      >
                        <div style={{
                          display: 'flex',
                          alignItems: 'center',
                          gap: '8px',
                          marginBottom: '8px'
                        }}>
                          <div style={{
                            width: '32px',
                            height: '32px',
                            borderRadius: '50%',
                            overflow: 'hidden',
                            backgroundColor: '#e5e7eb'
                          }}>
                            {post.SharedPost.UserProfilePictureUrl ? (
                              <img 
                                src={post.SharedPost.UserProfilePictureUrl} 
                                alt="Author"
                                style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                              />
                            ) : (
                              <div style={{
                                width: '100%',
                                height: '100%',
                                display: 'flex',
                                alignItems: 'center',
                                justifyContent: 'center'
                              }}>
                                <i className="fa-solid fa-user"></i>
                              </div>
                            )}
                          </div>
                          <div>
                            <p style={{
                              margin: '0',
                              fontWeight: '600',
                              fontSize: '14px'
                            }}>
                              {post.SharedPost.UserFullName || post.SharedPost.UserName}
                            </p>
                            <p style={{
                              margin: '0',
                              fontSize: '12px',
                              color: '#65676b'
                            }}>
                              {new Date(post.SharedPost.CreatedAt).toLocaleDateString('vi-VN')}
                            </p>
                          </div>
                        </div>

                        <p style={{
                          margin: '8px 0',
                          fontSize: '14px',
                          color: '#050505'
                        }}>
                          <HashtagContent 
                            content={post.SharedPost.Content} 
                            onHashtagClick={handleHashtagClick}
                          />
                        </p>

                        {post.SharedPost.ImageUrl && (
                          <img 
                            src={post.SharedPost.ImageUrl} 
                            alt="Shared Post" 
                            style={{
                              marginTop: '8px',
                              maxWidth: '100%',
                              borderRadius: '6px',
                              maxHeight: '300px',
                              objectFit: 'cover'
                            }}
                          />
                        )}

                        <div style={{
                          display: 'flex',
                          gap: '10px',
                          marginTop: '8px'
                        }}>
                          <button
                            type="button"
                            className={`post-action-btn shared-post-action ${likedPosts.has(post.SharedPost.Id) ? 'liked' : ''}`}
                            onClick={(e) => {
                              e.stopPropagation();
                              handleLike(post.SharedPost);
                            }}
                          >
                            <span>{likedPosts.has(post.SharedPost.Id) ? '❤️' : '🤍'}</span>
                            {likedPosts.has(post.SharedPost.Id) ? 'Bỏ thích bài gốc' : 'Thích bài gốc'}
                          </button>
                          <button
                            type="button"
                            className="post-action-btn shared-post-action"
                            onClick={(e) => {
                              e.stopPropagation();
                              handleToggleSharedComments(post.SharedPost);
                            }}
                          >
                            <span><i className="fa-solid fa-comments"></i></span> Bình luận bài gốc
                          </button>
                        </div>

                        <div style={{
                          display: 'flex',
                          gap: '16px',
                          marginTop: '8px',
                          paddingTop: '8px',
                          borderTop: '1px solid #e5e7eb',
                          fontSize: '12px',
                          color: '#65676b'
                        }}>
                          <span>❤️ {post.SharedPost.LikesCount} lượt thích</span>
                          <span><i className="fa-solid fa-comments"></i> {post.SharedPost.CommentsCount} bình luận</span>
                        </div>

                        {activeCommentPostId === post.SharedPost.Id && (
                          <div style={{ marginTop: '12px' }}>
                            <CommentSection
                              post={post.SharedPost}
                              comments={commentsByPost[post.SharedPost.Id] || []}
                              onClose={() => setActiveCommentPostId(null)}
                              onAddComment={handleAddComment}
                              onDeleteComment={handleDeleteComment}
                              onEditComment={handleEditComment}
                              currentUser={currentUser}
                            />
                          </div>
                        )}
                      </div>
                    </div>
                  )}

                  <div className="post-stats-profile">
                    <span>❤️ {post.LikesCount}</span>
                    <span><i className="fa-solid fa-comments"></i> {post.CommentsCount ?? (commentsByPost[post.Id]?.length ?? 0)} Bình luận</span>
                  </div>

                  <div className="post-actions-profile">
                    <button 
                      className={`post-action-btn-profile ${likedPosts.has(post.Id) ? 'liked' : ''}`}
                      onClick={() => handleLike(post)}
                    >
                      <span>{likedPosts.has(post.Id) ? '❤️' : '🤍'}</span> 
                      {likedPosts.has(post.Id) ? 'Bỏ thích' : 'Thích'}
                    </button>
                    <button className="post-action-btn-profile" onClick={() => handleToggleComments(post)}>
                      <span><i className="fa-solid fa-comments"></i></span> Bình luận
                    </button>
                  </div>
                  {activeCommentPostId === post.Id && (
                    <CommentSection
                      post={post}
                      comments={commentsByPost[post.Id] || []}
                      onClose={() => setActiveCommentPostId(null)}
                      onAddComment={handleAddComment}
                      onDeleteComment={handleDeleteComment}
                      onEditComment={handleEditComment}
                      currentUser={currentUser}
                    />
                  )}
                </div>
              ))
            )}
          </div>
        </main>
      </div>
    </div>
  );
}
