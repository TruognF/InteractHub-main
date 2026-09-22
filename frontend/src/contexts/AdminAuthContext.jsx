import React, { createContext, useState, useEffect, useCallback, useContext } from 'react';

export const AdminAuthContext = createContext();

// Helper to decode JWT
function decodeToken(token) {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    const payload = JSON.parse(atob(parts[1]));
    return payload;
  } catch {
    return null;
  }
}

// Helper to check if token is valid
function isTokenValid(token) {
  if (!token) return false;
  const payload = decodeToken(token);
  if (!payload || !payload.exp) return false;
  return Date.now() < payload.exp * 1000;
}

export function AdminAuthProvider({ children }) {
  const [adminToken, setAdminToken] = useState(() => {
    try {
      const saved = localStorage.getItem('adminToken');
      return saved && isTokenValid(saved) ? saved : null;
    } catch {
      return null;
    }
  });

  const [admin, setAdmin] = useState(() => {
    try {
      const saved = localStorage.getItem('admin');
      return saved ? JSON.parse(saved) : null;
    } catch {
      return null;
    }
  });

  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState(null);

  // Login as admin
  const adminLogin = useCallback(async (userName, password) => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/admin/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ userName, password })
      });

      if (!response.ok) {
        let errorMsg = 'Lỗi đăng nhập';
        try {
          const errorData = await response.json();
          errorMsg = errorData.message || errorData.Message || response.statusText || errorMsg;
        } catch {
          errorMsg = `HTTP ${response.status}: ${response.statusText}`;
        }
        throw new Error(errorMsg);
      }

      const data = await response.json();
      
      // Debug log
      console.log('Admin login response:', data);

      // Extract token and admin data from response
      const token = data.Data?.Token || data.data?.token;
      const adminData = data.Data?.Admin || data.data?.admin;

      if (!token) {
        throw new Error('Không nhận được token từ server');
      }

      if (!adminData) {
        throw new Error('Không nhận được thông tin admin từ server');
      }

      localStorage.setItem('adminToken', token);
      localStorage.setItem('admin', JSON.stringify(adminData));

      setAdminToken(token);
      setAdmin(adminData);

      return { token, admin: adminData };
    } catch (err) {
      const message = err.message || 'Đăng nhập thất bại';
      console.error('Admin login error:', message);
      setError(message);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, []);

  // Logout
  const adminLogout = useCallback(async () => {
    localStorage.removeItem('adminToken');
    localStorage.removeItem('admin');
    setAdminToken(null);
    setAdmin(null);
    setError(null);
  }, []);

  // Refresh token
  const refreshAdminToken = useCallback(async () => {
    if (!adminToken) return;

    try {
      const response = await fetch('/api/admin/refresh-token', {
        method: 'POST',
        headers: {
          'Authorization': `Bearer ${adminToken}`,
          'Content-Type': 'application/json'
        }
      });

      if (!response.ok) {
        throw new Error('Failed to refresh token');
      }

      const data = await response.json();
      const newToken = data.Data?.Token || data.data?.token;

      if (!newToken) {
        throw new Error('Invalid refresh token response');
      }

      localStorage.setItem('adminToken', newToken);
      setAdminToken(newToken);

      return newToken;
    } catch (err) {
      console.error('Token refresh failed:', err);
      await adminLogout();
      throw err;
    }
  }, [adminToken, adminLogout]);

  // Check if admin is still logged in
  const isAdminLoggedIn = Boolean(adminToken && admin && isTokenValid(adminToken));

  // Handle token refresh on mount and periodically
  useEffect(() => {
    if (!isAdminLoggedIn) return;

    const payload = decodeToken(adminToken);
    if (!payload?.exp) return;

    // Refresh token 5 minutes before expiration
    const expiresIn = payload.exp * 1000 - Date.now();
    const refreshTime = expiresIn - 5 * 60 * 1000;

    if (refreshTime <= 0) {
      // Token expires soon, refresh immediately
      refreshAdminToken().catch(err => {
        console.error('Auto-refresh failed:', err);
      });
      return;
    }

    const timeout = setTimeout(() => {
      refreshAdminToken().catch(err => {
        console.error('Auto-refresh failed:', err);
      });
    }, refreshTime);

    return () => clearTimeout(timeout);
  }, [adminToken, isAdminLoggedIn, refreshAdminToken]);

  const value = {
    adminToken,
    admin,
    isLoading,
    error,
    isAdminLoggedIn,
    adminLogin,
    adminLogout,
    refreshAdminToken
  };

  return (
    <AdminAuthContext.Provider value={value}>
      {children}
    </AdminAuthContext.Provider>
  );
}

export function useAdminAuth() {
  const context = React.useContext(AdminAuthContext);
  if (!context) {
    throw new Error('useAdminAuth must be used within AdminAuthProvider');
  }
  return context;
}
