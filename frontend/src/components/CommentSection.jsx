import { useState, useEffect } from 'react';
import { getUser } from '../api';
import { commentHubConnection } from '../utils/commentHubConnection';
import { formatCommentDateTime } from '../utils/commentDateTime';
import { dedupeCommentList, mergeCommentIntoList, sameCommentId } from '../utils/commentNormalize';
import '../styles/CommentSection.css';

export default function CommentSection({ post, comments, onClose, onAddComment, onDeleteComment, onEditComment, currentUser }) {
  const postId = post?.Id ?? post?.id;
  const postUserId = post?.UserId ?? post?.userId;
  const [commentList, setCommentList] = useState(comments ?? []);
  const [newComment, setNewComment] = useState('');
  const [likedComments, setLikedComments] = useState(new Set());
  const [commentUsers, setCommentUsers] = useState({});
  const [editingCommentId, setEditingCommentId] = useState(null);
  const [editingCommentText, setEditingCommentText] = useState('');
  const [activeMenuCommentId, setActiveMenuCommentId] = useState(null);

  useEffect(() => {
    setNewComment('');
  }, [postId]);

  useEffect(() => {
    setCommentList(dedupeCommentList(comments ?? []));
  }, [comments]);

  useEffect(() => {
    const liked = new Set();
    commentList.forEach(comment => {
      if (comment.likes && Array.isArray(comment.likes) && comment.likes.includes(currentUser?.Id)) {
        liked.add(comment.id);
      }
    });
    setLikedComments(liked);
  }, [commentList, currentUser?.Id]);

  // Load user data for comments that don't have profile picture
  useEffect(() => {
    const loadUserData = async () => {
      const newUsers = { ...commentUsers };
      
      for (const comment of commentList) {
        if (comment.userId && !newUsers[comment.userId]) {
          try {
            const userData = await getUser(comment.userId);
            newUsers[comment.userId] = userData;
          } catch (err) {
            console.error(`Error loading user ${comment.userId}:`, err);
          }
        }
      }
      
      if (Object.keys(newUsers).length > Object.keys(commentUsers).length) {
        setCommentUsers(newUsers);
      }
    };

    if (commentList.length > 0) {
      loadUserData();
    }
  }, [commentList]);

  // Listen for user updates to refresh comment user data (e.g., when profile name changes)
  useEffect(() => {
    const handleUserUpdate = async () => {
      console.log('userUpdated event received in CommentSection - refreshing user cache');
      const newUsers = {};
      
      for (const comment of commentList) {
        if (comment.userId && !newUsers[comment.userId]) {
          try {
            const userData = await getUser(comment.userId);
            newUsers[comment.userId] = userData;
          } catch (err) {
            console.error(`Error loading user ${comment.userId}:`, err);
          }
        }
      }
      
      if (Object.keys(newUsers).length > 0) {
        setCommentUsers(newUsers);
      }
    };

    window.addEventListener('userUpdated', handleUserUpdate);
    return () => {
      window.removeEventListener('userUpdated', handleUserUpdate);
    };
  }, [commentList]);

  useEffect(() => {
    if (postId == null) return undefined;

    let cancelled = false;
    const unsubscribes = [];

    const setup = async () => {
      const token = localStorage.getItem('token');
      if (!token) return;

      try {
        if (!commentHubConnection.isActive()) {
          await commentHubConnection.connect(token);
        }
        if (cancelled) return;

        await commentHubConnection.joinPostGroup(postId);
        if (cancelled) {
          await commentHubConnection.leavePostGroup(postId).catch(() => {});
          return;
        }

        unsubscribes.push(
          commentHubConnection.onCommentCreated((comment) => {
            if (!comment) return;
            const incomingPostId = comment.PostId ?? comment.postId;

            if (String(incomingPostId) !== String(postId)) return;

            setCommentList((prev) => mergeCommentIntoList(prev, comment, { prepend: true }));
          })
        );

        unsubscribes.push(
          commentHubConnection.onCommentUpdated((comment) => {
            if (!comment) return;
            const incomingPostId = comment.PostId ?? comment.postId;
            const incomingId = comment.Id ?? comment.id;

            if (String(incomingPostId) !== String(postId)) return;

            setCommentList((prev) =>
              prev.map((item) =>
                sameCommentId(item.id ?? item.Id, incomingId)
                  ? { ...item, content: comment.Content ?? comment.content }
                  : item
              )
            );
          })
        );

        unsubscribes.push(
          commentHubConnection.onCommentDeleted((payload) => {
            if (!payload) return;
            const incomingPostId = payload.PostId ?? payload.postId;
            const incomingId = payload.Id ?? payload.id;

            if (String(incomingPostId) !== String(postId)) return;

            setCommentList((prev) =>
              prev.filter((item) => !sameCommentId(item.id ?? item.Id, incomingId))
            );
          })
        );
      } catch (err) {
        if (!cancelled) {
          console.error('CommentHub connection error:', err);
        }
      }
    };

    setup();

    return () => {
      cancelled = true;
      unsubscribes.forEach((u) => u?.());
      commentHubConnection.leavePostGroup(postId).catch(() => {});
    };
  }, [postId]);

  const handleSubmit = () => {
    if (!newComment.trim()) return;
    onAddComment(postId, newComment.trim());
    setNewComment('');
  };

  const handleEditComment = (commentId, currentText) => {
    console.log('handleEditComment - id:', commentId, 'text:', currentText);
    setEditingCommentId(commentId);
    setEditingCommentText(currentText);
    setActiveMenuCommentId(null);
  };

  const handleSaveEdit = (commentId) => {
    console.log('handleSaveEdit - id:', commentId, 'newText:', editingCommentText);
    if (!editingCommentText.trim()) return;
    
    // Call parent callback to update state
    if (onEditComment) {
      onEditComment(postId, commentId, editingCommentText.trim());
    }
    
    // Close editing mode
    setEditingCommentId(null);
    setEditingCommentText('');
  };

  const handleCancelEdit = () => {
    setEditingCommentId(null);
    setEditingCommentText('');
  };

  return (
    <div className="comment-modal-backdrop" onClick={onClose}>
      <div className="comment-modal" onClick={(e) => e.stopPropagation()}>
        <div className="comment-modal-header">
          <div>
            <h2>Bình luận</h2>
            <p>{commentList.length} bình luận</p>
          </div>
          <button className="close-comment-modal" onClick={onClose}>✕</button>
        </div>

        <div className="comment-modal-body">
          {commentList.length === 0 ? (
            <div className="empty-comment-state">
              <p>Chưa có bình luận nào. Bạn hãy là người bình luận đầu tiên!</p>
            </div>
          ) : (
            <div className="comment-thread">
              {commentList.map((comment) => {
                // Get user data from loaded users or use comment data
                const userData = commentUsers[comment.userId];
                const displayName = userData?.FullName || userData?.fullName || comment.userName;
                const displayProfilePic = userData?.ProfilePictureUrl || userData?.profilePictureUrl || comment.userProfilePictureUrl;
                
                // Check if current user can delete this comment
                const canDeleteComment = currentUser?.Id === comment.userId || currentUser?.Id === postUserId;
                
                console.log('Rendering comment:', {
                  id: comment.id,
                  userId: comment.userId,
                  content: comment.content,
                  createdAt: comment.createdAt,
                  canDelete: canDeleteComment,
                  currentUserId: currentUser?.Id,
                  postUserId,
                  displayName
                });

                return (
                  <div key={comment.id} className="comment-item">
                    <div className="comment-avatar">
                      {displayProfilePic ? (
                        <img 
                          src={displayProfilePic} 
                          alt={displayName}
                          style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                          onError={(e) => {
                            console.warn('Failed to load comment avatar:', displayProfilePic);
                            e.target.style.display = 'none';
                            const fallbackIcon = e.target.nextElementSibling;
                            if (fallbackIcon) {
                              fallbackIcon.style.display = 'flex';
                            }
                          }}
                        />
                      ) : null}
                      <i 
                        className="fa-solid fa-user" 
                        style={{ 
                          display: displayProfilePic ? 'none' : 'flex',
                          width: '100%',
                          height: '100%',
                          alignItems: 'center',
                          justifyContent: 'center'
                        }}
                      ></i>
                    </div>
                    <div className="comment-content-wrapper">
                    <div className="comment-header">
                      <div className="comment-user-info">
                        <strong>{displayName}</strong>
                        <p className="comment-time">{formatCommentDateTime(comment.createdAt)}</p>
                      </div>
                      {canDeleteComment && (
                        <div className="comment-menu-container">
                          <button 
                            type="button"
                            className="comment-menu-btn"
                            onClick={(e) => {
                              e.preventDefault();
                              e.stopPropagation();
                              console.log('Menu button clicked for comment:', comment.id);
                              setActiveMenuCommentId(activeMenuCommentId === comment.id ? null : comment.id);
                            }}
                          >
                            ⋯
                          </button>
                          {activeMenuCommentId === comment.id && (
                            <div className="comment-menu-dropdown">
                              <button 
                                type="button"
                                className="menu-item"
                                onClick={(e) => {
                                  e.preventDefault();
                                  e.stopPropagation();
                                  console.log('Edit button clicked');
                                  handleEditComment(comment.id, comment.content);
                                }}
                              >
                                <i className="fa-solid fa-pen"></i> Chỉnh sửa
                              </button>
                              <button 
                                type="button"
                                className="menu-item delete"
                                onClick={(e) => {
                                  e.preventDefault();
                                  e.stopPropagation();
                                  console.log('Delete button clicked');
                                  setActiveMenuCommentId(null);
                                  onDeleteComment(postId, comment.id);
                                }}
                              >
                                <i className="fa-solid fa-trash"></i> Xóa
                              </button>
                            </div>
                          )}
                        </div>
                      )}
                    </div>
                    
                    {editingCommentId === comment.id ? (
                      <div className="comment-edit-mode">
                        <textarea
                          value={editingCommentText}
                          onChange={(e) => setEditingCommentText(e.target.value)}
                          className="comment-edit-input"
                        />
                        <div className="comment-edit-actions">
                          <button 
                            type="button"
                            className="btn-cancel"
                            onClick={handleCancelEdit}
                          >
                            Hủy
                          </button>
                          <button 
                            type="button"
                            className="btn-save"
                            onClick={() => handleSaveEdit(comment.id)}
                          >
                            Lưu
                          </button>
                        </div>
                      </div>
                    ) : (
                      <p className="comment-text">{comment.content}</p>
                    )}
                    
                    <div className="comment-meta-row">
                      <button 
                        className={`comment-like-btn ${likedComments.has(comment.id) ? 'liked' : ''}`}
                        onClick={() => {
                          const newLiked = new Set(likedComments);
                          if (newLiked.has(comment.id)) {
                            newLiked.delete(comment.id);
                            comment.likes = comment.likes.filter(id => id !== currentUser?.Id);
                          } else {
                            newLiked.add(comment.id);
                            if (!comment.likes) comment.likes = [];
                            if (!comment.likes.includes(currentUser?.Id)) {
                              comment.likes.push(currentUser?.Id);
                            }
                          }
                          setLikedComments(newLiked);
                        }}
                        title="Thích bình luận này"
                      >
                        <i className="fa-solid fa-heart"></i>
                        {comment.likes && comment.likes.length > 0 && (
                          <span className="like-count"> {comment.likes.length}</span>
                        )}
                      </button>
                    </div>
                    {comment.replies && comment.replies.length > 0 && (
                      <div className="comment-replies">
                        {comment.replies.map((reply) => (
                          <div key={reply.id} className="comment-reply-item">
                            <div className="comment-avatar">
                              {reply.userProfilePictureUrl ? (
                                <img 
                                  src={reply.userProfilePictureUrl} 
                                  alt={reply.userName}
                                  style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                                  onError={(e) => {
                                    console.warn('Failed to load reply avatar:', reply.userProfilePictureUrl);
                                    e.target.style.display = 'none';
                                    const fallbackIcon = e.target.nextElementSibling;
                                    if (fallbackIcon) {
                                      fallbackIcon.style.display = 'flex';
                                    }
                                  }}
                                />
                              ) : null}
                              <i 
                                className="fa-solid fa-user" 
                                style={{ 
                                  display: reply.userProfilePictureUrl ? 'none' : 'flex',
                                  width: '100%',
                                  height: '100%',
                                  alignItems: 'center',
                                  justifyContent: 'center'
                                }}
                              ></i>
                            </div>
                            <div className="comment-content-wrapper">
                              <div className="comment-top-row">
                                <strong>{reply.userName}</strong>
                                <span>{formatCommentDateTime(reply.createdAt)}</span>
                              </div>
                              <p className="comment-text">{reply.content}</p>
                              <div className="comment-meta-row">
                                <button 
                                  className={`comment-like-btn ${likedComments.has(reply.id) ? 'liked' : ''}`}
                                  onClick={() => {
                                    const newLiked = new Set(likedComments);
                                    if (newLiked.has(reply.id)) {
                                      newLiked.delete(reply.id);
                                      reply.likes = reply.likes.filter(id => id !== currentUser?.Id);
                                    } else {
                                      newLiked.add(reply.id);
                                      if (!reply.likes) reply.likes = [];
                                      if (!reply.likes.includes(currentUser?.Id)) {
                                        reply.likes.push(currentUser?.Id);
                                      }
                                    }
                                    setLikedComments(newLiked);
                                  }}
                                  title="Thích bình luận này"
                                >
                                  <i className="fa-solid fa-heart"></i>
                                  {reply.likes && reply.likes.length > 0 && (
                                    <span className="like-count"> {reply.likes.length}</span>
                                  )}
                                </button>
                              </div>
                            </div>
                          </div>
                        ))}
                      </div>
                    )}
                  </div>
                </div>
                );
              })}
            </div>
          )}
        </div>

        <div className="comment-input-wrapper">
          <div className="comment-avatar" id="comment-input-avatar">
            {currentUser?.ProfilePictureUrl ? (
              <img 
                src={currentUser.ProfilePictureUrl} 
                alt="Your avatar" 
                style={{ width: '100%', height: '100%', objectFit: 'cover', borderRadius: '50%' }}
                onError={(e) => {
                  console.warn('Failed to load comment input avatar:', currentUser.ProfilePictureUrl);
                  e.target.style.display = 'none';
                  const fallbackIcon = e.target.nextElementSibling;
                  if (fallbackIcon) {
                    fallbackIcon.style.display = 'flex';
                  }
                }}
              />
            ) : null}
            <i 
              className="fa-solid fa-user" 
              style={{ 
                display: currentUser?.ProfilePictureUrl ? 'none' : 'flex',
                width: '100%',
                height: '100%',
                alignItems: 'center',
                justifyContent: 'center'
              }}
            ></i>
          </div>
          <input
            type="text"
            value={newComment}
            onChange={(e) => setNewComment(e.target.value)}
            placeholder="Viết bình luận..."
            className="comment-input"
          />
          <button className="btn-submit-comment" onClick={handleSubmit}>
            Gửi
          </button>
        </div>
      </div>
    </div>
  );
}
