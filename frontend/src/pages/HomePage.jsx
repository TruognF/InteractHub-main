import { useState, useEffect, useRef, useMemo } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import { getPosts, getAcceptedFriends, getAllUsers, createPost, createStory, getStories, getPendingRequests, likePost, unlikePost, deletePost, acceptFriendRequest, declineFriendRequest, getUser, getUsersByIds, getCommentsByPost, createComment, updateComment, deleteComment, getNotifications, markFeedNotificationsRead, sharePost, getPostById } from '../api';
import Header from '../components/Header';
import CommentSection from '../components/CommentSection';
import HashtagContent from '../components/HashtagContent';
import ReportPostModal from '../components/ReportPostModal';
import UserAppealModal from '../components/UserAppealModal';
import '../styles/HomePage.css';
import { startConnection } from '../utils/postHubConnection';
import { mergeCommentIntoList, sameCommentId } from '../utils/commentNormalize';
import { isMessageNotificationType } from '../utils/notificationPayload';
import { normalizeStoryPayload, isStoryActive } from '../utils/storyRealtime';
import { normalizeFeedPostFromRealtime } from '../utils/postRealtime';
import { resizeImage } from '../utils/resizeImage';

export default function HomePage() {
  const [posts, setPosts] = useState([]);
  const [friends, setFriends] = useState([]);
  const [friendsInfo, setFriendsInfo] = useState({}); // Store user info for each friend
  const [suggestedUsers, setSuggestedUsers] = useState([]);
  const [allUsers, setAllUsers] = useState([]);
  const [pendingRequests, setPendingRequests] = useState([]);
  const [requestersInfo, setRequestersInfo] = useState({}); // Store user info for each requester
  const [currentUser, setCurrentUser] = useState(null);
  const [newPostContent, setNewPostContent] = useState('');
  const [newPostFile, setNewPostFile] = useState(null);
  const [postImagePreview, setPostImagePreview] = useState('');
  const [loading, setLoading] = useState(true);
  const [postsLoading, setPostsLoading] = useState(false);
  const [error, setError] = useState('');
  const [posting, setPosting] = useState(false);
  const [likedPosts, setLikedPosts] = useState(new Set());
  const [selectedNav, setSelectedNav] = useState('friends');
  const [searchQuery, setSearchQuery] = useState('');
  const [notifications, setNotifications] = useState([]);
  const [stories, setStories] = useState([]);
  const [activeCommentPostId, setActiveCommentPostId] = useState(null);
  const [commentsByPost, setCommentsByPost] = useState({});
  const [activePostMenuId, setActivePostMenuId] = useState(null);
  const [hashtagSearch, setHashtagSearch] = useState(null);
  const [showCreateStoryModal, setShowCreateStoryModal] = useState(false);
  const [unreadMessageCount, setUnreadMessageCount] = useState(0);
  const [searchKeyword, setSearchKeyword] = useState(''); // For keyword search
  const [newStoryContent, setNewStoryContent] = useState('');
  const [newStoryFile, setNewStoryFile] = useState(null);
  const [storyImagePreview, setStoryImagePreview] = useState('');
  const [creatingStory, setCreatingStory] = useState(false);
  const [postPage, setPostPage] = useState(1);
  const [hasMorePosts, setHasMorePosts] = useState(false);
  const [postPageSize] = useState(20);
  const [showShareModal, setShowShareModal] = useState(false);
  const [sharePostId, setSharePostId] = useState(null);
  const [shareCaption, setShareCaption] = useState('');
  const [sharePending, setSharePending] = useState(false);
  const [showReportModal, setShowReportModal] = useState(false);
  const [reportPostId, setReportPostId] = useState(null);
  const [appealingReportId, setAppealingReportId] = useState(null);
  const navigate = useNavigate();
  const location = useLocation();
  const postsEndRef = useRef(null);

  const feedUnreadNotificationCount = useMemo(
    () =>
      notifications.filter(
        (n) => !isMessageNotificationType(n.Type) && !(n.IsRead ?? n.isRead)
      ).length,
    [notifications]
  );

  const parseUtcDate = (dateVal) => {
    if (!dateVal) return null;
    const dateStr = typeof dateVal === 'string' && !dateVal.endsWith('Z') && !dateVal.includes('+')
      ? dateVal + 'Z'
      : dateVal;
    const date = new Date(dateStr);
    return isNaN(date.getTime()) ? null : date;
  };

  const formatTimeAgo = (createdAt) => {
    const date = parseUtcDate(createdAt);
    if (!date) return '';
    const diff = Date.now() - date.getTime();
    if (diff < 60000) return 'vừa xong';
    const minutes = Math.floor(diff / (1000 * 60));
    if (minutes < 60) return `${minutes} phút trước`;
    const hours = Math.floor(diff / (1000 * 60 * 60));
    if (hours < 24) return `${hours} giờ trước`;
    const days = Math.floor(diff / (1000 * 60 * 60 * 24));
    if (days < 30) return `${days} ngày trước`;
    const months = Math.floor(days / 30);
    if (months < 12) return `${months} tháng trước`;
    const years = Math.floor(days / 365);
    return `${years} năm trước`;
  };

  const isPostRelatedNotification = (item) => {
    if (!item || !item.RelatedEntityId) return false;
    const content = item.Content || item.title || '';
    if (content.includes('bị gỡ') || content.includes('Kháng cáo') || content.includes('kháng cáo')) {
      return false;
    }
    const typeStr = String(item.Type || item.type || '').toLowerCase();
    if (
      typeStr === 'postreport' ||
      typeStr === '6' ||
      typeStr === 'friendrequest' ||
      typeStr === '0' ||
      typeStr === 'friendrequestaccepted' ||
      typeStr === '1'
    ) {
      return false;
    }
    const postTypes = ['like', 'comment', 'commentreply', 'postshared', 'friendpublishedpost', '2', '3', '4', '9', '10'];
    if (postTypes.includes(typeStr)) {
      return true;
    }
    return (
      content.toLowerCase().includes('thích') ||
      content.toLowerCase().includes('bình luận') ||
      content.toLowerCase().includes('bài đăng') ||
      content.toLowerCase().includes('bài viết') ||
      content.toLowerCase().includes('chia sẻ')
    );
  };

  const normalizeSearchText = (text = '') => {
    return text
      .toString()
      .normalize('NFD')
      .replace(/([̀-ͯ])/g, '')
      .replace(/đ/g, 'd')
      .replace(/Đ/g, 'd')
      .toLowerCase()
      .trim();
  };

  const matchesSearch = (value = '', query = '') => {
    const normalizedValue = normalizeSearchText(value);
    const normalizedQuery = normalizeSearchText(query);
    return normalizedValue.includes(normalizedQuery);
  };

  const loadNotifications = async (userData) => {
    if (!userData?.Id) return;
    try {
      const notificationData = await getNotifications(userData.Id);
      // Filter out message notifications - only show friend requests, likes, comments, and shares
      const filteredNotifications = (notificationData || [])
        .filter((n) => !isMessageNotificationType(n.Type))
        .sort((a, b) => {
          const timeA = parseUtcDate(a.CreatedAt || a.createdAt)?.getTime() || 0;
          const timeB = parseUtcDate(b.CreatedAt || b.createdAt)?.getTime() || 0;
          return timeB - timeA;
        });
      setNotifications(filteredNotifications);
      
      // Count unread messages separately
      const unreadMessages = (notificationData || [])
        .filter((n) => isMessageNotificationType(n.Type) && !n.IsRead)
        .length;
      setUnreadMessageCount(unreadMessages);
    } catch (err) {
      console.error('Error loading notifications:', err);
    }
  };

  const handleSelectNav = async (nav) => {
    setSelectedNav(nav);
    if (nav === 'notifications' && currentUser) {
      await loadNotifications(currentUser);
      try {
        await markFeedNotificationsRead(currentUser.Id);
        await loadNotifications(currentUser);
      } catch (err) {
        console.error('Error marking feed notifications read:', err);
      }
    }
  };

  const loadPosts = async (userData, pageNum = 1) => {
    try {
      if (pageNum === 1) {
        setPostsLoading(true);
      }
      const response = await getPosts(pageNum, postPageSize);
      const postsData = response.posts || [];
      const paginationData = response.pagination || {};

      // Sort posts by CreatedAt descending (newest first)
      const sortedPosts = (postsData || [])
        .sort((a, b) => new Date(b.CreatedAt) - new Date(a.CreatedAt));

      if (pageNum === 1) {
        // First page: replace posts
        setPosts(sortedPosts);
      } else {
        // Subsequent pages: append new posts
        setPosts((prev) => [...prev, ...sortedPosts]);
      }

      setPostPage(pageNum);
      setHasMorePosts(paginationData.hasMore || false);

      // Initialize liked posts from response data
      const userLikedPostIds = new Set();
      (postsData || []).forEach((post) => {
        if (post.LikedByUserIds?.includes(userData.Id)) {
          userLikedPostIds.add(post.Id);
        }
        if (post.SharedPost?.LikedByUserIds?.includes(userData.Id)) {
          userLikedPostIds.add(post.SharedPost.Id);
        }
      });
      
      if (pageNum === 1) {
        setLikedPosts(new Set(userLikedPostIds));
      } else {
        setLikedPosts((prev) => new Set([...prev, ...userLikedPostIds]));
      }
    } catch (err) {
      console.error('Error loading posts:', err);
    } finally {
      setPostsLoading(false);
    }
  };

  const loadFriendAndRequesterInfo = async (friendsData, requestsData) => {
    const friendIds = friendsData
      .map(f => f.FriendId || f.friendId)
      .filter(Boolean);
    const requesterIds = requestsData
      .map(r => r.UserId)
      .filter(Boolean);

    const idsToLoad = [...new Set([...friendIds, ...requesterIds])]
      .filter(id => !friendsInfo[id] && !requestersInfo[id]);

    if (idsToLoad.length === 0) {
      return;
    }

    try {
      // Batch fetch user info in a single request (avoids N+1 HTTP calls)
      const users = await getUsersByIds(idsToLoad);

      const newFriendsInfo = {};
      const newRequestersInfo = {};

      users.forEach((user) => {
        if (!user) return;
        if (friendIds.includes(user.Id)) {
          newFriendsInfo[user.Id] = user;
        }
        if (requesterIds.includes(user.Id)) {
          newRequestersInfo[user.Id] = user;
        }
      });

      if (Object.keys(newFriendsInfo).length > 0) {
        setFriendsInfo((prev) => ({ ...prev, ...newFriendsInfo }));
      }
      if (Object.keys(newRequestersInfo).length > 0) {
        setRequestersInfo((prev) => ({ ...prev, ...newRequestersInfo }));
      }
    } catch (err) {
      console.error('Error loading friend/requester info:', err);
    }
  };

  const fetchAllUsers = async (searchTerm = '') => {
    try {
      const users = await getAllUsers(searchTerm);
      setAllUsers(users || []);

      if (!searchTerm.trim()) {
        const friendIds = friends.map(f => f.FriendId || f.friendId);
        const availableUsers = (users || []).filter(
          u => u.Id !== currentUser?.Id && !friendIds.includes(u.Id)
        );
        const shuffled = [...availableUsers].sort(() => Math.random() - 0.5);
        setSuggestedUsers(shuffled.slice(0, 3));
      }
    } catch (err) {
      console.error('Error loading all users:', err);
    }
  };

  useEffect(() => {
    const loadData = async () => {
      try {
        console.log('[HomePage] 🏠 Starting to load data...');
        const userDataJson = localStorage.getItem('user');
        if (!userDataJson) {
          console.error('[HomePage] ❌ No user data in localStorage');
          throw new Error('User data not found. Please login again.');
        }

        const userData = JSON.parse(userDataJson);
        console.log('[HomePage] ✅ User data loaded:', userData);
        setCurrentUser(userData);
        
        console.log('[HomePage] 📢 Loading notifications...');
        await loadNotifications(userData);
        
        // Debug logging for avatar
        console.log('HomePage - currentUser loaded:', {
          id: userData.Id,
          name: userData.UserName,
          ProfilePictureUrl: userData.ProfilePictureUrl,
          profilePictureUrl: userData.profilePictureUrl
        });

        console.log('[HomePage] 📝 Loading posts, friends, requests, stories...');
        const loadPostsPromise = loadPosts(userData);
        const [friendsData, requestsData, storyData] = await Promise.all([
          getAcceptedFriends(userData.Id, 1, 10),
          getPendingRequests(userData.Id, 1, 20),
          getStories()
        ]);

        console.log('Friends data from API:', friendsData);
        setFriends(friendsData || []);

        setPendingRequests(requestsData || []);

        const activeStories = (storyData || []).filter(story => {
          return !story.ExpireAt || new Date(story.ExpireAt) > new Date();
        });
        setStories(activeStories);

        await loadFriendAndRequesterInfo(friendsData || [], requestsData || []);
        await loadPostsPromise;
        
        console.log('[HomePage] ✅ All data loaded successfully');
      } catch (err) {
        console.error('Error loading data:', err);
        setError(err.message || 'Failed to load data');
      } finally {
        setLoading(false);
      }
    };

    loadData();

    // Listen for user data updates from other components (e.g., profile page)
    const handleUserUpdate = async () => {
      const userDataJson = localStorage.getItem('user');
      if (userDataJson) {
        const userData = JSON.parse(userDataJson);
        setCurrentUser(userData);
        // Reload posts to reflect updated user name in post author info
        console.log('User updated - reloading posts with new user name');
        await loadPosts(userData);
      }
    };

    // Listen for new friend request from UserProfilePage
    const handleFriendRequestSent = async () => {
      const userDataJson = localStorage.getItem('user');
      const latestUser = userDataJson ? JSON.parse(userDataJson) : null;
      if (!latestUser?.Id) return;
      console.log('Friend request sent - reloading pending requests');
      const requestsData = await getPendingRequests(latestUser.Id, 1, 20);
      setPendingRequests(requestsData || []);
      // Clear requesters info cache to reload user info
      setRequestersInfo({});
    };

    // Listen for friend acceptance - reload friends list
    const handleFriendAccepted = async () => {
      const userDataJson = localStorage.getItem('user');
      const latestUser = userDataJson ? JSON.parse(userDataJson) : null;
      if (!latestUser?.Id) return;
      console.log('Friend accepted - reloading friends list');
      const friendsData = await getAcceptedFriends(latestUser.Id, 1, 10);
      setFriends(friendsData || []);
      // Clear friends info cache to reload user info
      setFriendsInfo({});
    };

    window.addEventListener('userUpdated', handleUserUpdate);
    window.addEventListener('friendRequestSent', handleFriendRequestSent);
    window.addEventListener('friendAccepted', handleFriendAccepted);

    return () => {
      window.removeEventListener('userUpdated', handleUserUpdate);
      window.removeEventListener('friendRequestSent', handleFriendRequestSent);
      window.removeEventListener('friendAccepted', handleFriendAccepted);
    };
  }, []);

  useEffect(() => {
    if (!currentUser?.Id) return;

    const friendIdSet = new Set(
      (friends || []).map((f) => f.FriendId || f.friendId).filter(Boolean).map(String)
    );

    const onStoryCreated = (e) => {
      const s = normalizeStoryPayload(e.detail);
      if (!s || !isStoryActive(s)) return;
      const uid = String(s.UserId);
      if (uid !== String(currentUser.Id) && !friendIdSet.has(uid)) return;
      setStories((prev) => {
        if (prev.some((x) => (x.Id ?? x.id) === s.Id)) return prev;
        return [s, ...prev];
      });
    };

    const onStoryDeleted = (e) => {
      const d = e.detail;
      const sid = d?.storyId ?? d?.StoryId;
      if (sid == null) return;
      setStories((prev) => prev.filter((st) => (st.Id ?? st.id) !== sid));
    };

    window.addEventListener('signalr:story-created', onStoryCreated);
    window.addEventListener('signalr:story-deleted', onStoryDeleted);
    return () => {
      window.removeEventListener('signalr:story-created', onStoryCreated);
      window.removeEventListener('signalr:story-deleted', onStoryDeleted);
    };
  }, [currentUser, friends]);

  useEffect(() => {
    if (!currentUser?.Id) return;

    const visibleUserIds = new Set(
      (friends || []).map((f) => String(f.FriendId || f.friendId)).filter(Boolean)
    );
    visibleUserIds.add(String(currentUser.Id));

    const onPostCreated = (e) => {
      const post = normalizeFeedPostFromRealtime(e.detail);
      if (!post || post.GroupId != null) return;
      if (!visibleUserIds.has(String(post.UserId))) return;

      setPosts((prev) => {
        if (prev.some((p) => p.Id === post.Id)) return prev;
        return [post, ...prev];
      });

      const liked = post.LikedByUserIds || [];
      if (liked.includes(currentUser.Id)) {
        setLikedPosts((prev) => new Set(prev).add(post.Id));
      }
    };

    window.addEventListener('signalr:post-created', onPostCreated);
    return () => window.removeEventListener('signalr:post-created', onPostCreated);
  }, [currentUser, friends]);

  useEffect(() => {
    const onRealtimeNotification = (e) => {
      const notification = e.detail;
      if (!notification) return;
      if (isMessageNotificationType(notification.Type)) return;
      const nid = notification.Id ?? notification.id;
      setNotifications((prev) =>
        [notification, ...prev.filter((item) => (item.Id ?? item.id) !== nid)]
      );
    };

    const onMessageUnread = () => {
      setUnreadMessageCount((prev) => prev + 1);
    };

    const onUserProfileUpdated = (e) => {
      const updatedUser = e.detail;
      if (!updatedUser || !updatedUser.Id) return;
      
      console.log('[HomePage] 🔄 User profile updated:', updatedUser);
      
      // Update friends info if this user is in our friends list
      setFriendsInfo((prev) => ({
        ...prev,
        [updatedUser.Id]: updatedUser
      }));
      
      // Update requesters info if this user is in our pending requests
      setRequestersInfo((prev) => ({
        ...prev,
        [updatedUser.Id]: updatedUser
      }));
      
      // Update suggested users if this user is in the list
      setAllUsers((prev) =>
        prev.map((u) => u.Id === updatedUser.Id ? updatedUser : u)
      );
      
      // Update current user if it's us
      if (currentUser && currentUser.Id === updatedUser.Id) {
        const updatedCurrentUser = { ...currentUser, ...updatedUser };
        setCurrentUser(updatedCurrentUser);
        localStorage.setItem('user', JSON.stringify(updatedCurrentUser));
      }
      
      // Update posts that have this user as author
      setPosts((prev) =>
        prev.map((post) => {
          if (post.UserId === updatedUser.Id) {
            return {
              ...post,
              UserFullName: updatedUser.FullName || updatedUser.UserName,
              UserName: updatedUser.UserName,
              UserProfilePictureUrl: updatedUser.ProfilePictureUrl
            };
          }
          if (post.SharedPost?.UserId === updatedUser.Id) {
            return {
              ...post,
              SharedPost: {
                ...post.SharedPost,
                UserFullName: updatedUser.FullName || updatedUser.UserName,
                UserName: updatedUser.UserName,
                UserProfilePictureUrl: updatedUser.ProfilePictureUrl
              }
            };
          }
          return post;
        })
      );
    };

    const onFriendRequestReceived = async (e) => {
      const friendRequest = e.detail;
      if (!friendRequest || !friendRequest.Id) return;
      
      console.log('[HomePage] 📬 Friend request received:', friendRequest);
      
      // Reload pending requests
      if (!currentUser?.Id) return;
      try {
        const requestsData = await getPendingRequests(currentUser.Id, 1, 20);
        setPendingRequests(requestsData || []);
        // Clear cache to reload user info
        setRequestersInfo({});
      } catch (err) {
        console.error('[HomePage] Error reloading pending requests:', err);
      }
    };

    const onFriendRequestAccepted = async (e) => {
      const friendRequest = e.detail;
      if (!friendRequest || !friendRequest.UserId) return;
      
      console.log('[HomePage] ✅ Friend request accepted:', friendRequest);
      
      // Reload friends list since we're now friends with this user
      if (!currentUser?.Id) return;
      try {
        const friendsData = await getAcceptedFriends(currentUser.Id, 1, 10);
        setFriends(friendsData || []);
        setFriendsInfo({});
      } catch (err) {
        console.error('[HomePage] Error reloading friends:', err);
      }
    };

    window.addEventListener('signalr:notification', onRealtimeNotification);
    window.addEventListener('signalr:message-unread', onMessageUnread);
    window.addEventListener('signalr:user-profile-updated', onUserProfileUpdated);
    window.addEventListener('signalr:friend-request-received', onFriendRequestReceived);
    window.addEventListener('signalr:friend-request-accepted', onFriendRequestAccepted);
    
    return () => {
      window.removeEventListener('signalr:notification', onRealtimeNotification);
      window.removeEventListener('signalr:message-unread', onMessageUnread);
      window.removeEventListener('signalr:user-profile-updated', onUserProfileUpdated);
      window.removeEventListener('signalr:friend-request-received', onFriendRequestReceived);
      window.removeEventListener('signalr:friend-request-accepted', onFriendRequestAccepted);
    };
  }, [currentUser]);

  useEffect(() => {
    const loadInfoForFriends = async () => {
      const friendIds = friends
        .map(f => f.FriendId || f.friendId)
        .filter(Boolean);
      const requesterIds = pendingRequests
        .map(r => r.UserId)
        .filter(Boolean);

      const idsToLoad = [...new Set([...friendIds, ...requesterIds])]
        .filter(id => !friendsInfo[id] && !requestersInfo[id]);

      if (idsToLoad.length === 0) {
        return;
      }

      try {
        const users = await getUsersByIds(idsToLoad);

        const newFriendsInfo = {};
        const newRequestersInfo = {};

        users.forEach((user) => {
          if (!user) return;
          if (friendIds.includes(user.Id)) {
            newFriendsInfo[user.Id] = user;
          }
          if (requesterIds.includes(user.Id)) {
            newRequestersInfo[user.Id] = user;
          }
        });

        if (Object.keys(newFriendsInfo).length > 0) {
          setFriendsInfo((prev) => ({ ...prev, ...newFriendsInfo }));
        }
        if (Object.keys(newRequestersInfo).length > 0) {
          setRequestersInfo((prev) => ({ ...prev, ...newRequestersInfo }));
        }
      } catch (err) {
        console.error('Error loading friend/requester info:', err);
      }
    };

    loadInfoForFriends();
  }, [friends, pendingRequests]);

  useEffect(() => {
    if (selectedNav !== 'add-friends') return;

    const loadUsers = async () => {
      await fetchAllUsers(searchQuery.trim());
    };

    loadUsers();
  }, [selectedNav, searchQuery]);

  const handleFileChange = async (e) => {
    const file = e.target.files?.[0];
    if (!file) return;

    // Validate file
    const maxSize = 5 * 1024 * 1024; // 5MB
    const validTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
    
    if (file.size > maxSize) {
      setError('Kích thước hình ảnh không được vượt quá 5MB');
      return;
    }
    
    if (!validTypes.includes(file.type)) {
      setError('Chỉ hỗ trợ các định dạng: JPEG, PNG, GIF, WebP');
      return;
    }

    // Read file as Base64
    const reader = new FileReader();
    reader.onload = () => {
      setNewPostFile(file);
      setPostImagePreview(reader.result);
      setError('');
    };
    reader.onerror = () => {
      setError('Lỗi đọc tệp hình ảnh');
    };
    reader.readAsDataURL(file);
  };

  const handleCreatePost = async (e) => {
    e.preventDefault();
    if (!newPostContent.trim() && !newPostFile) {
      setError('Vui lòng nhập nội dung hoặc chọn hình ảnh');
      return;
    }

    setPosting(true);
    try {
      let imageBase64 = null;
      if (newPostFile) {
        imageBase64 = await resizeImage(newPostFile, 1024, 0.8);
      }

      const newPost = await createPost({
        content: newPostContent,
        imageUrl: imageBase64
      });
      
      console.log('Post created successfully:', newPost);
      
      setNewPostContent('');
      setNewPostFile(null);
      setPostImagePreview('');
      
      // Add new post to the beginning of the list
      if (newPost) {
        setPosts([newPost, ...posts]);
      }
    } catch (err) {
      setError(err.message);
      console.error('Error creating post:', err);
    } finally {
      setPosting(false);
    }
  };

  const handleStoryFileChange = (e) => {
    const file = e.target.files?.[0];
    if (file) {
      setNewStoryFile(file);
      const reader = new FileReader();
      reader.onload = (event) => {
        setStoryImagePreview(event.target?.result || '');
      };
      reader.onerror = () => {
        setError('Lỗi đọc tệp hình ảnh');
      };
      reader.readAsDataURL(file);
    }
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
        imageBase64 = await resizeImage(newStoryFile, 1024, 0.8);
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

      const storyWithUser = {
        ...createdStory,
        UserName: currentUser?.FullName || currentUser?.UserName || 'Bạn',
        UserProfilePictureUrl: currentUser?.ProfilePictureUrl || ''
      };

      setStories([storyWithUser, ...stories]);
      setNewStoryContent('');
      setNewStoryFile(null);
      setStoryImagePreview('');
      setShowCreateStoryModal(false);
      setError('');
      navigate(`/story/user/${createdStory.UserId}`);
    } catch (err) {
      setError(err.message);
      console.error('Error creating story:', err);
    } finally {
      setCreatingStory(false);
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.dispatchEvent(new Event('tokenUpdated'));
    navigate('/login');
  };

  const extractHashtag = (query) => {
    if (!query) return null;
    const match = query.match(/#?([a-zA-Z0-9_]+)/);
    return match?.[1] ? `#${match[1]}` : null;
  };

  const handleHomeSearch = (query) => {
    if (!query.trim()) {
      // Clear search
      setSearchKeyword('');
      setHashtagSearch(null);
      navigate('/home');
      return;
    }

    const hashtag = extractHashtag(query);
    if (hashtag && query.startsWith('#')) {
      // Hashtag search
      setSearchKeyword('');
      setHashtagSearch(hashtag);
      navigate(`/home?hashtag=${encodeURIComponent(hashtag.slice(1))}`);
    } else {
      // Keyword search
      setHashtagSearch(null);
      setSearchKeyword(query);
    }
  };

  useEffect(() => {
    const queryParams = new URLSearchParams(location.search);
    const hashtagParam = queryParams.get('hashtag');
    if (hashtagParam) {
      const hashtag = extractHashtag(hashtagParam);
      setHashtagSearch(hashtag);
      setSearchKeyword('');
    } else {
      setHashtagSearch(null);
    }
  }, [location.search]);

  const handleLike = async (post) => {
    if (!currentUser) {
      console.warn('No current user');
      return;
    }
    
    try {
      const isLiked = likedPosts.has(post.Id);
      console.log('Like status:', { postId: post.Id, userId: currentUser.Id, isLiked });
      
      if (isLiked) {
        // Unlike
        await unlikePost(post.Id, currentUser.Id);
        setLikedPosts(prev => {
          const newSet = new Set(prev);
          newSet.delete(post.Id);
          return newSet;
        });
      } else {
        // Like
        await likePost(post.Id, currentUser.Id);
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
              // IMPORTANT: Preserve SharedPost structure if this is a shared post
              return {
                ...updatedPost,
                SharedPost: p.SharedPost // Keep original SharedPost if exists
              };
            }
            // If this post shares the liked/unliked post, update the shared post data
            if (p.SharedPost?.Id === post.Id) {
              return {
                ...p,
                SharedPost: {
                  ...p.SharedPost,
                  ...updatedPost, // Merge new data
                  LikesCount: updatedPost.LikesCount ?? p.SharedPost.LikesCount
                }
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

  const handleAddComment = async (postId, content) => {
    try {
      const createdComment = await createComment(postId, content);
      setCommentsByPost((prev) => ({
        ...prev,
        [postId]: mergeCommentIntoList(prev[postId] || [], createdComment, { prepend: true })
      }));
      
      // Update comments count locally for immediate UI update
      setPosts((prev) => 
        prev.map((post) => {
          if (post.Id === postId) {
            return {
              ...post,
              CommentsCount: (post.CommentsCount || 0) + 1
            };
          }
          // If this post has a shared post that matches postId, update it too
          if (post.SharedPost?.Id === postId) {
            return {
              ...post,
              SharedPost: {
                ...post.SharedPost,
                CommentsCount: (post.SharedPost.CommentsCount || 0) + 1
              }
            };
          }
          return post;
        })
      );
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
        [postId]: (prev[postId] || []).filter((c) => !sameCommentId(c.id ?? c.Id, commentId))
      }));
      
      // Update comments count locally for immediate UI update
      setPosts((prev) => 
        prev.map((post) => {
          if (post.Id === postId) {
            return {
              ...post,
              CommentsCount: Math.max(0, (post.CommentsCount || 0) - 1)
            };
          }
          // If this post has a shared post that matches postId, update it too
          if (post.SharedPost?.Id === postId) {
            return {
              ...post,
              SharedPost: {
                ...post.SharedPost,
                CommentsCount: Math.max(0, (post.SharedPost.CommentsCount || 0) - 1)
              }
            };
          }
          return post;
        })
      );
    } catch (err) {
      console.error('Error deleting comment:', err);
    }
  };

  const handleEditComment = async (postId, commentId, newContent) => {
    try {
      await updateComment(commentId, newContent);
      setCommentsByPost((prev) => ({
        ...prev,
        [postId]: (prev[postId] || []).map((comment) =>
          sameCommentId(comment.id ?? comment.Id, commentId)
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

  const handleShare = (post) => {
    setSharePostId(post.Id);
    setShareCaption('');
    setShowShareModal(true);
  };

  const handleSubmitShare = async () => {
    if (!sharePostId) return;

    try {
      setSharePending(true);
      const newSharedPost = await sharePost(sharePostId, {
        content: shareCaption || ''
      });

      if (newSharedPost) {
        setPosts((prev) =>
          prev.some((p) => p.Id === newSharedPost.Id) ? prev : [newSharedPost, ...prev]
        );
        setError('');
        
        // Trigger event to notify ProfilePage to reload posts
        window.dispatchEvent(new Event('postShared'));
      }
      
      // Close modal
      setShowShareModal(false);
      setSharePostId(null);
      setShareCaption('');
    } catch (err) {
      console.error('Error sharing post:', err);
      setError(`Lỗi chia sẻ bài viết: ${err.message}`);
    } finally {
      setSharePending(false);
    }
  };

  const handleAcceptRequest = async (request) => {
    try {
      await acceptFriendRequest(request.Id);
      // Remove from pending requests
      setPendingRequests(pendingRequests.filter(r => r.Id !== request.Id));
      // Reload friends list
      const friendsData = await getAcceptedFriends(currentUser.Id, 1, 10);
      setFriends(friendsData || []);
      
      // 🔄 Preload new friend's info instead of clearing cache
      if (friendsData && friendsData.length > 0) {
        // Find newly accepted friend ID
        const newlyAcceptedFriendId = friendsData[0]?.FriendId || friendsData[0]?.friendId;
        
        if (newlyAcceptedFriendId) {
          try {
            const newFriendData = await getUser(newlyAcceptedFriendId);
            if (newFriendData) {
              setFriendsInfo((prev) => ({
                ...prev,
                [newlyAcceptedFriendId]: newFriendData
              }));
              console.log(`[HomePage] ✅ Preloaded new friend info: ${newlyAcceptedFriendId}`);
            }
          } catch (err) {
            console.error('[HomePage] Error preloading new friend info:', err);
          }
        }
      }
      
      setError('');
      
      // Emit event to notify other components
      window.dispatchEvent(new Event('friendAccepted'));
    } catch (err) {
      console.error('Error accepting friend request:', err);
      setError(`Lỗi chấp nhận lời mời: ${err.message}`);
    }
  };

  const handleDeclineRequest = async (request) => {
    if (!window.confirm('Bạn có chắc chắn muốn từ chối lời mời này?')) {
      return;
    }

    try {
      await declineFriendRequest(request.Id);
      setPendingRequests(pendingRequests.filter(r => r.Id !== request.Id));
      setError('');
      
      // Emit event to notify other components
      window.dispatchEvent(new Event('friendDeclined'));
    } catch (err) {
      console.error('Error declining friend request:', err);
      setError(`Lỗi từ chối lời mời: ${err.message}`);
    }
  };

  const handleHashtagClick = (hashtag) => {
    const slug = hashtag.startsWith('#') ? hashtag.slice(1) : hashtag;
    // Clear keyword search and navigate to hashtag
    setSearchKeyword('');
    navigate(`/home?hashtag=${encodeURIComponent(slug)}`);
  };

  const handleOpenUserProfile = (userId) => {
    if (!userId) return;
    navigate(`/user-profile/${userId}`);
  };

  const handlePostsScroll = (e) => {
    const element = e.target;
    const distanceToBottom = element.scrollHeight - (element.scrollTop + element.clientHeight);
    
    // Load more when user scrolls near the bottom (500px from bottom)
    if (distanceToBottom < 500 && hasMorePosts && !postsLoading && !searchKeyword && !hashtagSearch) {
      console.log('[HomePage] 📜 Loading more posts...');
      loadPosts(currentUser, postPage + 1);
    }
  };

  const hasHashtag = (content = '', tag) => {
    if (!tag) return false;
    const escaped = tag.replace(/[.*+?^${}()|[\\]\\]/g, '\\$&');
    const regex = new RegExp(`(^|\\s)${escaped}(?=\\s|$|[^a-zA-Z0-9_])`, 'i');
    return regex.test(content);
  };

  // Filter posts based on search type
  const displayedPosts = searchKeyword
    ? posts.filter(post => 
        (post.Content || '').toLowerCase().includes(searchKeyword.toLowerCase()) ||
        (post.UserFullName || '').toLowerCase().includes(searchKeyword.toLowerCase())
      )
    : hashtagSearch
    ? posts.filter(post => hasHashtag(post.Content || '', hashtagSearch))
    : posts;

  useEffect(() => {
    let cancelled = false;
    let conn;

    const onPostLiked = (data) => {
      if (!data?.postId) return;
      setPosts((prev) =>
        prev.map((p) => {
          if (p.Id === data.postId) {
            return { ...p, LikesCount: data.likesCount };
          }
          if (p.SharedPost?.Id === data.postId) {
            return {
              ...p,
              SharedPost: { ...p.SharedPost, LikesCount: data.likesCount }
            };
          }
          return p;
        })
      );
    };

    const onPostUnliked = (data) => {
      if (!data?.postId) return;
      setPosts((prev) =>
        prev.map((p) => {
          if (p.Id === data.postId) {
            return { ...p, LikesCount: data.likesCount };
          }
          if (p.SharedPost?.Id === data.postId) {
            return {
              ...p,
              SharedPost: { ...p.SharedPost, LikesCount: data.likesCount }
            };
          }
          return p;
        })
      );
    };

    const applyCommentsCount = (postId, count) => {
      if (postId == null || typeof count !== 'number') return;
      setPosts((prev) =>
        prev.map((p) => {
          if (p.Id === postId) {
            return { ...p, CommentsCount: count };
          }
          if (p.SharedPost?.Id === postId) {
            return {
              ...p,
              SharedPost: { ...p.SharedPost, CommentsCount: count }
            };
          }
          return p;
        })
      );
    };

    const onCommentAdded = (data) => {
      if (!data?.postId) return;
      setCommentsByPost((prev) => {
        const pid = data.postId;
        const list = prev[pid] || [];
        const next = mergeCommentIntoList(list, data.comment, { prepend: true });
        if (next === list) return prev;
        return { ...prev, [pid]: next };
      });
      if (typeof data.commentsCount === 'number') {
        applyCommentsCount(data.postId, data.commentsCount);
      }
    };

    const onCommentUpdated = (data) => {
      if (data?.postId == null || data.commentId == null) return;
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
      if (!data?.postId) return;
      const cid = data.commentId;
      setCommentsByPost((prev) => ({
        ...prev,
        [data.postId]: (prev[data.postId] || []).filter(
          (c) => !sameCommentId(c.Id ?? c.id, cid)
        )
      }));
      if (typeof data.commentsCount === 'number') {
        applyCommentsCount(data.postId, data.commentsCount);
      }
    };

    (async () => {
      const connection = await startConnection();
      if (cancelled || !connection) return;
      conn = connection;
      connection.on('PostLiked', onPostLiked);
      connection.on('PostUnliked', onPostUnliked);
      connection.on('CommentAdded', onCommentAdded);
      connection.on('CommentUpdated', onCommentUpdated);
      connection.on('CommentDeleted', onCommentDeleted);
    })();

    return () => {
      cancelled = true;
      if (!conn) return;
      conn.off('PostLiked', onPostLiked);
      conn.off('PostUnliked', onPostUnliked);
      conn.off('CommentAdded', onCommentAdded);
      conn.off('CommentUpdated', onCommentUpdated);
      conn.off('CommentDeleted', onCommentDeleted);
    };
  }, []);

  if (loading) {
    return <div className="home-container"><p>Đang tải...</p></div>;
  }

  // Debug: log all users
  console.log('=== DEBUG: All Users ===');
  console.log('Total users:', allUsers.length);
  console.log('All users:', allUsers);
  console.log('Current user:', currentUser);
  console.log('Friends:', friends);

  const filteredUsers = searchQuery.trim() ?
    allUsers.filter(u => {
      const userText = `${u.FullName || u.fullName || ''} ${u.UserName || u.userName || ''}`;
      return (
        u.Id !== currentUser?.Id &&
        !friends.some(f => {
          const fId = f.FriendId || f.friendId;
          return fId === u.Id;
        }) &&
        matchesSearch(userText, searchQuery)
      );
    }) : suggestedUsers;

  console.log('Search query:', searchQuery);
  console.log('Filtered users:', filteredUsers);

  return (
    <div className="home-wrapper">
      <Header onLogout={handleLogout} onSearch={handleHomeSearch} searchValue={searchKeyword || hashtagSearch || ''} unreadMessageCount={unreadMessageCount} />
      <div className="home-container">
        {/* Left Sidebar - Show back button during search (keyword or hashtag) */}
        {searchKeyword || hashtagSearch ? (
          <aside className="sidebar-left">
            <button 
              className="back-to-home-btn"
              onClick={() => {
                setSearchKeyword('');
                setHashtagSearch(null);
                navigate('/home');
              }}
              style={{
                width: '100%',
                padding: '12px 16px',
                marginBottom: '16px',
                backgroundColor: '#007bff',
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
              <span>Quay về trang chủ</span>
            </button>
          </aside>
        ) : (
          <aside className="sidebar-left">
            <nav className="sidebar-nav">
              <div className={`sidebar-notification ${selectedNav === 'notifications' ? 'active' : ''}`} onClick={() => handleSelectNav('notifications')}>
                <span className="nav-icon"><i className="fa-solid fa-bell"></i></span>
                <div className="notification-text">
                  <strong>Thông báo</strong>
                  <p>
                    {feedUnreadNotificationCount > 0
                      ? `${feedUnreadNotificationCount} cập nhật mới`
                      : 'Xem lịch sử thông báo'}
                  </p>
                </div>
              </div>
              <div className={`nav-item ${selectedNav === 'friends' ? 'active' : ''}`} onClick={() => handleSelectNav('friends')}>
                <span className="nav-icon"><i className="fa-solid fa-people-pulling"></i></span>
                <span>Tất cả bạn bè</span>
              </div>
              <div className={`nav-item ${selectedNav === 'requests' ? 'active' : ''}`} onClick={() => handleSelectNav('requests')}>
                <span className="nav-icon"><i className="fa-solid fa-address-book"></i></span>
                <span>Lời mời kết bạn</span>
              </div>
              <div className={`nav-item ${selectedNav === 'add-friends' ? 'active' : ''}`} onClick={() => handleSelectNav('add-friends')}>
                <span className="nav-icon">➕</span>
                <span>Thêm bạn bè</span>
              </div>
            </nav>
          </aside>
        )}

        {/* Main Content */}
        <main className="main-content">
          {error && <div className="error-message">{error}</div>}

          {!searchKeyword && !hashtagSearch && (
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
                Bạn đang nghĩ gì? Hãy chia sẻ cảm nghĩ của bạn đến bạn bè thông qua...
              </p>
            </div>

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
                  <label htmlFor="post-image-input" className="file-input-label">
                    <i className="fa-solid fa-image"></i>
                    <span>Thêm hình ảnh</span>
                  </label>
                  <input
                    id="post-image-input"
                    type="file"
                    accept="image/jpeg,image/png,image/gif,image/webp"
                    onChange={handleFileChange}
                    className="post-file-input"
                  />
                  {newPostFile && (
                    <span className="file-selected">
                      ✓ {newPostFile.name}
                    </span>
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
                  <button
                    type="button"
                    onClick={() => {
                      setNewPostFile(null);
                      setPostImagePreview('');
                    }}
                    className="btn-remove-image"
                  >
                    ✕ Xóa hình ảnh
                  </button>
                </div>
              )}
            </form>
          </section>
          )}


          {/* Stories Section - Always show */}
          {!searchKeyword && !hashtagSearch && (
            <section className="stories-section">
            <div className="stories-carousel">
              {/* Create Story Card */}
              <div 
                className="story-card create-story-card"
                onClick={() => setShowCreateStoryModal(true)}
                style={{ cursor: 'pointer' }}
              >
                <div className="story-create-icon">+</div>
                <p className="story-label">Tạo tin</p>
              </div>

              {/* Friend Stories */}
              {Object.values(
                stories.reduce((acc, story) => {
                  if (!acc[story.UserId]) {
                    acc[story.UserId] = {
                      UserId: story.UserId,
                      UserName: story.UserName,
                      UserProfilePictureUrl: story.UserProfilePictureUrl,
                      stories: []
                    };
                  }
                  acc[story.UserId].stories.push(story);
                  return acc;
                }, {})
              ).map((userStories) => (
                <div
                  key={userStories.UserId}
                  className="story-card story-card-clickable"
                  onClick={() => navigate(`/story/user/${userStories.UserId}`)}
                >
                  <div className="story-background"></div>

                  <div className="story-avatar">
                    {userStories.UserProfilePictureUrl ? (
                      <img src={userStories.UserProfilePictureUrl} alt="Avatar" />
                    ) : (
                      <i className="fa-solid fa-user"></i>
                    )}
                  </div>
                  <p className="story-label">{userStories.UserName || 'Tin mới'}</p>
                </div>
              ))}
            </div>
          </section>
          )}

          <section className="posts-feed" onScroll={handlePostsScroll}>
            {displayedPosts.length === 0 ? (
              <p className="no-posts">
                {hashtagSearch ? `Không có bài viết nào với ${hashtagSearch}.` : 'Chưa có bài viết nào. Hãy tạo bài viết đầu tiên!'}
              </p>
            ) : (
              <>
                {displayedPosts.map((post) => (
                <div key={post.Id} className="post-card">
                  <div className="post-header">
                    <div className="post-user-info post-user-clickable" onClick={() => handleOpenUserProfile(post.UserId || post.userId)}>
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
                    <div className="post-menu-container">
                      <button 
                        className="post-menu-btn" 
                        onClick={() => setActivePostMenuId(activePostMenuId === post.Id ? null : post.Id)}
                      >
                        ⋯
                      </button>
                      {activePostMenuId === post.Id && (
                        <div className="post-menu-dropdown">
                          {currentUser?.Id === post.UserId && (
                            <button 
                              className="menu-item"
                              onClick={() => handleDeletePost(post)}
                            >
                              <i className="fa-solid fa-trash"></i> Xóa bài viết
                            </button>
                          )}
                          {currentUser?.Id !== post.UserId && (
                            <button 
                              className="menu-item menu-item-report"
                              onClick={() => {
                                setReportPostId(post.Id);
                                setShowReportModal(true);
                                setActivePostMenuId(null);
                              }}
                            >
                              <i className="fa-solid fa-flag"></i> Báo cáo bài viết
                            </button>
                          )}
                        </div>
                      )}
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
                      <img src={post.ImageUrl} alt="Post" className="post-image" loading="lazy" />
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
                              loading="lazy"
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
                    <button className="post-action-btn" onClick={() => handleShare(post)}>
                      <span><i className="fa-solid fa-share"></i></span> Chia sẻ
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
              ))}
                <div ref={postsEndRef} />
                {postsLoading && <div className="loading-indicator">Đang tải bài viết...</div>}
              </>
            )}
          </section>
        </main>

        {/* Right Sidebar - Hidden during keyword search */}
        {!searchKeyword && (
          <aside className="sidebar-right">
          {selectedNav === 'add-friends' && hashtagSearch ? (
            <>
              <h3 className="sidebar-title">
                Bài viết có #{hashtagSearch.slice(1)}
              </h3>
              <button 
                className="btn-clear-hashtag"
                onClick={() => {
                  setHashtagSearch(null);
                  setSearchQuery('');
                }}
              >
                ✕ Xóa tìm kiếm
              </button>
              <div className="hashtag-posts">
                {posts.length > 0 ? (
                  <>
                    {posts.filter(post => post.Content.includes(hashtagSearch)).map(post => (
                      <div key={post.Id} className="hashtag-post-item">
                        <p className="hashtag-post-author">{post.UserFullName || post.UserName}</p>
                        <p className="hashtag-post-content">{post.Content.substring(0, 100)}...</p>
                      </div>
                    )).length > 0 ? (
                      posts.filter(post => post.Content.includes(hashtagSearch)).map(post => (
                        <div key={post.Id} className="hashtag-post-item">
                          <p className="hashtag-post-author">{post.UserFullName || post.UserName}</p>
                          <p className="hashtag-post-content">{post.Content.substring(0, 100)}...</p>
                        </div>
                      ))
                    ) : (
                      <p className="no-results">Không có bài viết với hashtag này</p>
                    )}
                  </>
                ) : (
                  <p className="no-results">Không có bài viết với hashtag này</p>
                )}
              </div>
            </>
          ) : selectedNav === 'notifications' ? (
            <>
              <h3 className="sidebar-title">Thông báo</h3>
              <div className="notifications-list">
                {notifications.length === 0 ? (
                  <p className="no-results">Hiện chưa có thông báo</p>
                ) : (
                  notifications.map((item) => {
                    const isModerationNotice = item.Content && (item.Content.includes('bị gỡ') || item.Content.includes('Kháng cáo') || item.Content.includes('kháng cáo'));
                    const isPostNotification = isPostRelatedNotification(item);

                    return (
                      <div
                        key={item.Id || item.id}
                        className={`notification-item ${isPostNotification ? 'clickable-notification' : ''}`}
                        onClick={() => {
                          if (isPostNotification) {
                            navigate(`/post/${item.RelatedEntityId}`);
                          }
                        }}
                      >
                        <p className="notification-title">{item.Content || item.title}</p>
                        <p className="notification-time">{formatTimeAgo(item.CreatedAt || item.createdAt) || item.TimeAgo || item.time}</p>
                        {isModerationNotice && item.RelatedEntityId && (
                          <div style={{ marginTop: '8px' }}>
                            <button
                              type="button"
                              onClick={(e) => {
                                e.stopPropagation();
                                setAppealingReportId(item.RelatedEntityId);
                              }}
                              style={{
                                padding: '5px 12px',
                                fontSize: '12px',
                                borderRadius: '6px',
                                border: '1px solid #e67e22',
                                background: '#fffaf0',
                                color: '#d35400',
                                cursor: 'pointer',
                                fontWeight: '600',
                                display: 'inline-flex',
                                alignItems: 'center',
                                gap: '5px'
                              }}
                            >
                              ⚖️ {item.Content.includes('bị gỡ') ? 'Gửi đơn kháng cáo' : 'Xem chi tiết kháng cáo'}
                            </button>
                          </div>
                        )}
                      </div>
                    );
                  })
                )}
              </div>
            </>
          ) : selectedNav === 'add-friends' ? (
            <>
              <h3 className="sidebar-title">Thêm bạn bè</h3>
              <div className="add-friends-container">
                <div className="search-wrapper">
                  <input
                    type="text"
                    placeholder="Tìm bạn bè..."
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    className="add-friends-search"
                  />
                  <span className="search-icon"><i class="fa-solid fa-magnifying-glass"></i></span>
                </div>
                
                <div className="search-results">
                  {filteredUsers.length > 0 ? (
                    <>
                      {searchQuery.trim() === '' && <h4 className="suggestions-title">Gợi ý bạn bè</h4>}
                      {filteredUsers.map(user => (
                        <div 
                          key={user.Id || user.id} 
                          className="suggested-friend-card"
                          onClick={() => navigate(`/user-profile/${user.Id || user.id}`)}
                          style={{ cursor: 'pointer' }}
                        >
                          <div className="friend-avatar">
                            {user.ProfilePictureUrl || user.profilePictureUrl ? (
                              <img 
                                src={user.ProfilePictureUrl || user.profilePictureUrl} 
                                alt={user.FullName || user.fullName}
                                style={{ width: '100%', height: '100%', objectFit: 'cover', borderRadius: '50%' }}
                              />
                            ) : (
                              <i className="fa-solid fa-user"></i>
                            )}
                          </div>
                          <p className="friend-name">{user.FullName || user.fullName}</p>
                          <button 
                            className="btn-add-friend"
                            onClick={(e) => {
                              e.stopPropagation();
                              navigate(`/user-profile/${user.Id || user.id}`);
                            }}
                          >
                            Xem hồ sơ
                          </button>
                        </div>
                      ))}
                    </>
                  ) : (
                    <p className="no-results">
                      {searchQuery.trim() ? 'Không tìm thấy người dùng' : 'Không có gợi ý bạn bè'}
                    </p>
                  )}
                </div>
              </div>
            </>
          ) : selectedNav === 'requests' ? (
            <>
              <h3 className="sidebar-title">Lời mời kết bạn</h3>
              <div className="friends-list">
                {pendingRequests.length === 0 ? (
                  <p className="no-friends">Hiện chưa có lời mời kết bạn</p>
                ) : (
                  pendingRequests.map((request) => {
                    const requesterInfo = requestersInfo[request.UserId];

                    return (
                      <div key={request.Id} className="friend-request-item">
                        <div className="request-header">
                          <div className="friend-avatar-small">
                            {requesterInfo?.ProfilePictureUrl || requesterInfo?.profilePictureUrl ? (
                              <img 
                                src={requesterInfo.ProfilePictureUrl || requesterInfo.profilePictureUrl} 
                                alt={requesterInfo?.FullName || requesterInfo?.fullName}
                                style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                              />
                            ) : (
                              <i className="fa-solid fa-user"></i>
                            )}
                          </div>
                          <p 
                            className="friend-name-small"
                            onClick={() => navigate(`/user-profile/${request.UserId}`)}
                            style={{ cursor: 'pointer' }}
                          >
                            {requesterInfo?.FullName || requesterInfo?.fullName || 'Người dùng'}
                          </p>
                        </div>
                        <div className="request-actions">
                          <button 
                            className="btn-accept"
                            onClick={() => handleAcceptRequest(request)}
                          >
                            Xác nhận
                          </button>
                          <button 
                            className="btn-reject"
                            onClick={() => handleDeclineRequest(request)}
                          >
                            Xóa bỏ
                          </button>
                        </div>
                      </div>
                    );
                  })
                )}
              </div>
            </>
          ) : (
            <>
              <h3 className="sidebar-title">Danh sách bạn bè</h3>
              <div className="friends-list">
                {friends.length === 0 ? (
                  <p className="no-friends">Chưa có bạn bè</p>
                ) : (
                  friends.map((friend) => {
                    const friendId = friend.FriendId || friend.friendId;
                    const friendInfo = friendsInfo[friendId];

                    return (
                      <div 
                        key={friend.Id || friendId} 
                        className="friend-item"
                        onClick={() => navigate(`/user-profile/${friendId}`)}
                        style={{ cursor: 'pointer' }}
                      >
                        <div className="friend-avatar-small">
                          {friendInfo?.ProfilePictureUrl || friendInfo?.profilePictureUrl ? (
                            <img 
                              src={friendInfo.ProfilePictureUrl || friendInfo.profilePictureUrl} 
                              alt={friendInfo?.FullName || friendInfo?.fullName}
                              style={{ width: '100%', height: '100%', objectFit: 'cover' }}
                            />
                          ) : (
                            <i className="fa-solid fa-user"></i>
                          )}
                        </div>
                        <p className="friend-name-small">{friendInfo?.FullName || friendInfo?.fullName || 'Đang tải...'}</p>
                      </div>
                    );
                  })
                )}
              </div>
            </>
          )}
        </aside>
)}
      </div>

      {/* Create Story Modal */}
      {showCreateStoryModal && (
        <div className="modal-overlay" onClick={() => setShowCreateStoryModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <div className="modal-header">
              <h2>Tạo tin</h2>
              <button 
                className="modal-close-btn"
                onClick={() => setShowCreateStoryModal(false)}
              >
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
                        <span className="file-selected">
                          ✓ {newStoryFile.name}
                        </span>
                      )}
                    </div>
                    <button 
                      type="submit" 
                      className="btn-post"
                      disabled={creatingStory}
                    >
                      {creatingStory ? 'Đang đăng...' : 'Đăng tin'}
                    </button>
                  </div>
                </div>
              </div>
            </form>
          </div>
        </div>
      )}

      {showShareModal && (
        <div className="modal-overlay" onClick={() => setShowShareModal(false)}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()} style={{ maxWidth: '600px' }}>
            <div className="modal-header">
              <h2>Chia sẻ bài viết</h2>
              <button 
                className="modal-close-btn"
                onClick={() => setShowShareModal(false)}
              >
                ✕
              </button>
            </div>

            <div className="share-modal-body" style={{ padding: '20px' }}>
              <textarea
                value={shareCaption}
                onChange={(e) => setShareCaption(e.target.value)}
                placeholder="Thêm chú thích cho bài viết của bạn..."
                className="post-textarea"
                rows="4"
                style={{ marginBottom: '20px', width: '100%', padding: '10px', borderRadius: '8px', border: '1px solid #ccc' }}
              />

              <div style={{ display: 'flex', gap: '10px' }}>
                <button 
                  type="button"
                  className="btn-post"
                  onClick={handleSubmitShare}
                  disabled={sharePending}
                  style={{ flex: 1 }}
                >
                  {sharePending ? 'Đang chia sẻ...' : 'Chia sẻ'}
                </button>
                <button 
                  type="button"
                  className="btn-post"
                  onClick={() => setShowShareModal(false)}
                  style={{ flex: 1, backgroundColor: '#ccc', color: '#333' }}
                >
                  Hủy
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      <ReportPostModal 
        open={showReportModal} 
        postId={reportPostId}
        onClose={() => {
          setShowReportModal(false);
          setReportPostId(null);
        }}
        onSuccess={() => {
          setShowReportModal(false);
          setReportPostId(null);
        }}
      />

      {appealingReportId && (
        <UserAppealModal 
          reportId={appealingReportId}
          onClose={() => setAppealingReportId(null)}
          onSuccess={() => {
            if (currentUser) loadNotifications(currentUser);
          }}
        />
      )}
    </div>
  );
}
