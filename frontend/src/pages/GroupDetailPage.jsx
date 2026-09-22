import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useGroups } from '../contexts/GroupsContext';
import { getPostsByGroup, createPost, deletePost, getCommentsByPost, createComment, updateComment, deleteComment, likePost, unlikePost, sharePost } from '../api';
import { joinGroupChannel, leaveGroupChannel } from '../utils/postHubConnection';
import Header from '../components/Header';
import CommentSection from '../components/CommentSection';
import '../styles/GroupDetailPage.css';
import { mergeCommentIntoList } from '../utils/commentNormalize';

export default function GroupDetailPage() {
  const { groupSlug } = useParams();
  const [group, setGroup] = useState(null);
  const [posts, setPosts] = useState([]);
  const [currentUser, setCurrentUser] = useState(null);
  const [activeCommentPostId, setActiveCommentPostId] = useState(null);
  const [commentsByPost, setCommentsByPost] = useState({});
  const [loading, setLoading] = useState(true);
  const [newPostContent, setNewPostContent] = useState('');
  const [newPostFile, setNewPostFile] = useState(null);
  const [postImagePreview, setPostImagePreview] = useState('');
  const [posting, setPosting] = useState(false);
  const [error, setError] = useState('');
  const [activePostMenuId, setActivePostMenuId] = useState(null);
  const [showGroupMenu, setShowGroupMenu] = useState(false);
  const [postSearchQuery, setPostSearchQuery] = useState('');
  const [showShareModal, setShowShareModal] = useState(false);
  const [sharePostId, setSharePostId] = useState(null);
  const [shareCaption, setShareCaption] = useState('');
  const [sharePending, setSharePending] = useState(false);
  const navigate = useNavigate();
  const { groups, leaveGroup, joinGroup } = useGroups();

  const normalizePost = (post) => ({
    id: post.id || post.Id,
    groupId: post.groupId || post.GroupId,
    userId: post.userId || post.UserId,
    UserId: post.userId || post.UserId,
    username: post.username || post.UserFullName || post.UserName || 'Người dùng',
    userProfilePictureUrl: post.userProfilePictureUrl || post.UserProfilePictureUrl,
    content: post.content || post.Content,
    imageUrl: post.imageUrl || post.ImageUrl,
    createdAt: post.createdAt || post.CreatedAt,
    likesCount: post.likesCount ?? post.LikesCount ?? 0,
    commentsCount: post.commentsCount ?? post.CommentsCount ?? 0,
    likedBy: post.likedBy || post.LikedByUserIds || []
  });

  const filteredPosts = posts.filter((post) => {
    const query = postSearchQuery.trim().toLowerCase();
    if (!query) return true;

    const content = (post.content || '').toLowerCase();
    const author = (post.username || '').toLowerCase();
    return content.includes(query) || author.includes(query);
  });

  useEffect(() => {
    const loadGroupAndPosts = async () => {
      const userData = JSON.parse(localStorage.getItem('user'));
      setCurrentUser(userData);

      // Find group by slug from context
      const foundGroup = groups.find(g => g.slug === groupSlug);
      if (foundGroup) {
        setGroup(foundGroup);

        try {
          const groupPosts = await getPostsByGroup(foundGroup.id);

          const normalizedPosts = groupPosts.map((p) => {
            const normalized = normalizePost(p);
            return {
              ...normalized,
              likedBy: normalized.likedBy || [],
              likesCount: normalized.likesCount || 0,
              commentsCount: normalized.commentsCount || 0
            };
          });

          setPosts(normalizedPosts);
        } catch (error) {
          console.error('Error loading group posts:', error);
          setPosts([]);
        }
      }

      setLoading(false);
    };

    loadGroupAndPosts();
  }, [groupSlug, groups]);

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
    if (!group?.id) {
      return;
    }

    joinGroupChannel(group.id).catch((err) => {
      console.error('Cannot join group realtime channel:', err);
    });

    const handleGroupMemberCountUpdated = (event) => {
      const payload = event.detail || {};
      const payloadGroupId = payload.groupId ?? payload.GroupId;
      if (payloadGroupId !== group.id) return;

      const memberCount = payload.memberCount ?? payload.MemberCount;
      console.log('[GroupDetailPage] 📊 Member count updated:', memberCount);
      
      setGroup((prev) => (prev ? { ...prev, memberCount } : prev));
    };

    const handleGroupPostCreated = (event) => {
      const payload = event.detail || {};
      const payloadGroupId = payload.groupId ?? payload.GroupId;
      if (payloadGroupId !== group.id) return;

      const normalized = normalizePost(payload);
      setPosts((prev) => {
        const exists = prev.some((p) => p.id === normalized.id);
        if (exists) {
          return prev.map((p) => (p.id === normalized.id ? { ...p, ...normalized } : p));
        }
        return [{ ...normalized, likedBy: normalized.likedBy || [] }, ...prev];
      });
    };

    const handleGroupPostDeleted = (event) => {
      const payload = event.detail || {};
      const payloadGroupId = payload.groupId ?? payload.GroupId;
      if (payloadGroupId !== group.id) return;

      const postId = payload.postId ?? payload.PostId;
      setPosts((prev) => prev.filter((p) => p.id !== postId));
      setCommentsByPost((prev) => {
        if (!(postId in prev)) return prev;
        const next = { ...prev };
        delete next[postId];
        return next;
      });
    };

    const handleGroupCommentAdded = (event) => {
      const payload = event.detail || {};
      const payloadGroupId = payload.groupId ?? payload.GroupId;
      if (payloadGroupId !== group.id) return;

      const postId = payload.postId ?? payload.PostId;
      const comment = payload.comment ?? payload.Comment;
      if (!postId || !comment) return;

      setCommentsByPost((prev) => ({
        ...prev,
        [postId]: mergeCommentIntoList(prev[postId] || [], comment, { prepend: true })
      }));
      setPosts((prev) => prev.map((post) =>
        post.id === postId
          ? { ...post, commentsCount: payload.commentsCount ?? payload.CommentsCount ?? (post.commentsCount ?? 0) + 1 }
          : post
      ));
    };

    const handleGroupCommentUpdated = (event) => {
      const payload = event.detail || {};
      const payloadGroupId = payload.groupId ?? payload.GroupId;
      if (payloadGroupId !== group.id) return;

      const postId = payload.postId ?? payload.PostId;
      const comment = payload.comment ?? payload.Comment;
      if (!postId || !comment) return;
      const commentId = comment.id ?? comment.Id;

      setCommentsByPost((prev) => ({
        ...prev,
        [postId]: (prev[postId] || []).map((item) =>
          item.id === commentId ? { ...item, ...comment } : item
        )
      }));
    };

    const handleGroupCommentDeleted = (event) => {
      const payload = event.detail || {};
      const payloadGroupId = payload.groupId ?? payload.GroupId;
      if (payloadGroupId !== group.id) return;

      const postId = payload.postId ?? payload.PostId;
      const commentId = payload.commentId ?? payload.CommentId;
      if (!postId || !commentId) return;

      setCommentsByPost((prev) => ({
        ...prev,
        [postId]: (prev[postId] || []).filter((comment) => comment.id !== commentId)
      }));
      setPosts((prev) => prev.map((post) =>
        post.id === postId
          ? { ...post, commentsCount: payload.commentsCount ?? payload.CommentsCount ?? Math.max(0, (post.commentsCount ?? 1) - 1) }
          : post
      ));
    };

    const handleGroupPostLiked = (event) => {
      const payload = event.detail || {};
      const payloadGroupId = payload.groupId ?? payload.GroupId;
      if (payloadGroupId !== group.id) return;

      const postId = payload.postId ?? payload.PostId;
      const actorUserId = payload.userId ?? payload.UserId;
      if (!postId || !actorUserId) return;

      setPosts((prev) => prev.map((post) => {
        if (post.id === postId) {
          const likedBy = post.likedBy || [];
          return {
            ...post,
            likesCount: payload.likesCount ?? payload.LikesCount ?? post.likesCount,
            likedBy: likedBy.includes(actorUserId) ? likedBy : [...likedBy, actorUserId]
          };
        }
        if ((post.sharedPost?.id || post.SharedPost?.Id) === postId) {
          if (post.sharedPost) {
            const nestedLikedBy = post.sharedPost.likedBy || [];
            return {
              ...post,
              sharedPost: {
                ...post.sharedPost,
                likesCount: payload.likesCount ?? payload.LikesCount ?? post.sharedPost.likesCount,
                likedBy: nestedLikedBy.includes(actorUserId) ? nestedLikedBy : [...nestedLikedBy, actorUserId]
              }
            };
          }
          const nestedLikedBy = post.SharedPost?.LikedByUserIds || [];
          return {
            ...post,
            SharedPost: {
              ...post.SharedPost,
              LikesCount: payload.likesCount ?? payload.LikesCount ?? post.SharedPost?.LikesCount,
              LikedByUserIds: nestedLikedBy.includes(actorUserId) ? nestedLikedBy : [...nestedLikedBy, actorUserId]
            }
          };
        }
        return post;
      }));
    };

    const handleGroupPostUnliked = (event) => {
      const payload = event.detail || {};
      const payloadGroupId = payload.groupId ?? payload.GroupId;
      if (payloadGroupId !== group.id) return;

      const postId = payload.postId ?? payload.PostId;
      const actorUserId = payload.userId ?? payload.UserId;
      if (!postId || !actorUserId) return;

      setPosts((prev) => prev.map((post) => {
        if (post.id === postId) {
          return {
            ...post,
            likesCount: payload.likesCount ?? payload.LikesCount ?? post.likesCount,
            likedBy: (post.likedBy || []).filter((id) => id !== actorUserId)
          };
        }
        if ((post.sharedPost?.id || post.SharedPost?.Id) === postId) {
          if (post.sharedPost) {
            return {
              ...post,
              sharedPost: {
                ...post.sharedPost,
                likesCount: payload.likesCount ?? payload.LikesCount ?? post.sharedPost.likesCount,
                likedBy: (post.sharedPost.likedBy || []).filter((id) => id !== actorUserId)
              }
            };
          }
          return {
            ...post,
            SharedPost: {
              ...post.SharedPost,
              LikesCount: payload.likesCount ?? payload.LikesCount ?? post.SharedPost?.LikesCount,
              LikedByUserIds: (post.SharedPost?.LikedByUserIds || []).filter((id) => id !== actorUserId)
            }
          };
        }
        return post;
      }));
    };

    window.addEventListener('signalr:group-post-created', handleGroupPostCreated);
    window.addEventListener('signalr:group-post-deleted', handleGroupPostDeleted);
    window.addEventListener('signalr:group-comment-added', handleGroupCommentAdded);
    window.addEventListener('signalr:group-comment-updated', handleGroupCommentUpdated);
    window.addEventListener('signalr:group-comment-deleted', handleGroupCommentDeleted);
    window.addEventListener('signalr:group-post-liked', handleGroupPostLiked);
    window.addEventListener('signalr:group-post-unliked', handleGroupPostUnliked);
    window.addEventListener('signalr:group-member-count-updated', handleGroupMemberCountUpdated);

    return () => {
      leaveGroupChannel(group.id).catch(() => {});
      window.removeEventListener('signalr:group-post-created', handleGroupPostCreated);
      window.removeEventListener('signalr:group-post-deleted', handleGroupPostDeleted);
      window.removeEventListener('signalr:group-comment-added', handleGroupCommentAdded);
      window.removeEventListener('signalr:group-comment-updated', handleGroupCommentUpdated);
      window.removeEventListener('signalr:group-comment-deleted', handleGroupCommentDeleted);
      window.removeEventListener('signalr:group-post-liked', handleGroupPostLiked);
      window.removeEventListener('signalr:group-post-unliked', handleGroupPostUnliked);
      window.removeEventListener('signalr:group-member-count-updated', handleGroupMemberCountUpdated);
    };
  }, [group?.id]);

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    
    // Dispatch event to notify App.jsx about token change
    window.dispatchEvent(new Event('tokenUpdated'));
    
    navigate('/login');
  };

  const handleBackToGroup = () => {
    navigate('/group');
  };

  const handleLike = (post) => {
    if (!currentUser) {
      console.warn('No current user');
      return;
    }

    try {
      const likedBy = post.likedBy || [];
      const isLiked = likedBy.includes(currentUser.Id);
      const postId = post.id || post.Id;
      let newLikesCount = post.likesCount || 0;

      if (isLiked) {
        unlikePost(postId, currentUser.Id).catch((err) => {
          console.error('Error unliking group post:', err);
        });
        newLikesCount = Math.max(0, newLikesCount - 1);
      } else {
        likePost(postId, currentUser.Id).catch((err) => {
          console.error('Error liking group post:', err);
        });
        newLikesCount += 1;
      }

      setPosts((prev) => prev.map((p) => {
        const normalizedId = p.id || p.Id;
        if (normalizedId === postId) {
          const nextLikedBy = isLiked
            ? likedBy.filter((id) => id !== currentUser.Id)
            : [...likedBy, currentUser.Id];
          return {
            ...p,
            likedBy: nextLikedBy,
            likesCount: newLikesCount
          };
        }
        if ((p.sharedPost?.id || p.SharedPost?.Id) === postId) {
          const nested = p.sharedPost || p.SharedPost;
          const nestedLikedBy = nested?.likedBy || nested?.LikedByUserIds || [];
          const nextNestedLikedBy = isLiked
            ? nestedLikedBy.filter((id) => id !== currentUser.Id)
            : [...nestedLikedBy, currentUser.Id];
          if (p.sharedPost) {
            return {
              ...p,
              sharedPost: {
                ...p.sharedPost,
                likedBy: nextNestedLikedBy,
                likesCount: newLikesCount
              }
            };
          }
          return {
            ...p,
            SharedPost: {
              ...p.SharedPost,
              LikedByUserIds: nextNestedLikedBy,
              LikesCount: newLikesCount
            }
          };
        }
        return p;
      }));
      console.log('Like/Unlike successful');
    } catch (err) {
      console.error('Error liking post:', err);
    }
  };

  const handleShare = (post) => {
    setSharePostId(post.id);
    setShareCaption('');
    setShowShareModal(true);
  };

  const handleSubmitShare = async () => {
    if (!sharePostId) return;
    try {
      setSharePending(true);
      await sharePost(sharePostId, { content: shareCaption || '' });
      window.dispatchEvent(new Event('postShared'));
      setError('');
      setShowShareModal(false);
      setSharePostId(null);
      setShareCaption('');
    } catch (err) {
      console.error('Error sharing group post:', err);
      setError('Không thể chia sẻ bài viết lúc này');
    } finally {
      setSharePending(false);
    }
  };

  const handleGroupFileChange = (e) => {
    const file = e.target.files?.[0];
    if (!file) {
      return;
    }

    setNewPostFile(file);

    const reader = new FileReader();
    reader.onload = () => {
      setPostImagePreview(reader.result || '');
    };
    reader.onerror = () => {
      setError('Lỗi đọc tệp hình ảnh');
    };
    reader.readAsDataURL(file);
  };

  const handleLeaveGroup = () => {
    leaveGroup(group.id);
    // Navigate back to group list with discover tab selected
    navigate('/group', { state: { tab: 'discover' } });
  };

  const handleJoinGroup = async () => {
    try {
      await joinGroup(group.id);
      // Refresh local group state immediately so UI updates without reload
      setGroup((prev) => (prev ? { ...prev, isJoined: true, memberCount: (prev.memberCount || 0) + 1 } : prev));
    } catch (err) {
      console.error('Error joining group:', err);
      setError('Không thể vào nhóm lúc này');
    }
  };

  const handleToggleComments = (post) => {
    setActiveCommentPostId((current) => (current === post.id ? null : post.id));
  };

  const handleOpenUserProfile = (userId) => {
    if (!userId) return;
    navigate(`/user-profile/${userId}`);
  };

  const handleCreatePost = async (e) => {
    e.preventDefault();
    if (!newPostContent.trim() && !postImagePreview) {
      setError('Vui lòng nhập nội dung hoặc chọn hình ảnh');
      return;
    }

    setPosting(true);
    try {
      const imageUrl = postImagePreview || null;

      // Create post via API
      const createdPost = await createPost({
        content: newPostContent,
        imageUrl,
        groupId: group.id
      });

      if (createdPost) {
        const normalized = normalizePost(createdPost);
        setPosts((prev) => {
          const exists = prev.some((p) => p.id === normalized.id);
          if (exists) return prev;
          return [{ ...normalized, likedBy: normalized.likedBy || [], commentsCount: normalized.commentsCount ?? 0 }, ...prev];
        });
      }

      setNewPostContent('');
      setNewPostFile(null);
      setPostImagePreview('');
      setActivePostMenuId(null);
      setError('');
    } catch (err) {
      setError(err.message);
    } finally {
      setPosting(false);
    }
  };

  const handleDeletePost = async (post) => {
    if (!currentUser || post.userId !== currentUser.Id) {
      setError('Bạn chỉ có thể xóa bài viết của chính mình');
      return;
    }

    if (!window.confirm('Bạn có chắc chắn muốn xóa bài viết này?')) {
      return;
    }

    try {
      await deletePost(post.id);
      setPosts(posts.filter((p) => p.id !== post.id));
      setActivePostMenuId(null);
      setError('');
    } catch (err) {
      setError(err.message);
    }
  };

  const handleAddComment = async (postId, content) => {
    try {
      const createdComment = await createComment(postId, content);
      setCommentsByPost((prev) => ({
        ...prev,
        [postId]: mergeCommentIntoList(prev[postId] || [], createdComment, { prepend: true })
      }));
      setPosts((prev) => prev.map((post) =>
        post.id === postId
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
        post.id === postId
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

  if (loading) {
    return <div>Đang tải...</div>;
  }

  if (!group) {
    return (
      <div className="group-detail-wrapper">
        <Header onLogout={handleLogout} />
        <div className="not-found">
          <p>Không tìm thấy nhóm</p>
          <button onClick={handleBackToGroup}>Quay lại</button>
        </div>
      </div>
    );
  }

  return (
    <div className="group-detail-wrapper">
      <Header onLogout={handleLogout} />
      
      <div className="group-detail-container">
        {/* Left Sidebar */}
        <aside className="group-detail-sidebar">
          <nav className="group-sidebar-nav">
            <div className="search-wrapper">
              <input
                type="text"
                placeholder="Tìm kiếm"
                className="search-input"
              />
              <span className="search-icon"><i class="fa-solid fa-magnifying-glass"></i></span>
            </div>

            <div className="nav-item active">
              <span className="nav-icon"><i class="fa-solid fa-users"></i></span>
              <span>Nhóm của bạn</span>
            </div>
            <div className="nav-item">
              <span className="nav-icon"><i class="fa-solid fa-magnifying-glass"></i></span>
              <span>Khám phá</span>
            </div>
            <div 
              className="nav-item create-group-item"
              onClick={() => navigate('/creategroup')}
              style={{ cursor: 'pointer' }}
            >
              <span className="nav-icon">➕</span>
              <span>Tạo nhóm mới</span>
            </div>
          </nav>
        </aside>

        {/* Main Content */}
        <main className="group-detail-main">
          {/* Group Header */}
          {(() => {
            let parsedImages = null;
            try {
              if (group.imageUrl && group.imageUrl.startsWith('[')) {
                parsedImages = JSON.parse(group.imageUrl);
              } else if (group.imageUrl) {
                parsedImages = [group.imageUrl];
              }
            } catch (e) {}
            const coverImg = parsedImages && parsedImages.length > 0 ? parsedImages[0] : null;

            return (
              <div className="group-detail-header" style={{ position: 'relative', overflow: 'hidden', padding: coverImg ? '120px 20px 20px' : '20px', display: 'flex', flexDirection: 'column', gap: '10px' }}>
                {coverImg && (
                  <img src={coverImg} alt={group.name} style={{ position: 'absolute', top: 0, left: 0, width: '100%', height: '100%', objectFit: 'cover', zIndex: 0, opacity: 0.3 }} />
                )}
                <div style={{ position: 'relative', zIndex: 1, display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', width: '100%' }}>
                  <div className="group-header-left" style={{ display: 'flex', flexDirection: 'column', gap: '5px' }}>
                    <button className="btn-back" onClick={handleBackToGroup} style={{ alignSelf: 'flex-start' }}>
                      ← Quay lại
                    </button>
                    <h1 className="group-title" style={{ fontSize: '28px', margin: 0 }}>{group.name}</h1>
                    {group.description && <p style={{ margin: 0, color: '#555', fontSize: '15px' }}>{group.description}</p>}
                    <p style={{ margin: 0, color: '#888', fontSize: '13px' }}>{group.memberCount} thành viên</p>
                  </div>
            {group.isJoined ? (
              <div className="group-menu-container">
                <button
                  className="btn-group-menu"
                  onClick={() => setShowGroupMenu(!showGroupMenu)}
                >
                  ⋯
                </button>
                {showGroupMenu && (
                  <div className="group-menu-dropdown">
                    <button
                      className="menu-item leave"
                      onClick={() => {
                        handleLeaveGroup();
                        setShowGroupMenu(false);
                      }}
                    >
                      Rời nhóm
                    </button>
                  </div>
                )}
              </div>
                ) : (
                  <button className="btn-post" onClick={handleJoinGroup}>Vào nhóm</button>
                )}
                </div>
              </div>
            );
          })()}

          {group.isJoined ? (
          <section className="create-post-section">
            <div className="create-post-header">
              <div className="user-avatar">
                {currentUser?.ProfilePictureUrl ? (
                  <img 
                    src={currentUser.ProfilePictureUrl} 
                    alt="Avatar"
                    style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                    onError={(e) => {
                      console.warn('Failed to load avatar image:', currentUser.ProfilePictureUrl);
                      e.target.style.display = 'none';
                      if (e.target.nextElementSibling) {
                        e.target.nextElementSibling.style.display = 'flex';
                      }
                    }}
                  />
                ) : null}
                <div className="avatar-placeholder" style={{ display: currentUser?.ProfilePictureUrl ? 'none' : 'flex' }}>
                  <i className="fa-solid fa-user"></i>
                </div>
              </div>
              <p className="create-post-prompt">
                Bạn đang nghĩ gì? Hãy chia sẻ cảm nghĩ của bạn đến thành viên nhóm...
              </p>
            </div>

            {error && <div className="error-message">{error}</div>}

            <form onSubmit={handleCreatePost} className="create-post-form">
              <textarea
                value={newPostContent}
                onChange={(e) => setNewPostContent(e.target.value)}
                placeholder="Chia sẻ suy nghĩ của bạn..."
                className="post-textarea"
                rows="4"
              />
              
              <div className="post-form-bottom">
                <div className="file-input-wrapper">
                  <label htmlFor="group-post-image-input" className="file-input-label">
                    <i className="fa-solid fa-image"></i>
                    <span>Thêm hình ảnh</span>
                  </label>
                  <input
                    id="group-post-image-input"
                    type="file"
                    accept="image/jpeg,image/png,image/gif,image/webp"
                    onChange={handleGroupFileChange}
                    className="post-file-input"
                  />
                  {newPostFile && (
                    <span className="file-selected">✓ {newPostFile.name}</span>
                  )}
                </div>
                <button 
                  type="submit" 
                  className="btn-post"
                  disabled={posting}
                >
                  {posting ? 'Đang đăng...' : 'Đăng'}
                </button>
              </div>
              {postImagePreview && (
                <div className="post-image-preview-wrapper">
                  <img src={postImagePreview} alt="Preview" className="post-image-preview" />
                </div>
              )}
            </form>
          </section>
          ) : null}

          <section className="posts-feed">
            <div className="search-wrapper" style={{ marginBottom: '12px' }}>
              <input
                type="text"
                placeholder="Tìm kiếm bài viết trong nhóm"
                className="search-input"
                value={postSearchQuery}
                onChange={(e) => setPostSearchQuery(e.target.value)}
              />
              <span className="search-icon"><i className="fa-solid fa-magnifying-glass"></i></span>
            </div>

            {filteredPosts.length === 0 ? (
              <p className="no-posts">Chưa có bài viết nào trong nhóm</p>
            ) : (
              filteredPosts.map((post) => (
                <div key={post.id} className="post-card">
                  <div className="post-header">
                    <div className="post-user-info post-user-clickable" onClick={() => handleOpenUserProfile(post.userId || post.UserId)}>
                      <div className="post-avatar">
                        {post.userProfilePictureUrl ? (
                          <img 
                            src={post.userProfilePictureUrl} 
                            alt="User Avatar"
                            style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                          />
                        ) : (
                          <i className="fa-solid fa-user"></i>
                        )}
                      </div>
                      <div className="post-user-details">
                        <p className="post-username">{post.username}</p>
                        <p className="post-time">
                          {new Date(post.createdAt).toLocaleDateString('vi-VN')}
                        </p>
                      </div>
                    </div>
                <div className="post-menu-container">
                  <button
                    className="post-menu-btn"
                    onClick={() => setActivePostMenuId(activePostMenuId === post.id ? null : post.id)}
                  >
                    ⋯
                  </button>
                  {activePostMenuId === post.id && currentUser?.Id === post.userId && (
                    <div className="post-menu-dropdown">
                      <button className="menu-item delete" onClick={() => handleDeletePost(post)}>
                        <i className="fa-solid fa-trash"></i> Xóa bài viết
                      </button>
                    </div>
                  )}
                </div>
                  </div>

                  {/* Post Content */}
                  <div className="post-content">
                    <p>{post.content}</p>
                  </div>

                  {/* Post Image */}
                  {post.imageUrl && (
                    <div className="post-image-container">
                      <img src={post.imageUrl} alt="Post" className="post-image" />
                    </div>
                  )}

                  <div className="post-stats">
                    <span>❤️ {post.likesCount} lượt thích</span>
                    <span><i className="fa-solid fa-comments"></i> {(commentsByPost[post.id]?.length ?? 0)} bình luận</span>
                  </div>

                  <div className="post-actions">
                    <button 
                      className={`post-action-btn ${(post.likedBy || []).includes(currentUser?.Id) ? 'liked' : ''}`}
                      onClick={() => handleLike(post)}
                    >
                      <span>{(post.likedBy || []).includes(currentUser?.Id) ? '❤️' : '🤍'}</span> 
                      {(post.likedBy || []).includes(currentUser?.Id) ? 'Bỏ thích' : 'Thích'}
                    </button>
                    <button className="post-action-btn" onClick={() => handleToggleComments(post)}>
                      <span><i className="fa-solid fa-comments"></i></span> Bình luận
                    </button>
                    <button className="post-action-btn" onClick={() => handleShare(post)}>
                      <span><i className="fa-solid fa-share"></i></span> Chia sẻ
                    </button>
                  </div>

                  {activeCommentPostId === post.id && (
                    <CommentSection
                      post={post}
                      comments={commentsByPost[post.id] || []}
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
          </section>
        </main>
      </div>

      {showShareModal && (
        <div className="modal-overlay" onClick={() => setShowShareModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Chia sẻ bài viết</h2>
              <button
                className="modal-close-btn"
                onClick={() => setShowShareModal(false)}
              >
                ✕
              </button>
            </div>

            <div className="share-modal-body">
              <textarea
                value={shareCaption}
                onChange={(e) => setShareCaption(e.target.value)}
                placeholder="Thêm chú thích cho bài viết của bạn..."
                className="post-textarea"
                rows="4"
              />

              <div className="share-modal-actions">
                <button
                  type="button"
                  className="btn-post"
                  onClick={handleSubmitShare}
                  disabled={sharePending}
                >
                  {sharePending ? 'Đang chia sẻ...' : 'Chia sẻ'}
                </button>
                <button
                  type="button"
                  className="btn-secondary"
                  onClick={() => setShowShareModal(false)}
                >
                  Hủy
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
