import { useNavigate, useLocation } from 'react-router-dom';
import { useState, useEffect } from 'react';
import logoImage from '../assets/logo.png';
import logoTextImage from '../assets/chữ logo 2.png';
import '../styles/Header.css';

export default function Header({ onLogout, showControls = true, onSearch, searchValue = '', unreadMessageCount = 0 }) {
  const navigate = useNavigate();
  const location = useLocation();
  const [currentUser, setCurrentUser] = useState(null);
  const [searchQuery, setSearchQuery] = useState(searchValue);

  useEffect(() => {
    if (typeof searchValue === 'string') {
      setSearchQuery(searchValue);
    }
  }, [searchValue]);

  useEffect(() => {
    // Load user data from localStorage
    const loadUserData = () => {
      try {
        const userDataJson = localStorage.getItem('user');
        if (userDataJson) {
          const userData = JSON.parse(userDataJson);
          setCurrentUser(userData);
          
          // Debug logging
          console.log('Header - currentUser loaded:', {
            id: userData.Id,
            name: userData.UserName,
            ProfilePictureUrl: userData.ProfilePictureUrl
          });
        }
      } catch (err) {
        console.error('Error loading user data in Header:', err);
        setCurrentUser(null);
      }
    };

    loadUserData();

    // Listen for storage changes (when updated in other tabs or same page)
    const handleStorageChange = (e) => {
      if (e.key === 'user' || e.key === null) {
        loadUserData();
      }
    };

    // Listen for custom events (when updated on same page)
    const handleUserUpdate = () => {
      loadUserData();
    };

    window.addEventListener('storage', handleStorageChange);
    window.addEventListener('userUpdated', handleUserUpdate);

    return () => {
      window.removeEventListener('storage', handleStorageChange);
      window.removeEventListener('userUpdated', handleUserUpdate);
    };
  }, []);

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    
    // Dispatch event to notify App.jsx about token change
    window.dispatchEvent(new Event('tokenUpdated'));
    
    if (onLogout) {
      onLogout();
    } else {
      navigate('/login');
    }
  };

  const handleSearchSubmit = (event) => {
    event.preventDefault();
    const query = searchQuery.trim();
    if (!query) return;

    const match = query.match(/#?([a-zA-Z0-9_]+)/);
    if (!match || !match[1]) return;

    const hashtag = match[1];
    const normalized = `#${hashtag}`;
    setSearchQuery(normalized);
    if (typeof onSearch === 'function') {
      onSearch(hashtag);
    } else {
      navigate(`/home?hashtag=${encodeURIComponent(hashtag)}`);
    }
  };

  return (
    <header className="header">
      <div className="header-content">
        <div className="header-logo">
          <img src={logoImage} alt="Logo icon" className="logo-icon" />
          <img src={logoTextImage} alt="Logo text" className="logo-text-image" />
        </div>

        {showControls && (
          <>
            <div className="header-search">
              <form onSubmit={handleSearchSubmit} className="header-search-form">
                <input 
                  type="text" 
                  placeholder="Tìm kiếm"
                  value={searchQuery}
                  onChange={(e) => {
                    const value = e.target.value;
                    setSearchQuery(value);
                    if (typeof onSearch === 'function') {
                      onSearch(value);
                    }
                  }}
                  className="search-input"
                />
                <button type="submit" className="search-btn"><i className="fa-solid fa-magnifying-glass"></i></button>
              </form>
            </div>

            <div className="header-actions">
              <button 
                className={`header-icon-btn ${location.pathname === '/home' ? 'active' : ''}`}
                onClick={() => navigate('/home')}
                title="Home"
              >
                <span className="icon-home"><i className="fa-regular fa-house"></i></span>
              </button>
              <button 
                className={`header-icon-btn ${location.pathname === '/group' ? 'active' : ''}`}
                onClick={() => navigate('/group')}
                title="Groups"
              >
                <span className="icon-friends"><i className="fa-solid fa-users"></i></span>
              </button>
              <button 
                className={`header-icon-btn ${location.pathname === '/message' ? 'active' : ''}`}
                onClick={() => navigate('/message')}
                title="Messages"
              >
                <span className="icon-messages" style={{ position: 'relative' }}>
                  <i className="fa-regular fa-envelope"></i>
                  {unreadMessageCount > 0 && (
                    <span className="message-badge" style={{
                      position: 'absolute',
                      top: '-5px',
                      right: '-5px',
                      backgroundColor: '#dc3545',
                      color: 'white',
                      borderRadius: '50%',
                      width: '12px',
                      height: '12px',
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                      fontSize: '12px',
                      fontWeight: 'bold',
                      border: '2px solid white'
                    }}>
                      
                    </span>
                  )}
                </span>
              </button>
              <button 
                className="header-icon-btn logout-btn" 
                onClick={handleLogout}
                title="Logout"
              >
                <span className="icon-logout"><i className="fa-solid fa-arrow-right-from-bracket"></i></span>
              </button>
              <button 
                className={`header-icon-btn profile-btn ${location.pathname === '/profile' ? 'active' : ''}`}
                onClick={() => navigate('/profile')}
                title="Profile"
              >
                {currentUser?.ProfilePictureUrl ? (
                  <span className="icon-profile-avatar">
                    <img 
                      src={currentUser.ProfilePictureUrl} 
                      alt="Avatar"
                      style={{ width: '100%', height: '100%', objectFit: 'cover', borderRadius: '50%' }}
                      onError={(e) => {
                        console.warn('Failed to load profile avatar:', currentUser.ProfilePictureUrl);
                        e.target.style.display = 'none';
                        if (e.target.parentElement?.nextElementSibling) {
                          e.target.parentElement.nextElementSibling.style.display = 'flex';
                        }
                      }}
                    />
                  </span>
                ) : (
                  <span className="icon-profile"><i className="fa-solid fa-user"></i></span>
                )}
              </button>
            </div>
          </>
        )}
      </div>
    </header>
  );
}
