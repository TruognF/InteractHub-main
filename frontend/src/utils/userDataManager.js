/**
 * User Data Manager - Manages independent data storage for each user
 * Each user has their own:
 * - Friends list
 * - Posts
 * - Messages
 * - Group memberships
 * - Liked posts
 */

// Get all users
export const getAllUsers = () => {
  const users = JSON.parse(localStorage.getItem('all_users') || '[]');
  return users;
};

// Create new user
export const createUser = (userData) => {
  const users = getAllUsers();
  const newUser = {
    ...userData,
    id: `user_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
    createdAt: new Date().toISOString()
  };
  
  users.push(newUser);
  localStorage.setItem('all_users', JSON.stringify(users));
  
  // Initialize user data structure
  initializeUserData(newUser.id);
  
  return newUser;
};

// Initialize empty data structure for a new user
export const initializeUserData = (userId) => {
  // Create mock friends for new user
  const mockFriends = [
    { id: 'friend1', name: 'Nguyễn Văn A', fullName: 'Nguyễn Văn A', avatar: 'user', isActive: true },
    { id: 'friend2', name: 'Trần Thị B', fullName: 'Trần Thị B', avatar: 'user', isActive: false },
  ];
  
  // Create mock posts for new user
  const today = new Date().toLocaleDateString('vi-VN');
  const mockPosts = [
    {
      id: `post_${Date.now()}_1`,
      userId: userId,
      username: 'Tôi',
      content: 'Hôm nay thời tiết đẹp quá!',
      imageUrl: 'https://via.placeholder.com/300',
      createdAt: new Date().toISOString(),
      likesCount: 10,
      commentsCount: 3,
      likedBy: []
    },
  ];
  
  const userData = {
    friends: mockFriends,
    posts: mockPosts,
    messages: {},
    groupMemberships: [],
    likedPosts: {}
  };
  
  localStorage.setItem(`user_data_${userId}`, JSON.stringify(userData));
};

// Get user data
export const getUserData = (userId) => {
  const data = JSON.parse(localStorage.getItem(`user_data_${userId}`) || '{}');
  return {
    friends: data.friends || [],
    posts: data.posts || [],
    messages: data.messages || {},
    groupMemberships: data.groupMemberships || [],
    likedPosts: data.likedPosts || {}
  };
};

// Update user data
export const updateUserData = (userId, updates) => {
  const currentData = getUserData(userId);
  const newData = { ...currentData, ...updates };
  localStorage.setItem(`user_data_${userId}`, JSON.stringify(newData));
  return newData;
};

// Add friend
export const addFriend = (userId, friend) => {
  const data = getUserData(userId);
  if (!data.friends.find(f => f.id === friend.id)) {
    data.friends.push(friend);
    updateUserData(userId, { friends: data.friends });
  }
};

// Get friends
export const getFriendsForUser = (userId) => {
  const data = getUserData(userId);
  return data.friends;
};

// Get group memberships
export const getGroupMembershipsForUser = (userId) => {
  const data = getUserData(userId);
  return data.groupMemberships;
};

// Add post
export const addPost = (userId, post) => {
  const data = getUserData(userId);
  const newPost = {
    ...post,
    id: `post_${Date.now()}_${Math.random().toString(36).substr(2, 9)}`,
    userId: userId,
    createdAt: new Date().toISOString(),
    likesCount: 0,
    commentsCount: 0,
    likedBy: []
  };
  data.posts.push(newPost);
  updateUserData(userId, { posts: data.posts });
  return newPost;
};

// Get posts for user
export const getPostsForUser = (userId) => {
  const data = getUserData(userId);
  return data.posts;
};

// Like post
export const likePost = (userId, postId) => {
  const data = getUserData(userId);
  if (!data.likedPosts[postId]) {
    data.likedPosts[postId] = [userId];
  } else if (!data.likedPosts[postId].includes(userId)) {
    data.likedPosts[postId].push(userId);
  }
  updateUserData(userId, { likedPosts: data.likedPosts });
};

// Unlike post
export const unlikePost = (userId, postId) => {
  const data = getUserData(userId);
  if (data.likedPosts[postId]) {
    data.likedPosts[postId] = data.likedPosts[postId].filter(id => id !== userId);
    if (data.likedPosts[postId].length === 0) {
      delete data.likedPosts[postId];
    }
  }
  updateUserData(userId, { likedPosts: data.likedPosts });
};

// Get liked posts for user
export const getLikedPostsForUser = (userId) => {
  const data = getUserData(userId);
  return data.likedPosts;
};

// Add/Update message conversation
export const addMessage = (userId, conversationId, message) => {
  const data = getUserData(userId);
  if (!data.messages[conversationId]) {
    data.messages[conversationId] = [];
  }
  data.messages[conversationId].push(message);
  updateUserData(userId, { messages: data.messages });
};

// Get messages for conversation
export const getMessagesForConversation = (userId, conversationId) => {
  const data = getUserData(userId);
  return data.messages[conversationId] || [];
};

// Get all conversations for user
export const getConversationsForUser = (userId) => {
  const data = getUserData(userId);
  return Object.keys(data.messages);
};

// Add group membership
export const addGroupMembership = (userId, group) => {
  const data = getUserData(userId);
  if (!data.groupMemberships.find(g => g.id === group.id)) {
    data.groupMemberships.push(group);
    updateUserData(userId, { groupMemberships: data.groupMemberships });
  }
};

// Remove group membership
export const removeGroupMembership = (userId, groupId) => {
  const data = getUserData(userId);
  data.groupMemberships = data.groupMemberships.filter(g => g.id !== groupId);
  updateUserData(userId, { groupMemberships: data.groupMemberships });
};
