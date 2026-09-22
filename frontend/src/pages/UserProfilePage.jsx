import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { getUserPosts, getUser, likePost, unlikePost, sendFriendRequest, deletePost, getCommentsByPost, createComment, updateComment, deleteComment, getPostById, removeFriend, checkFriendshipStatus } from '../api';
import Header from '../components/Header';
import CommentSection from '../components/CommentSection';
import HashtagContent from '../components/HashtagContent';
import '../styles/ProfilePage.css';
import { mergeCommentIntoList } from '../utils/commentNormalize';
import { normalizeFeedPostFromRealtime } from '../utils/postRealtime';

export default function UserProfilePage() {
  const { userId } = useParams();
  const navigate = useNavigate();
  
  const [user, setUser] = useState(null);
  const [posts, setPosts] = useState([]);
  const [currentUser, setCurrentUser] = useState(null);
  const [activeCommentPostId, setActiveCommentPostId] = useState(null);
  const [commentsByPost, setCommentsByPost] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [likedPosts, setLikedPosts] = useState(new Set());
  const [activePostMenuId, setActivePostMenuId] = useState(null);
  const [friendRequestSent, setFriendRequestSent] = useState(false);
  const [isAddingFriend, setIsAddingFriend] = useState(false);
  const [isFriend, setIsFriend] = useState(false);
  const [showUnfriendConfirm, setShowUnfriendConfirm] = useState(false);
  const [isUnfriending, setIsUnfriending] = useState(false);

  const loadUserData = async () => {
    try {
      if (!userId) {
        setError('Invalid user ID');
        setLoading(false);
        return;
      }

      const userData = await getUser(userId);
      setUser(userData);

      // Load user's posts
      const postsData = await getUserPosts(userId);
      setPosts(postsData || []);

      // Get current user for like tracking
      const currentUserJson = localStorage.getItem('user');
      if (currentUserJson) {
        const currentUserData = JSON.parse(currentUserJson);
        setCurrentUser(currentUserData);
        
        // Track liked posts
        const userLikedPostIds = (postsData || [])
          .filter(post => post.LikedByUserIds && post.LikedByUserIds.includes(currentUserData.Id))
          .map(post => post.Id);
        setLikedPosts(new Set(userLikedPostIds));
        
        const statusData = await checkFriendshipStatus(userId);
        const relationshipStatus = String(statusData?.Status || statusData?.status || 'None');
        const isFriendAlready = relationshipStatus.toLowerCase() === 'accepted';
        const isPending = relationshipStatus.toLowerCase() === 'pending';
        setIsFriend(isFriendAlready);
        setFriendRequestSent(isPending);
      }
    } catch (err) {
      console.error('Error loading user data:', err);
      setError(`Error loading user: ${err.message}`);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadUserData();
  }, [userId]);

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

  useEffect(() => {
    if (!userId) {
      return;
    }

    // 🔔 Listener for realtime post created by this user
    const handlePostCreated = (event) => {
      const payload = event.detail || {};
      const postUserId = payload.userId ?? payload.UserId;
      
      // Only add if post is by this profile user
      if (postUserId !== userId) return;

      const normalized = normalizeFeedPostFromRealtime(payload);
      console.log(`[UserProfilePage] 📝 Post created by user ${userId}:`, normalized.Id);
      setPosts((prev) => {
        const exists = prev.some((p) => p.id === normalized.Id || p.Id === normalized.Id);
        if (exists) return prev;
        return [{ ...normalized, likedBy: normalized.likedBy || [] }, ...prev];
      });
    };

    // 🔔 Listener for realtime post deleted by this user
    const handlePostDeleted = (event) => {
      const payload = event.detail || {};
      const postUserId = payload.userId ?? payload.UserId;
      
      // Only process if post was by this profile user
      if (postUserId !== userId) return;

      const postId = payload.postId ?? payload.PostId;
      console.log(`[UserProfilePage] 🗑️ Post deleted from user ${userId}:`, postId);
      setPosts((prev) => prev.filter((p) => p.id !== postId && p.Id !== postId));
      setCommentsByPost((prev) => {
        if (!(postId in prev)) return prev;
        const next = { ...prev };
        delete next[postId];
        return next;
      });
    };

    // 🔔 Listener for post liked
    const handlePostLiked = (event) => {
      const payload = event.detail || {};
      const postId = payload.postId ?? payload.PostId;
      const actorUserId = payload.userId ?? payload.UserId;
      
      if (!postId || !actorUserId) return;

      setPosts((prev) => prev.map((post) => {
        if (post.id === postId || post.Id === postId) {
          return {
            ...post,
            likesCount: payload.likesCount ?? payload.LikesCount ?? (post.likesCount || post.LikesCount || 0),
            LikedByUserIds: Array.isArray(payload.LikedByUserIds) ? payload.LikedByUserIds : (post.LikedByUserIds || [])
          };
        }
        return post;
      }));
    };

    // 🔔 Listener for post unliked
    const handlePostUnliked = (event) => {
      const payload = event.detail || {};
      const postId = payload.postId ?? payload.PostId;
      const actorUserId = payload.userId ?? payload.UserId;
      
      if (!postId || !actorUserId) return;

      setPosts((prev) => prev.map((post) => {
        if (post.id === postId || post.Id === postId) {
          return {
            ...post,
            likesCount: payload.likesCount ?? payload.LikesCount ?? (post.likesCount || post.LikesCount || 0),
            LikedByUserIds: Array.isArray(payload.LikedByUserIds) ? payload.LikedByUserIds : (post.LikedByUserIds || [])
          };
        }
        return post;
      }));
    };

    window.addEventListener('signalr:post-created', handlePostCreated);
    window.addEventListener('signalr:post-deleted', handlePostDeleted);
    window.addEventListener('signalr:post-liked', handlePostLiked);
    window.addEventListener('signalr:post-unliked', handlePostUnliked);

    return () => {
      window.removeEventListener('signalr:post-created', handlePostCreated);
      window.removeEventListener('signalr:post-deleted', handlePostDeleted);
      window.removeEventListener('signalr:post-liked', handlePostLiked);
      window.removeEventListener('signalr:post-unliked', handlePostUnliked);
    };
  }, [userId]);

  const handleAddFriend = async () => {
    if (!currentUser || !user || isFriend) return;

    setIsAddingFriend(true);
    try {
      await sendFriendRequest(user.Id || userId);
      setFriendRequestSent(true);
      setError('');
      
      // Emit event to notify HomePage about new friend request
      window.dispatchEvent(new Event('friendRequestSent'));
      
    } catch (err) {
      console.error('Error sending friend request:', err);
      setError(`Lỗi gửi lời mời: ${err.message}`);
    } finally {
      setIsAddingFriend(false);
    }
  };

  const handleUnfriend = async () => {
    if (!currentUser || !user) return;

    setIsUnfriending(true);
    try {
      await removeFriend(user.Id || userId);
      setIsFriend(false);
      setShowUnfriendConfirm(false);
      setError('');
      
      // Emit event to notify HomePage about friend removal
      window.dispatchEvent(new Event('friendRemoved'));
      
      // Show success notification
      setTimeout(() => {
        setError('');
      }, 3000);
    } catch (err) {
      console.error('Error unfriending:', err);
      setError(`Lỗi hủy kết bạn: ${err.message}`);
    } finally {
      setIsUnfriending(false);
    }
  };

  const handleLike = async (post) => {
    if (!currentUser) return;

    try {
      const currentUserId = currentUser.Id || currentUser.id;
      const isLiked = likedPosts.has(post.Id);

      if (isLiked) {
        await unlikePost(post.Id, currentUserId);
        setLikedPosts(prev => {
          const newSet = new Set(prev);
          newSet.delete(post.Id);
          return newSet;
        });
      } else {
        await likePost(post.Id, currentUserId);
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

  const handleAddComment = async (postId, content) => {
    try {
      const createdComment = await createComment(postId, content);
      setCommentsByPost((prev) => ({
        ...prev,
        [postId]: mergeCommentIntoList(prev[postId] || [], createdComment, { prepend: true })
      }));
      setPosts((prev) => prev.map((post) =>
        post.Id === postId
          ? { ...post, commentsCount: (post.commentsCount ?? 0) + 1 }
          : post
      ));
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
      setPosts((prev) => prev.map((post) =>
        post.Id === postId
          ? { ...post, commentsCount: Math.max(0, (post.commentsCount ?? 1) - 1) }
          : post
      ));
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

  const handleHashtagClick = (hashtag) => {
    const slug = hashtag.startsWith('#') ? hashtag.slice(1) : hashtag;
    navigate(`/home?hashtag=${encodeURIComponent(slug)}`);
  };

  if (loading) {
    return (
      <div className="profile-wrapper">
        <Header />
        <div className="profile-container">
          <p style={{ padding: '20px', textAlign: 'center' }}>Đang tải...</p>
        </div>
      </div>
    );
  }

  if (!user) {
    return (
      <div className="profile-wrapper">
        <Header />
        <div className="profile-container">
          <div style={{ background: 'white', padding: '40px', borderRadius: '12px', textAlign: 'center', color: '#d32f2f' }}>
            <p style={{ margin: '0 0 20px 0', fontSize: '16px' }}>Không tìm thấy người dùng này.</p>
            <button 
              onClick={() => navigate('/home')}
              style={{ padding: '10px 20px', backgroundColor: '#6b4fc7', color: 'white', border: 'none', borderRadius: '4px', cursor: 'pointer', fontWeight: '600' }}
            >
              Quay lại
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="profile-wrapper">
      <Header />
      <div className="profile-container">
        {/* Profile Header */}
        <div className="profile-header">
          <div className="profile-cover"></div>

          <div className="profile-info-section">
            <div className="profile-avatar-large">
              {user?.ProfilePictureUrl || user?.profilePictureUrl ? (
                <img src={user.ProfilePictureUrl || user.profilePictureUrl} alt="Avatar" style={{ width: '100%', height: '100%', objectFit: 'cover' }} />
              ) : (
                <i className="fa-solid fa-user"></i>
              )}
            </div>

            <div className="profile-user-info">
              <h1 className="profile-username">{user?.FullName || user?.fullName || user?.UserName || user?.userName}</h1>

              <div className="profile-bio">
                {user?.Bio || user?.bio ? (
                  <p>{user.Bio || user.bio}</p>
                ) : (
                  <p style={{ color: '#999' }}>Chưa có tiểu sử</p>
                )}
              </div>

              {isFriend ? (
                <div style={{ display: 'flex', gap: '10px', alignItems: 'center' }}>
                  <div style={{ padding: '10px 20px', background: '#6b4fc7', color: 'white', borderRadius: '6px', fontWeight: '600', textAlign: 'center', flex: 1 }}>
                    ✓ Bạn bè
                  </div>
                  <button
                    onClick={() => setShowUnfriendConfirm(true)}
                    style={{ padding: '10px 20px', background: '#f44336', color: 'white', borderRadius: '6px', fontWeight: '600', border: 'none', cursor: 'pointer' }}
                    title="Hủy kết bạn"
                  >
                    <i className="fa-solid fa-user-minus"></i>
                  </button>
                </div>
              ) : friendRequestSent ? (
                <div style={{ padding: '10px 20px', background: '#4caf50', color: 'white', borderRadius: '6px', fontWeight: '600', textAlign: 'center' }}>
                  ✓ Đã gửi lời mời kết bạn
                </div>
              ) : (
                <button 
                  className="profile-edit-btn" 
                  onClick={handleAddFriend}
                  disabled={isAddingFriend}
                  style={{ background: 'white', color: '#6b4fc7' }}
                >
                  <span><i className="fa-solid fa-user-plus"></i></span> Kết bạn
                </button>
              )}
            </div>
          </div>
        </div>

        {/* User Posts */}
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

        {/* Unfriend Confirmation Dialog */}
        {showUnfriendConfirm && (
          <div style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.5)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000
          }}>
            <div style={{
              background: 'white',
              padding: '30px',
              borderRadius: '12px',
              boxShadow: '0 4px 6px rgba(0, 0, 0, 0.1)',
              maxWidth: '400px',
              textAlign: 'center'
            }}>
              <h3 style={{ margin: '0 0 15px 0', color: '#333', fontSize: '18px' }}>
                Hủy kết bạn?
              </h3>
              <p style={{ margin: '0 0 20px 0', color: '#666', fontSize: '14px' }}>
                Bạn có chắc chắn muốn hủy kết bạn với <strong>{user?.FullName || user?.fullName || user?.UserName || user?.userName}</strong> không?
              </p>
              <div style={{ display: 'flex', gap: '10px', justifyContent: 'center' }}>
                <button
                  onClick={() => setShowUnfriendConfirm(false)}
                  disabled={isUnfriending}
                  style={{
                    padding: '10px 20px',
                    backgroundColor: '#ccc',
                    color: '#333',
                    border: 'none',
                    borderRadius: '6px',
                    cursor: 'pointer',
                    fontWeight: '600',
                    fontSize: '14px'
                  }}
                >
                  Hủy bỏ
                </button>
                <button
                  onClick={handleUnfriend}
                  disabled={isUnfriending}
                  style={{
                    padding: '10px 20px',
                    backgroundColor: '#f44336',
                    color: 'white',
                    border: 'none',
                    borderRadius: '6px',
                    cursor: isUnfriending ? 'not-allowed' : 'pointer',
                    fontWeight: '600',
                    fontSize: '14px',
                    opacity: isUnfriending ? 0.6 : 1
                  }}
                >
                  {isUnfriending ? 'Đang xử lý...' : 'Hủy kết bạn'}
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}
