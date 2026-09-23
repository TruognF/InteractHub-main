import { shapeComment, sortCommentsNewestFirst } from './utils/commentNormalize';
import { toIsoUtcString } from './utils/commentDateTime';

const API_BASE = '/api';

// Helper function to handle API responses and errors
async function handleResponse(response) {
  // Check if response has content
  const contentType = response.headers.get('content-type');
  let data = null;

  if (contentType && contentType.includes('application/json')) {
    try {
      data = await response.json();
    } catch (err) {
      console.error('Error parsing JSON response:', err);
      console.error('Response status:', response.status);
      console.error('Response statusText:', response.statusText);
      throw new Error('Invalid response format from server');
    }
  } else {
    console.warn('Response is not JSON, content-type:', contentType);
    data = {};
  }

  if (!response.ok) {
    // Try to get the error message from the response in various formats
    const errorMessage = data?.Message || data?.message || data?.error || `HTTP ${response.status}: ${response.statusText}`;
    console.error('API Error:', {
      status: response.status,
      message: errorMessage,
      data: data
    });
    throw new Error(errorMessage);
  }

  return data;
}

export async function login({ userName, password }) {
  const response = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userName, password })
  });
  
  const data = await handleResponse(response);
  
  // Debug logging
  console.log('Login response:', data);
  console.log('Login data.Data:', data?.Data);
  console.log('Login data.Data.User:', data?.Data?.User);
  console.log('Login data.Data.User.Id:', data?.Data?.User?.Id);
  
  // Check if data structure is correct - handle both PascalCase (from backend) and camelCase
  if (!data || !data.Data) {
    console.error('Invalid response structure:', data);
    throw new Error('Invalid response from server - missing Data object');
  }

  // Backend uses PascalCase (PropertyNamingPolicy = null in Program.cs)
  const token = data.Data.Token || data.Data.token;
  const user = data.Data.User || data.Data.user;

  if (!token || !user) {
    console.error('Missing token or user in response:', data.Data);
    console.error('Token:', token);
    console.error('User:', user);
    throw new Error('Invalid response from server - missing Token or User');
  }

  console.log('Final user object to be returned:', user);
  console.log('Final user.Id:', user.Id);
  
  return { token, user };
}

export async function register({ userName, email, fullName, password }) {
  const response = await fetch(`${API_BASE}/auth/register`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userName, email, fullName, password })
  });
  
  const data = await handleResponse(response);
  // Backend uses PascalCase (PropertyNamingPolicy = null in Program.cs)
  const token = data?.Data?.Token || data?.Data?.token;
  const user = data?.Data?.User || data?.Data?.user;

  if (!token || !user) {
    throw new Error('Invalid registration response from server - missing Token or User');
  }

  return { token, user };
}

export async function getPostsByGroup(groupId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/posts/group/${groupId}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  
  const data = await handleResponse(response);
  return data?.Data || [];
}

export async function getUserPosts(userId) {
  const token = localStorage.getItem('token');
  console.log('getUserPosts called with userId:', userId);
  
  if (!userId) {
    console.warn('getUserPosts: userId is empty/null');
    return [];
  }
  
  const url = `${API_BASE}/posts/user/${userId}`;
  console.log('getUserPosts - fetching from URL:', url);
  
  const response = await fetch(url, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  
  const data = await handleResponse(response);
  console.log('getUserPosts response data:', data);
  console.log('getUserPosts Data array:', data?.Data);
  
  return data?.Data || [];
}

export async function getPosts(page = 1, pageSize = 20) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/posts?page=${page}&pageSize=${pageSize}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const data = await handleResponse(response);
  return {
    posts: data?.Data || data?.data || [],
    pagination: data?.Pagination || data?.pagination || { page, pageSize, totalCount: 0, totalPages: 0, hasMore: false }
  };
}

export async function getPostById(postId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/posts/${postId}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function getPostByIdAsAdmin(postId) {
  const token = localStorage.getItem('adminToken');
  const response = await fetch(`${API_BASE}/posts/${postId}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function createPost({ content, imageUrl, groupId }) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/posts`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
    body: JSON.stringify({ content, imageUrl, groupId })
  });
  
  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function sharePost(postId, { content, groupId } = {}) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/posts/${postId}/share`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
    body: JSON.stringify({ content: content || '', groupId })
  });
  
  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function getStories() {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/stories`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const data = await handleResponse(response);
  return data?.Data || [];
}

export async function getStoryById(storyId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/stories/${storyId}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function getStoriesByUserId(userId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/stories/user/${userId}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const data = await handleResponse(response);
  return data?.Data || [];
}

export async function deleteStory(storyId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/stories/${storyId}`, {
    method: 'DELETE',
    headers: { 'Authorization': `Bearer ${token}` }
  });
  await handleResponse(response);
  return true;
}

export async function createStory({ content, imageUrl, expireAt }) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/stories`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
    body: JSON.stringify({ content, imageUrl, expireAt })
  });
  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function likePost(postId, userId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/likes`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
    body: JSON.stringify({ postId, userId })
  });
  
  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function unlikePost(postId, userId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/likes/post/${postId}/user/${userId}`, {
    method: 'DELETE',
    headers: { 'Authorization': `Bearer ${token}` }
  });
  
  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function deletePost(postId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/posts/${postId}`, {
    method: 'DELETE',
    headers: { 'Authorization': `Bearer ${token}` }
  });
  
  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function getUser(userId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/users/${userId}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  
  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function getUsersByIds(userIds) {
  const token = localStorage.getItem('token');
  if (!userIds || userIds.length === 0) return [];
  const response = await fetch(`${API_BASE}/users/batch`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
    body: JSON.stringify({ userIds })
  });
  
  const data = await handleResponse(response);
  return data?.Data || [];
}

export async function getNotifications(userId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/notifications/user/${userId}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const data = await handleResponse(response);
  return data?.Data || [];
}

/** Đánh dấu đã đọc thông báo feed (không gồm tin nhắn); lịch sử giữ nguyên trên server. */
export async function markFeedNotificationsRead(userId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/notifications/user/${userId}/mark-feed-read`, {
    method: 'PUT',
    headers: { 'Authorization': `Bearer ${token}` }
  });
  await handleResponse(response);
  return true;
}

const normalizeComment = (comment) => {
  const shaped = shapeComment(comment);
  if (shaped) return shaped;
  const rawT = comment?.CreatedAt ?? comment?.createdAt;
  const iso = toIsoUtcString(rawT);
  const createdAt = iso || (rawT != null && rawT !== '' ? String(rawT) : '');
  return {
    id: comment?.Id ?? comment?.id,
    content: comment?.Content ?? comment?.content ?? '',
    postId: comment?.PostId ?? comment?.postId,
    userId: comment?.UserId ?? comment?.userId,
    createdAt
  };
};

export async function getCommentsByPost(postId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/comments/post/${postId}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const data = await handleResponse(response);
  const comments = data?.Data || [];
  return sortCommentsNewestFirst((comments || []).map(normalizeComment));
}

export async function createComment(postId, content) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/comments`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
    body: JSON.stringify({ postId, content })
  });
  const data = await handleResponse(response);
  return normalizeComment(data?.Data || {});
}

export async function updateComment(commentId, content) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/comments/${commentId}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
    body: JSON.stringify({ content })
  });
  await handleResponse(response);
  return true;
}

export async function deleteComment(commentId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/comments/${commentId}`, {
    method: 'DELETE',
    headers: { 'Authorization': `Bearer ${token}` }
  });
  await handleResponse(response);
  return true;
}

const normalizeGroup = (group) => {
  if (!group) return null;
  return {
    id: group.Id ?? group.id,
    name: group.Name ?? group.name,
    slug: group.Slug ?? group.slug,
    description: group.Description ?? group.description ?? '',
    creatorId: group.CreatorId ?? group.creatorId,
    isJoined: group.IsJoined ?? group.isJoined ?? false,
    memberCount: group.MemberCount ?? group.memberCount ?? 0,
    createdAt: group.CreatedAt ?? group.createdAt,
    imageUrl: group.ImageUrl ?? group.imageUrl ?? null,
    images: group.Images ?? group.images ?? ['img1', 'img2', 'img3'],
    likes: group.Likes ?? group.likes ?? 0,
    comments: group.Comments ?? group.comments ?? 0
  };
};

export async function getGroups() {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/groups`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const data = await handleResponse(response);
  return (data?.Data || data?.data || []).map(normalizeGroup);
}

export async function createGroup({ name, description, memberIds, imageUrl }) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/groups`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({ name, description, memberIds, imageUrl })
  });
  const data = await handleResponse(response);
  return normalizeGroup(data?.Data || data?.data);
}

export async function joinGroupApi(groupId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/groups/${groupId}/join`, {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` }
  });
  await handleResponse(response);
}

export async function leaveGroupApi(groupId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/groups/${groupId}/leave`, {
    method: 'DELETE',
    headers: { 'Authorization': `Bearer ${token}` }
  });
  await handleResponse(response);
}

export async function getAllUsers(search = '') {
  const token = localStorage.getItem('token');
  const url = `${API_BASE}/users${search ? `?search=${encodeURIComponent(search)}` : ''}`;
  const response = await fetch(url, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  
  const data = await handleResponse(response);
  return data?.Data || [];
}

export async function getAcceptedFriends(userId, pageNumber = 1, pageSize = 20) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/friendships/user/${userId}/accepted?pageNumber=${pageNumber}&pageSize=${pageSize}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  
  const data = await handleResponse(response);
  return data?.Data || [];
}

export async function getConversationsSorted(userId) {
  const token = localStorage.getItem('token');
  try {
    const response = await fetch(`${API_BASE}/friendships/user/${userId}/conversations`, {
      headers: { 'Authorization': `Bearer ${token}` }
    });
    
    console.log('🔄 API Response status:', response.status);
    console.log('🔄 API Response ok:', response.ok);
    
    const data = await handleResponse(response);
    console.log('🔄 API Response data:', data);
    return data?.Data || [];
  } catch (err) {
    console.error('🔄 Error calling getConversationsSorted:', err);
    throw err;
  }
}

export async function getConversationMessages(friendId, page = 1, pageSize = 50) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/messages/conversation/${friendId}?page=${page}&pageSize=${pageSize}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const data = await handleResponse(response);
  
  // Handle the new response structure: { Data: { messages: [...], pagination: {...} } }
  const responseData = data?.Data || {};
  return {
    messages: responseData?.messages || [],
    pagination: responseData?.pagination || { page, pageSize, totalCount: 0, totalPages: 0, hasMore: false }
  };
}

export async function getGroupMessages(groupId, page = 1, pageSize = 50) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/messages/group/${groupId}?page=${page}&pageSize=${pageSize}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const data = await handleResponse(response);
  
  // Handle the new response structure: { Data: { messages: [...], pagination: {...} } }
  const responseData = data?.Data || {};
  return {
    messages: responseData?.messages || [],
    pagination: responseData?.pagination || { page, pageSize, totalCount: 0, totalPages: 0, hasMore: false }
  };
}

export async function getPendingRequests(userId, pageNumber = 1, pageSize = 20) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/friendships/user/${userId}/pending?pageNumber=${pageNumber}&pageSize=${pageSize}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  
  const data = await handleResponse(response);
  return data?.Data || [];
}

export async function updateUser(userId, { fullName, bio, profilePictureUrl }) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/users/${userId}`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
    body: JSON.stringify({ fullName, bio, profilePictureUrl })
  });
  
  const data = await handleResponse(response);
  return data;
}

export async function uploadProfilePicture(userId, file) {
  const token = localStorage.getItem('token');
  const formData = new FormData();
  formData.append('file', file);
  
  const response = await fetch(`${API_BASE}/users/${userId}/upload-profile-picture`, {
    method: 'POST',
    headers: { 'Authorization': `Bearer ${token}` },
    body: formData
  });
  
  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function sendFriendRequest(friendId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/friendships/send-request`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
    body: JSON.stringify({ friendId })
  });
  
  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function acceptFriendRequest(friendshipId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/friendships/${friendshipId}/accept`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }
  });
  
  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function declineFriendRequest(friendshipId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/friendships/${friendshipId}/decline`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }
  });
  
  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function sendMessage(receiverId, content, groupId = null) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/messages`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` },
    body: JSON.stringify({ receiverId, content, groupId })
  });
  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function removeFriend(friendId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/friendships/remove/${friendId}`, {
    method: 'DELETE',
    headers: { 'Content-Type': 'application/json', 'Authorization': `Bearer ${token}` }
  });
  
  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function checkFriendshipStatus(friendId) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/friendships/status/${friendId}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const data = await handleResponse(response);
  return data?.Data || null;
}

// ===== ADMIN API FUNCTIONS =====

export async function adminLogin({ userName, password }) {
  const response = await fetch(`${API_BASE}/admin/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ userName, password })
  });

  const data = await handleResponse(response);
  const token = data?.Data?.Token || data?.Data?.token;
  const admin = data?.Data?.Admin || data?.Data?.admin;

  if (!token || !admin) {
    throw new Error('Invalid response from server - missing Token or Admin');
  }

  return { token, admin };
}

export async function getAdminInfo() {
  const token = localStorage.getItem('adminToken');
  const response = await fetch(`${API_BASE}/admin/me`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });
  const data = await handleResponse(response);
  return data?.Data || null;
}

// POST REPORTS - User side
export async function createPostReport(postId, reason, detail) {
  const token = localStorage.getItem('token');
  const response = await fetch(`${API_BASE}/postreports/create`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({ PostId: postId, Reason: reason, Detail: detail })
  });

  const data = await handleResponse(response);
  return data?.Data || null;
}

// POST REPORTS - Admin side
export async function getPendingReports() {
  const token = localStorage.getItem('adminToken');
  const response = await fetch(`${API_BASE}/postreports/pending`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });

  const data = await handleResponse(response);
  return data?.Data || [];
}

export async function getAllReports(page = 1, pageSize = 20) {
  const token = localStorage.getItem('adminToken');
  const response = await fetch(`${API_BASE}/postreports?page=${page}&pageSize=${pageSize}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });

  const data = await handleResponse(response);
  return data?.Data || [];
}

export async function getReportById(reportId) {
  const token = localStorage.getItem('adminToken');
  const response = await fetch(`${API_BASE}/postreports/${reportId}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });

  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function approveReport(reportId) {
  const token = localStorage.getItem('adminToken');
  const response = await fetch(`${API_BASE}/postreports/${reportId}/approve`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    }
  });

  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function rejectReport(reportId) {
  const token = localStorage.getItem('adminToken');
  const response = await fetch(`${API_BASE}/postreports/${reportId}/reject`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    }
  });

  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function deleteReport(reportId) {
  const token = localStorage.getItem('adminToken');
  const response = await fetch(`${API_BASE}/postreports/${reportId}`, {
    method: 'DELETE',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });

  return await handleResponse(response);
}

// ===== ADMIN USER MANAGEMENT API =====

export async function getAllUsersWithRoles() {
  const token = localStorage.getItem('adminToken');
  const response = await fetch(`${API_BASE}/admin/users-with-roles`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });

  const data = await handleResponse(response);
  return data?.Data || [];
}

export async function getUserWithRoles(userId) {
  const token = localStorage.getItem('adminToken');
  const response = await fetch(`${API_BASE}/admin/user/${userId}`, {
    headers: { 'Authorization': `Bearer ${token}` }
  });

  const data = await handleResponse(response);
  return data?.Data || null;
}

export async function assignRoleToUser(userId, role) {
  const token = localStorage.getItem('adminToken');
  const response = await fetch(`${API_BASE}/admin/assign-role`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({ userId, role })
  });

  return await handleResponse(response);
}

export async function removeRoleFromUser(userId, role) {
  const token = localStorage.getItem('adminToken');
  const response = await fetch(`${API_BASE}/admin/remove-role`, {
    method: 'DELETE',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({ userId, role })
  });

  return await handleResponse(response);
}

export async function toggleLockUser(userId) {
  const token = localStorage.getItem('adminToken');
  const response = await fetch(`${API_BASE}/admin/toggle-lock-user`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({ userId })
  });

  return await handleResponse(response);
}

export async function resetUserPassword(userId, newPassword) {
  const token = localStorage.getItem('adminToken');
  const response = await fetch(`${API_BASE}/admin/reset-user-password`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${token}`
    },
    body: JSON.stringify({ userId, newPassword })
  });

  return await handleResponse(response);
}

export async function deleteUser(userId) {
  const token = localStorage.getItem('adminToken');
  const response = await fetch(`${API_BASE}/admin/delete-user/${userId}`, {
    method: 'DELETE',
    headers: {
      'Authorization': `Bearer ${token}`
    }
  });

  return await handleResponse(response);
}
