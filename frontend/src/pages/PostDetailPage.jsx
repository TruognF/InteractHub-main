import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { getPostById, likePost, unlikePost, getCommentsByPost, createComment, updateComment, deleteComment } from '../api';
import Header from '../components/Header';
import CommentSection from '../components/CommentSection';
import HashtagContent from '../components/HashtagContent';
import { startConnection } from '../utils/postHubConnection';
import { mergeCommentIntoList, sameCommentId } from '../utils/commentNormalize';
import '../styles/PostDetailPage.css';

export default function PostDetailPage() {
  const { postId } = useParams();
  const navigate = useNavigate();
  const [post, setPost] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [currentUser, setCurrentUser] = useState(null);
  const [likedPosts, setLikedPosts] = useState(new Set());
  const [activeCommentPostId, setActiveCommentPostId] = useState(null);
  const [commentsByPost, setCommentsByPost] = useState({});

  useEffect(() => {
    const loadData = async () => {
      try {
        const userDataJson = localStorage.getItem('user');
        if (!userDataJson) {
          navigate('/login');
          return;
        }

        const userData = JSON.parse(userDataJson);
        setCurrentUser(userData);

        const postData = await getPostById(postId);
        if (!postData) {
          setError('Bài viết không tồn tại');
          return;
        }

        setPost(postData);

        // Initialize liked posts
        const userLikedPostIds = new Set();
        if (postData.LikedByUserIds?.includes(userData.Id)) {
          userLikedPostIds.add(postData.Id);
        }
        if (postData.SharedPost?.LikedByUserIds?.includes(userData.Id)) {
          userLikedPostIds.add(postData.SharedPost.Id);
        }
        setLikedPosts(userLikedPostIds);

      } catch (err) {
        console.error('Error loading post:', err);
        setError('Không thể tải bài viết');
      } finally {
        setLoading(false);
      }
    };

    loadData();
  }, [postId, navigate]);

  const handleLike = async (targetPost) => {
    if (!currentUser) return;

    try {
      const isLiked = likedPosts.has(targetPost.Id);

      if (isLiked) {
        await unlikePost(targetPost.Id, currentUser.Id);
        setLikedPosts(prev => {
          const newSet = new Set(prev);
          newSet.delete(targetPost.Id);
          return newSet;
        });
      } else {
        await likePost(targetPost.Id, currentUser.Id);
        setLikedPosts(prev => new Set(prev).add(targetPost.Id));
      }

      // Update post data
      const updatedPost = await getPostById(targetPost.Id);
      if (updatedPost) {
        if (targetPost.Id === post.Id) {
          setPost(updatedPost);
        } else if (post.SharedPost?.Id === targetPost.Id) {
          setPost(prev => ({
            ...prev,
            SharedPost: updatedPost
          }));
        }
      }
    } catch (err) {
      console.error('Error liking post:', err);
    }
  };

  const handleToggleComments = (targetPost) => {
    setActiveCommentPostId((current) => (current === targetPost.Id ? null : targetPost.Id));
  };

  useEffect(() => {
    if (!activeCommentPostId) return;

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
    if (!post?.Id) return undefined;

    const mainId = post.Id;
    const sharedNestedId = post.SharedPost?.Id;

    let cancelled = false;
    let conn;

    const caresAbout = (pid) => {
      if (pid == null) return false;
      const key = String(pid);
      return key === String(mainId) || (sharedNestedId != null && key === String(sharedNestedId));
    };

    const applyCount = (pid, count) => {
      if (typeof count !== 'number') return;
      setPost((prev) => {
        if (!prev) return prev;
        if (prev.Id === pid) {
          return { ...prev, CommentsCount: count };
        }
        if (prev.SharedPost?.Id === pid) {
          return {
            ...prev,
            SharedPost: { ...prev.SharedPost, CommentsCount: count }
          };
        }
        return prev;
      });
    };

    const onCommentAdded = (data) => {
      if (!data?.postId || !caresAbout(data.postId)) return;
      setCommentsByPost((prev) => {
        const pid = data.postId;
        const list = prev[pid] || [];
        const next = mergeCommentIntoList(list, data.comment, { prepend: true });
        if (next === list) return prev;
        return { ...prev, [pid]: next };
      });
      applyCount(data.postId, data.commentsCount);
    };

    const onCommentUpdated = (data) => {
      if (!data?.postId || !caresAbout(data.postId)) return;
      const cid = data.commentId;
      const nextContent = data.content ?? '';
      setCommentsByPost((prev) => ({
        ...prev,
        [data.postId]: (prev[data.postId] || []).map((c) =>
          sameCommentId(c.id ?? c.Id, cid) ? { ...c, content: nextContent } : c
        )
      }));
    };

    const onCommentDeleted = (data) => {
      if (!data?.postId || !caresAbout(data.postId)) return;
      const cid = data.commentId;
      setCommentsByPost((prev) => ({
        ...prev,
        [data.postId]: (prev[data.postId] || []).filter((c) => !sameCommentId(c.id ?? c.Id, cid))
      }));
      applyCount(data.postId, data.commentsCount);
    };

    (async () => {
      const connection = await startConnection();
      if (cancelled || !connection) return;
      conn = connection;
      connection.on('CommentAdded', onCommentAdded);
      connection.on('CommentUpdated', onCommentUpdated);
      connection.on('CommentDeleted', onCommentDeleted);
    })();

    return () => {
      cancelled = true;
      if (!conn) return;
      conn.off('CommentAdded', onCommentAdded);
      conn.off('CommentUpdated', onCommentUpdated);
      conn.off('CommentDeleted', onCommentDeleted);
    };
  }, [post]);

  const handleAddComment = async (commentPostId, content) => {
    try {
      const createdComment = await createComment(commentPostId, content);
      setCommentsByPost((prev) => ({
        ...prev,
        [commentPostId]: mergeCommentIntoList(prev[commentPostId] || [], createdComment, {
          prepend: true
        })
      }));

      // Update post comment count
      if (commentPostId === post.Id) {
        setPost(prev => ({
          ...prev,
          CommentsCount: (prev.CommentsCount || 0) + 1
        }));
      } else if (post.SharedPost?.Id === commentPostId) {
        setPost(prev => ({
          ...prev,
          SharedPost: {
            ...prev.SharedPost,
            CommentsCount: (prev.SharedPost.CommentsCount || 0) + 1
          }
        }));
      }
    } catch (err) {
      console.error('Error creating comment:', err);
    }
  };

  const handleDeleteComment = async (commentPostId, commentId) => {
    if (!window.confirm('Bạn có chắc chắn muốn xóa bình luận này?')) return;

    try {
      await deleteComment(commentId);
      setCommentsByPost((prev) => ({
        ...prev,
        [commentPostId]: (prev[commentPostId] || []).filter(
          (c) => !sameCommentId(c.id ?? c.Id, commentId)
        )
      }));

      // Update post comment count
      if (commentPostId === post.Id) {
        setPost(prev => ({
          ...prev,
          CommentsCount: Math.max(0, (prev.CommentsCount || 0) - 1)
        }));
      } else if (post.SharedPost?.Id === commentPostId) {
        setPost(prev => ({
          ...prev,
          SharedPost: {
            ...prev.SharedPost,
            CommentsCount: Math.max(0, (prev.SharedPost.CommentsCount || 0) - 1)
          }
        }));
      }
    } catch (err) {
      console.error('Error deleting comment:', err);
    }
  };

  const handleEditComment = async (commentPostId, commentId, newContent) => {
    try {
      await updateComment(commentId, newContent);
      setCommentsByPost((prev) => ({
        ...prev,
        [commentPostId]: (prev[commentPostId] || []).map((comment) =>
          sameCommentId(comment.id ?? comment.Id, commentId)
            ? { ...comment, content: newContent }
            : comment
        )
      }));
    } catch (err) {
      console.error('Error updating comment:', err);
    }
  };

  const handleHashtagClick = (hashtag) => {
    navigate(`/home?hashtag=${encodeURIComponent(hashtag.slice(1))}`);
  };

  if (loading) {
    return (
      <div className="post-detail-page">
        <Header onLogout={() => navigate('/login')} />
        <div className="loading">Đang tải...</div>
      </div>
    );
  }

  if (error || !post) {
    return (
      <div className="post-detail-page">
        <Header onLogout={() => navigate('/login')} />
        <div className="error">{error || 'Bài viết không tồn tại'}</div>
        <button onClick={() => navigate(-1)} className="back-btn">Quay lại</button>
      </div>
    );
  }

  return (
    <div className="post-detail-page">
      <Header onLogout={() => navigate('/login')} />

      <div className="post-detail-container">
        <button onClick={() => navigate(-1)} className="back-btn">
          ← Quay lại
        </button>

        <div className="post-detail-card">
          <div className="post-header">
            <div className="post-user-info">
              <div className="post-avatar">
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
              <div className="post-user-details">
                <p className="post-username">{post.UserFullName || post.UserName || 'Người dùng'}</p>
                <p className="post-time">
                  {new Date(post.CreatedAt).toLocaleDateString('vi-VN')}
                </p>
              </div>
            </div>
          </div>

          <div className="post-content">
            <p>
              <HashtagContent
                content={post.Content}
                onHashtagClick={handleHashtagClick}
              />
            </p>
            {post.ImageUrl && (
              <img src={post.ImageUrl} alt="Post" className="post-image" />
            )}

            {/* Shared Post Display */}
            {post.IsShared && post.SharedPost && (
              <div className="shared-post-container">
                <div className="shared-post-header">
                  <div className="shared-post-label">
                    <i className="fa-solid fa-share"></i>
                    <span>Bài viết được chia sẻ</span>
                  </div>
                  <span className="shared-post-user">{post.UserFullName || post.UserName} đã chia sẻ</span>
                </div>

                <div
                  className="original-post-card"
                  title="Xem bài viết gốc"
                  onClick={() => navigate('/post/' + post.SharedPost.Id)}
                >
                  <div className="original-post-author">
                    <div className="original-post-avatar">
                      {post.SharedPost.UserProfilePictureUrl ? (
                        <img
                          src={post.SharedPost.UserProfilePictureUrl}
                          alt="Author"
                        />
                      ) : (
                        <div className="original-post-avatar-icon">
                          <i className="fa-solid fa-user"></i>
                        </div>
                      )}
                    </div>
                    <div>
                      <p className="original-post-author-name">
                        {post.SharedPost.UserFullName || post.SharedPost.UserName}
                      </p>
                      <p className="original-post-meta">
                        {new Date(post.SharedPost.CreatedAt).toLocaleDateString('vi-VN')}
                      </p>
                    </div>
                  </div>

                  <p className="original-post-text">
                    <HashtagContent
                      content={post.SharedPost.Content}
                      onHashtagClick={handleHashtagClick}
                    />
                  </p>

                  {post.SharedPost.ImageUrl && (
                    <img
                      src={post.SharedPost.ImageUrl}
                      alt="Shared Post"
                      className="original-post-image"
                    />
                  )}
                </div>

                <div className="shared-post-footer">
                  <div className="shared-post-actions">
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
                        handleToggleComments(post.SharedPost);
                      }}
                    >
                      <span><i className="fa-solid fa-comments"></i></span> Bình luận bài gốc
                    </button>
                  </div>

                  <div className="shared-post-stats">
                    <span>❤️ {post.SharedPost.LikesCount} lượt thích</span>
                    <span><i className="fa-solid fa-comments"></i> {post.SharedPost.CommentsCount} bình luận</span>
                  </div>
                </div>

                {activeCommentPostId === post.SharedPost.Id && (
                  <div className="shared-post-comments">
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
            )}
          </div>

          <div className="post-stats">
            <span>❤️ {post.LikesCount} lượt thích</span>
            <span><i className="fa-solid fa-comments"></i> {post.CommentsCount ?? (commentsByPost[post.Id]?.length ?? 0)} bình luận</span>
          </div>

          <div className="post-actions">
            <button
              className={`post-action-btn ${likedPosts.has(post.Id) ? 'liked' : ''}`}
              onClick={() => handleLike(post)}
            >
              <span>{likedPosts.has(post.Id) ? '❤️' : '🤍'}</span>
              {likedPosts.has(post.Id) ? 'Bỏ thích' : 'Thích'}
            </button>
            <button className="post-action-btn" onClick={() => handleToggleComments(post)}>
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
      </div>
    </div>
  );
}