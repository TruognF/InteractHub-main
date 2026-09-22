import { useState, useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { GroupsProvider } from './contexts/GroupsContext';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import HomePage from './pages/HomePage';
import GroupPage from './pages/GroupPage';
import GroupDetailPage from './pages/GroupDetailPage';
import CreateGroupPage from './pages/CreateGroupPage';
import MessagePage from './pages/MessagePage';
import ProfilePage from './pages/ProfilePage';
import StoryPage from './pages/StoryPage';
import UserProfilePage from './pages/UserProfilePage';
import PostDetailPage from './pages/PostDetailPage';
import AdminLoginPage from './pages/AdminLoginPage';
import AdminReportsPage from './pages/AdminReportsPage';
import AdminProtectedRoute from './components/AdminProtectedRoute';
import { startConnection, resetPostHubConnection } from './utils/postHubConnection';
import { startStoryConnection, resetStoryHubConnection } from './utils/storyHubConnection';
import { startConnection as startGroupConnection, resetGroupHubConnection } from './utils/groupHubConnection';
import { startConnection as startUserConnection, resetUserHubConnection } from './utils/userHubConnection';
import { startConnection as startNotificationConnection, resetConnection as resetNotificationConnection } from './utils/notificationHubConnection';
import NotificationHubBridge from './components/NotificationHubBridge';

// Helper function to check if JWT token is valid (not expired)
function isTokenValid(token) {
  if (!token) return false;
  
  try {
    // JWT has 3 parts separated by dots: header.payload.signature
    const parts = token.split('.');
    if (parts.length !== 3) return false;
    
    // Decode the payload (second part)
    const payload = JSON.parse(atob(parts[1]));
    
    // Check if token has expired (exp is in seconds, Date.now() is in milliseconds)
    const expirationTime = payload.exp * 1000;
    const currentTime = Date.now();
    
    console.log('Token expiration check:', {
      expiresAt: new Date(expirationTime),
      currentTime: new Date(currentTime),
      isValid: currentTime < expirationTime
    });
    
    return currentTime < expirationTime;
  } catch (err) {
    console.error('Error validating token:', err);
    return false;
  }
}

function App() {
  const [token, setToken] = useState(() => {
    // Initialize token from localStorage, but validate it
    try {
      const savedToken = localStorage.getItem('token');
      if (savedToken && isTokenValid(savedToken)) {
        return savedToken;
      } else {
        // Clear invalid/expired token
        if (savedToken) {
          console.log('Token is expired or invalid, clearing...');
          localStorage.removeItem('token');
          localStorage.removeItem('user');
        }
        return null;
      }
    } catch (err) {
      console.error('Error initializing token:', err);
      localStorage.removeItem('token');
      localStorage.removeItem('user');
      return null;
    }
  });

  useEffect(() => {
    if (!token) {
      resetStoryHubConnection();
      resetPostHubConnection();
      resetGroupHubConnection();
      resetUserHubConnection();
      resetNotificationConnection();
      return;
    }
    startConnection().catch((err) => {
      console.error('Failed to start PostHub connection:', err);
    });
    startStoryConnection().catch((err) => {
      console.error('Failed to start StoryHub connection:', err);
    });
    startGroupConnection().catch((err) => {
      console.error('Failed to start GroupHub connection:', err);
    });
    startUserConnection().catch((err) => {
      console.error('Failed to start UserHub connection:', err);
    });
    startNotificationConnection().catch((err) => {
      console.error('Failed to start NotificationHub connection:', err);
    });
  }, [token]);

  useEffect(() => {
    // Listen for storage changes (when token is saved from another tab or in this tab)
    const handleStorageChange = async () => {
      const savedToken = localStorage.getItem('token');
      if (savedToken && isTokenValid(savedToken)) {
        setToken(savedToken);
        try {
          await startConnection();
        } catch (err) {
          console.error('Failed to start PostHub connection:', err);
        }
        try {
          await startStoryConnection();
        } catch (err) {
          console.error('Failed to start StoryHub connection:', err);
        }
        try {
          await startGroupConnection();
        } catch (err) {
          console.error('Failed to start GroupHub connection:', err);
        }
        try {
          await startUserConnection();
        } catch (err) {
          console.error('Failed to start UserHub connection:', err);
        }
        try {
          await startNotificationConnection();
        } catch (err) {
          console.error('Failed to start NotificationHub connection:', err);
        }
      } else {
        setToken(null);
      }
    };

    // Listen to storage events (cross-tab)
    window.addEventListener('storage', handleStorageChange);

    // Also listen to custom event for same-tab updates
    window.addEventListener('tokenUpdated', handleStorageChange);

    return () => {
      window.removeEventListener('storage', handleStorageChange);
      window.removeEventListener('tokenUpdated', handleStorageChange);
    };
  }, []);

  return (
    <GroupsProvider>
      <Router>
        {token ? <NotificationHubBridge token={token} /> : null}
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/register" element={<RegisterPage />} />
          <Route path="/home" element={token ? <HomePage /> : <Navigate to="/login" replace />} />
          <Route path="/group" element={token ? <GroupPage /> : <Navigate to="/login" replace />} />
          <Route path="/group/:groupSlug" element={token ? <GroupDetailPage /> : <Navigate to="/login" replace />} />
          <Route path="/story/:storyId" element={token ? <StoryPage /> : <Navigate to="/login" replace />} />
          <Route path="/story/user/:userId" element={token ? <StoryPage /> : <Navigate to="/login" replace />} />
          <Route path="/creategroup" element={token ? <CreateGroupPage /> : <Navigate to="/login" replace />} />
          <Route path="/message" element={token ? <MessagePage /> : <Navigate to="/login" replace />} />
          <Route path="/profile" element={token ? <ProfilePage /> : <Navigate to="/login" replace />} />
          <Route path="/user-profile/:userId" element={token ? <UserProfilePage /> : <Navigate to="/login" replace />} />
          <Route path="/post/:postId" element={token ? <PostDetailPage /> : <Navigate to="/login" replace />} />
          <Route path="/admin/login" element={<AdminLoginPage />} />
          <Route path="/admin/reports" element={<AdminProtectedRoute><AdminReportsPage /></AdminProtectedRoute>} />
          <Route path="/" element={token ? <Navigate to="/home" replace /> : <Navigate to="/login" replace />} />
        </Routes>
      </Router>
    </GroupsProvider>
  );
}

export default App;
