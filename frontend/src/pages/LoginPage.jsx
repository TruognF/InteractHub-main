import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { login } from '../api';
import Header from '../components/Header';

export default function LoginPage() {
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    try {
      const result = await login({ userName: username, password });
      
      if (!result || !result.token) {
        throw new Error('Invalid login response from server');
      }

      localStorage.setItem('token', result.token);
      if (result.user) {
        localStorage.setItem('user', JSON.stringify(result.user));
      }
      
      // Dispatch event to notify App.jsx about token change (same-tab)
      window.dispatchEvent(new Event('tokenUpdated'));
      
      navigate('/home');
    } catch (err) {
      setError(err.message || 'An error occurred during login');
    } finally {
      setLoading(false);
    }
  };

  const handleNavigateToRegister = () => {
    navigate('/register');
  };

  return (
    <div className="auth-container">
      <Header showControls={false} />
      <div className="auth-page">
        <div className="auth-form-wrapper">
          <h2 className="auth-title">Đăng Nhập</h2>
          
          {error && <div className="error-message">{error}</div>}

          <form onSubmit={handleSubmit} className="auth-form">
            <div className="form-group">
              <label htmlFor="username" className="form-label">Tên đăng nhập:</label>
              <input
                id="username"
                type="text"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                placeholder="Nhập tên đăng nhập"
                className="form-input"
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="password" className="form-label">Mật khẩu:</label>
              <div className="password-wrapper">
                <input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  placeholder="Nhập mật khẩu"
                  className="form-input"
                  required
                />
                <button
                  type="button"
                  className="password-toggle"
                  onClick={() => setShowPassword(!showPassword)}
                >
                  <i className="fa-solid fa-eye"></i>
                </button>
              </div>
            </div>

            <button 
              type="submit" 
              className="btn btn-login"
              disabled={loading}
            >
              {loading ? 'Đang đăng nhập...' : 'Đăng nhập'}
            </button>
          </form>

          <div className="divider"></div>

          <button 
            type="button"
            className="btn btn-register"
            onClick={handleNavigateToRegister}
          >
            Tạo tài khoản
          </button>

          <div className="admin-login-hint" style={{ marginTop: '16px', textAlign: 'center' }}>
            <span style={{ color: '#888', fontSize: '14px' }}>Admin? </span>
            <button
              type="button"
              onClick={() => navigate('/admin/login')}
              style={{ background: 'none', border: 'none', color: '#4a90d9', cursor: 'pointer', textDecoration: 'underline', fontSize: '14px' }}
            >
              Đăng nhập tại đây
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
