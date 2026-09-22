import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { register } from '../api';
import Header from '../components/Header';

export default function RegisterPage() {
  const [formData, setFormData] = useState({
    userName: '',
    fullName: '',
    email: '',
    password: '',
    confirmPassword: ''
  });
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [error, setError] = useState('');
  const [passwordErrors, setPasswordErrors] = useState({
    length: false,
    uppercase: false,
    lowercase: false,
    number: false,
    special: false
  });
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  const validatePassword = (password) => {
    const errors = {
      length: password.length >= 6,
      uppercase: /[A-Z]/.test(password),
      lowercase: /[a-z]/.test(password),
      number: /[0-9]/.test(password),
      special: /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password)
    };
    setPasswordErrors({
      length: !errors.length,
      uppercase: !errors.uppercase,
      lowercase: !errors.lowercase,
      number: !errors.number,
      special: !errors.special
    });
    return !Object.values(errors).includes(false);
  };

  const handleInputChange = (e) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));

    // Validate password format when user types
    if (name === 'password') {
      validatePassword(value);
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setLoading(true);

    // Validate password format
    if (!validatePassword(formData.password)) {
      setError('Mật khẩu không đúng định dạng. Vui lòng kiểm tra các yêu cầu bên dưới!');
      setLoading(false);
      return;
    }

    // Validate passwords match
    if (formData.password !== formData.confirmPassword) {
      setError('Mật khẩu không trùng khớp. Vui lòng kiểm tra lại!');
      setLoading(false);
      return;
    }

    try {
      const result = await register({
        userName: formData.userName,
        email: formData.email,
        fullName: formData.fullName,
        password: formData.password
      });
      
      if (!result || !result.token) {
        throw new Error('Invalid registration response from server');
      }

      localStorage.setItem('token', result.token);
      if (result.user) {
        localStorage.setItem('user', JSON.stringify(result.user));
      }
      
      // Dispatch event to notify App.jsx about token change (same-tab)
      window.dispatchEvent(new Event('tokenUpdated'));
      
      navigate('/login');
    } catch (err) {
      setError(err.message || 'An error occurred during registration');
    } finally {
      setLoading(false);
    }
  };

  const handleNavigateToLogin = () => {
    navigate('/login');
  };

  return (
    <div className="auth-container">
      <Header showControls={false} />
      <div className="auth-page">
        <div className="auth-form-wrapper register-form-wrapper">
          <h2 className="auth-title">Tạo Tài Khoản</h2>
          
          {error && <div className="error-message">{error}</div>}

          <form onSubmit={handleSubmit} className="auth-form register-form">
            <div className="form-group">
              <label htmlFor="userName" className="form-label">Tên đăng nhập:</label>
              <input
                id="userName"
                type="text"
                name="userName"
                value={formData.userName}
                onChange={handleInputChange}
                placeholder="Tên đăng nhập"
                className="form-input"
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="fullName" className="form-label">Họ và Tên:</label>
              <input
                id="fullName"
                type="text"
                name="fullName"
                value={formData.fullName}
                onChange={handleInputChange}
                placeholder="Nhập họ và tên đầy đủ"
                className="form-input"
                required
              />
            </div>



            <div className="form-group">
              <label htmlFor="email" className="form-label">Email:</label>
              <input
                id="email"
                type="email"
                name="email"
                value={formData.email}
                onChange={handleInputChange}
                placeholder="Nhập email"
                className="form-input"
                required
              />
            </div>

            <div className="form-group">
              <label htmlFor="password" className="form-label">Mật Khẩu:</label>
              <div className="password-wrapper">
                <input
                  id="password"
                  type={showPassword ? 'text' : 'password'}
                  name="password"
                  value={formData.password}
                  onChange={handleInputChange}
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
              <div className="password-requirements">
                <div className={passwordErrors.length ? 'requirement error' : 'requirement success'}>
                  <i className={passwordErrors.length ? 'fa-solid fa-times' : 'fa-solid fa-check'}></i>
                  Ít nhất 6 ký tự
                </div>
                <div className={passwordErrors.uppercase ? 'requirement error' : 'requirement success'}>
                  <i className={passwordErrors.uppercase ? 'fa-solid fa-times' : 'fa-solid fa-check'}></i>
                  Chứa chữ hoa (A-Z)
                </div>
                <div className={passwordErrors.lowercase ? 'requirement error' : 'requirement success'}>
                  <i className={passwordErrors.lowercase ? 'fa-solid fa-times' : 'fa-solid fa-check'}></i>
                  Chứa chữ thường (a-z)
                </div>
                <div className={passwordErrors.number ? 'requirement error' : 'requirement success'}>
                  <i className={passwordErrors.number ? 'fa-solid fa-times' : 'fa-solid fa-check'}></i>
                  Chứa số (0-9)
                </div>
                <div className={passwordErrors.special ? 'requirement error' : 'requirement success'}>
                  <i className={passwordErrors.special ? 'fa-solid fa-times' : 'fa-solid fa-check'}></i>
                  Chứa ký tự đặc biệt (!@#$%^&*...)
                </div>
              </div>
            </div>

            <div className="form-group">
              <label htmlFor="confirmPassword" className="form-label">Nhập Lại Mật Khẩu:</label>
              <div className="password-wrapper">
                <input
                  id="confirmPassword"
                  type={showConfirmPassword ? 'text' : 'password'}
                  name="confirmPassword"
                  value={formData.confirmPassword}
                  onChange={handleInputChange}
                  placeholder="Nhập lại mật khẩu"
                  className="form-input"
                  required
                />
                <button
                  type="button"
                  className="password-toggle"
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                >
                  <i className="fa-solid fa-eye"></i>
                </button>
              </div>
            </div>

            <button 
              type="submit" 
              className="btn btn-register"
              disabled={loading}
            >
              {loading ? 'Đang đăng ký...' : 'Đăng ký'}
            </button>
          </form>

          <div className="form-footer">
            <span>Đã có tài khoản? </span>
            <button 
              type="button"
              className="link-button"
              onClick={handleNavigateToLogin}
            >
              Đăng nhập
            </button>
          </div>
        </div>
      </div>
    </div>
  );
}
