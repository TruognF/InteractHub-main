import { useState, useEffect, useRef, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { getConversationsSorted, getConversationMessages, sendMessage } from '../api';
import { messageHubConnection } from '../utils/messageHubConnection';
import Header from '../components/Header';
import '../styles/MessagePage.css';

// ⚙️ Configuration
const SCROLL_THRESHOLD = 100; // pixels from top to trigger lazy load
const DEBOUNCE_DELAY = 300; // ms for scroll event debouncing

export default function MessagePage() {
  const sortConversationsByLatest = (list = []) => {
    return [...list].sort((a, b) => {
      const timeA = a.lastMessageAt ? new Date(a.lastMessageAt).getTime() : 0;
      const timeB = b.lastMessageAt ? new Date(b.lastMessageAt).getTime() : 0;
      if (timeA !== timeB) return timeB - timeA;
      return String(a.name || '').localeCompare(String(b.name || ''));
    });
  };

  const [conversations, setConversations] = useState([]);
  const [selectedConversation, setSelectedConversation] = useState(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [currentUser, setCurrentUser] = useState(null);
  const [messages, setMessages] = useState([]);
  const [newMessage, setNewMessage] = useState('');
  const [loading, setLoading] = useState(true);
  const [messagesLoading, setMessagesLoading] = useState(false);
  const [page, setPage] = useState(1);
  const [hasMoreMessages, setHasMoreMessages] = useState(false);
  const [error, setError] = useState('');
  const [onlineFriends, setOnlineFriends] = useState([]);
  
  const navigate = useNavigate();
  const unsubscribeRef = useRef(null);
  const presenceUnsubscribeRef = useRef(null);
  const previousConversationRef = useRef(null);
  const messagesAreaRef = useRef(null);
  const isNearBottomRef = useRef(true); // Track if user is at bottom
  const scrollTimeoutRef = useRef(null); // Debounce scroll
  const lastScrollHeightRef = useRef(0); // Preserve scroll position

  const formatLastSeen = useCallback((lastSeenAt) => {
    if (!lastSeenAt) return '⚫ Offline';
    const lastSeen = new Date(lastSeenAt);
    const diffMs = Date.now() - lastSeen.getTime();
    if (!Number.isFinite(diffMs) || diffMs < 0) return '⚫ Offline';

    const minutes = Math.floor(diffMs / 60000);
    if (minutes < 1) return 'Vừa mới hoạt động';
    if (minutes < 60) return `Hoạt động ${minutes} phút trước`;

    const hours = Math.floor(minutes / 60);
    if (hours < 24) return `Hoạt động ${hours} giờ trước`;

    const days = Math.floor(hours / 24);
    return `Hoạt động ${days} ngày trước`;
  }, []);

  // Initialize SignalR connection and load friends
  useEffect(() => {
    const loadData = async () => {
      try {
        const userData = JSON.parse(localStorage.getItem('user'));
        const token = localStorage.getItem('token');
        const normalizedUser = {
          ...userData,
          id: userData.Id ?? userData.id,
          Id: userData.Id ?? userData.id
        };
        setCurrentUser(normalizedUser);

        // 🔄 Connect to SignalR MessageHub
        if (token && !messageHubConnection.isActive()) {
          try {
            console.log('[MessagePage] 📡 Attempting SignalR connection...');
            await messageHubConnection.connect(token);
            console.log('[MessagePage] ✅ SignalR connected successfully');
            
            // CRITICAL: Wait a moment to ensure connection is truly established
            await new Promise(resolve => setTimeout(resolve, 500));
          } catch (err) {
            console.warn('[MessagePage] ⚠️ SignalR connection failed, but REST API will still work:', err);
            // Continue with REST API even if SignalR fails
          }
        }

        // 🔄 Get conversations sorted by latest message
        const conversationsData = await getConversationsSorted(normalizedUser.Id);
        console.log('[MessagePage] 💬 Conversations loaded:', conversationsData?.length || 0);
        
        const conversationList = (conversationsData || [])
          .filter((convo) => !(convo.IsGroup ?? convo.isGroup ?? false))
          .map((convo) => ({
          id: convo.Id || convo.id || convo.FriendId || convo.friendId,
          name: convo.ConversationName || convo.conversationName || convo.FriendName || convo.friendName || 'Bạn',
          avatarUrl: convo.ConversationAvatarUrl || convo.conversationAvatarUrl || convo.FriendProfilePictureUrl || convo.friendProfilePictureUrl || '',
          isUnread: false,
          isActive: convo.IsOnline ?? convo.isOnline ?? false,
          lastSeenAt: convo.LastSeenAt ?? convo.lastSeenAt ?? null,
          lastMessage: convo.LastMessage || convo.lastMessage || '',
          lastMessageAt: convo.LastMessageTime || convo.lastMessageTime || null,
          lastTime: (convo.LastMessageTime || convo.lastMessageTime)
            ? new Date(convo.LastMessageTime || convo.lastMessageTime).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' })
            : convo.lastTime || '',
          isGroup: false,
          participantCount: convo.ParticipantCount ?? convo.participantCount ?? 2
        }));

        setConversations(sortConversationsByLatest(conversationList));
        
        // Extract online friends
        const online = conversationList.filter(c => c.isActive && !c.isGroup);
        setOnlineFriends(online);
        
        if (conversationList.length > 0) {
          const firstConversation = conversationList[0];
          setSelectedConversation(firstConversation);
          
          // 🔄 Join SignalR group for first conversation (if connected)
          if (messageHubConnection.isActive()) {
            try {
              if (firstConversation.isGroup) {
                console.log('[MessagePage] 👥 Joining group conversation:', firstConversation.id);
                await messageHubConnection.joinGroupConversation(firstConversation.id);
              } else {
                console.log('[MessagePage] 👥 Joining personal conversation:', firstConversation.id);
                await messageHubConnection.joinConversation(firstConversation.id);
              }
              previousConversationRef.current = firstConversation;
              console.log('[MessagePage] ✅ Joined conversation group');
            } catch (err) {
              console.warn('[MessagePage] ⚠️ Failed to join group:', err);
            }
          } else {
            console.warn('[MessagePage] ⚠️ messageHubConnection.isActive() returned false!');
          }
          
          await loadMessages(firstConversation);
        }

        // 👥 Subscribe to presence updates (online/offline)
        const unsubscribePresence = messageHubConnection.onPresence((presenceData) => {
          console.log('[MessagePage] 👥 Presence update received:', presenceData);
          
          setConversations((prev) => {
            const updated = prev.map((conv) => {
              if (conv.id === presenceData.userId) {
                console.log(`[MessagePage] Updating ${conv.name} to ${presenceData.status}`);
                return {
                  ...conv,
                  isActive: presenceData.status === 'online',
                  lastSeenAt: presenceData.status === 'online'
                    ? null
                    : (presenceData.lastSeenAt || new Date().toISOString())
                };
              }
              return conv;
            });
            
            // Update online friends list
            const online = updated.filter(c => c.isActive);
            setOnlineFriends(online);
            
            return updated;
          });
        });
        presenceUnsubscribeRef.current = unsubscribePresence;
      } catch (err) {
        console.error('[MessagePage] ❌ Error loading data:', err);
        setError(`Lỗi tải dữ liệu: ${err.message}`);
      } finally {
        setLoading(false);
      }
    };

    loadData();

    // Cleanup on unmount
    return () => {
      if (unsubscribeRef.current) {
        unsubscribeRef.current();
      }
      if (presenceUnsubscribeRef.current) {
        presenceUnsubscribeRef.current();
      }
      if (previousConversationRef.current && messageHubConnection.isActive()) {
        const conv = previousConversationRef.current;
        if (conv.isGroup) {
          messageHubConnection.leaveGroupConversation(conv.id).catch(err => 
            console.warn('[MessagePage] ⚠️ Error leaving group conversation:', err)
          );
        } else {
          messageHubConnection.leaveConversation(conv.id).catch(err => 
            console.warn('[MessagePage] ⚠️ Error leaving personal conversation:', err)
          );
        }
      }
      // Don't disconnect on unmount - keep connection alive for other pages
    };
  }, []);

  // 📜 Detect if user is near bottom
  const checkIfNearBottom = useCallback(() => {
    if (!messagesAreaRef.current) return false;
    const { scrollTop, scrollHeight, clientHeight } = messagesAreaRef.current;
    return scrollHeight - (scrollTop + clientHeight) < SCROLL_THRESHOLD;
  }, []);

  // 📜 Auto-scroll to bottom (only if user is near bottom)
  const scrollToBottom = useCallback(() => {
    if (messagesAreaRef.current) {
      setTimeout(() => {
        messagesAreaRef.current.scrollTop = messagesAreaRef.current.scrollHeight;
        isNearBottomRef.current = true;
      }, 0);
    }
  }, []);

  // 📜 Scroll with position preservation (for lazy loading)
  const scrollToPosition = useCallback((scrollHeight) => {
    if (messagesAreaRef.current) {
      setTimeout(() => {
        const newScrollHeight = messagesAreaRef.current.scrollHeight;
        const heightDifference = newScrollHeight - scrollHeight;
        messagesAreaRef.current.scrollTop = heightDifference;
      }, 0);
    }
  }, []);

  const loadMessages = async (conversation, pageNum = 1) => {
    if (!conversation) {
      setMessages([]);
      return;
    }

    try {
      setMessagesLoading(true);
      const response = await getConversationMessages(conversation.id, pageNum, 50);
      const messageData = response.messages || [];
      const paginationData = response.pagination || {};

      const normalized = (messageData || []).map((message) => ({
        id: message.Id || message.id,
        senderId: message.SenderId || message.senderId,
        text: message.Content || message.content,
        timestamp: new Date(message.CreatedAt || message.createdAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }),
        createdAt: new Date(message.CreatedAt || message.createdAt) // Store for sorting
      }));

      const safeSorted = [...normalized].sort(
        (a, b) => a.createdAt.getTime() - b.createdAt.getTime()
      );

      console.log('PAGE:', pageNum);
      console.log('RAW:', normalized.map(m => m.createdAt.toISOString()));

      console.log("PAGE:", pageNum);
      console.log(
        "MIN:",
        safeSorted[0]?.createdAt,
        "MAX:",
        safeSorted[safeSorted.length - 1]?.createdAt
      );

      // CRITICAL: Ensure messages are sorted by CreatedAt ASCENDING (oldest first)
  

      if (pageNum === 1) {
        // 🎯 First load: Set messages in ascending order (oldest first)
        setMessages(safeSorted);
        setPage(1);
        setHasMoreMessages(paginationData.hasMore || false);
        
        // Scroll to bottom after first load
        setTimeout(() => scrollToBottom(), 100);

        if (safeSorted.length > 0) {
          const last = safeSorted[safeSorted.length - 1]; // Get the last (newest) message
          setConversations((prev) => prev.map((item) =>
            item.id === conversation.id
              ? { ...item, lastMessage: last.text, lastTime: last.timestamp, lastMessageAt: last.createdAt.toISOString(), isUnread: false }
              : item
          ));
        }
      } else {
        // 📜 Lazy load: Prepend older messages (sorted ascending) and preserve scroll position
        const currentScrollHeight = messagesAreaRef.current?.scrollHeight || 0;
        
        setMessages((prev) => {
          const existingIds = new Set(prev.map(m => m.id));
          const newMessages = safeSorted.filter(m => !existingIds.has(m.id));

          return [...prev, ...newMessages].sort((a,b) => a.createdAt.getTime() - b.createdAt.getTime());

         
        });

        setPage(pageNum);
        setHasMoreMessages(paginationData.hasMore || false);
        
        // Preserve scroll position
        scrollToPosition(currentScrollHeight);
      }
    } catch (err) {
      console.error('Không thể tải cuộc trò chuyện:', err);
      setMessages([]);
    } finally {
      setMessagesLoading(false);
    }
  };

  const handleSelectConversation = async (conversation) => {
    setSelectedConversation(conversation);
    
    // 🔄 Leave previous conversation group
    if (previousConversationRef.current && messageHubConnection.isActive()) {
      try {
        console.log('[MessagePage] 👋 Leaving previous group:', previousConversationRef.current);
        if (previousConversationRef.current.isGroup) {
          await messageHubConnection.leaveGroupConversation(previousConversationRef.current.id);
        } else {
          await messageHubConnection.leaveConversation(previousConversationRef.current.id);
        }
      } catch (err) {
        console.warn('[MessagePage] ⚠️ Error leaving previous group:', err);
      }
    }

    // 🔄 Join new conversation group
    if (messageHubConnection.isActive()) {
      try {
        if (conversation.isGroup) {
          console.log('[MessagePage] 👥 Joining group conversation:', conversation.id);
          await messageHubConnection.joinGroupConversation(conversation.id);
        } else {
          console.log('[MessagePage] 👥 Joining personal conversation:', conversation.id);
          await messageHubConnection.joinConversation(conversation.id);
        }
        previousConversationRef.current = conversation;
        console.log('[MessagePage] ✅ Joined new conversation');
      } catch (err) {
        console.warn('[MessagePage] ⚠️ Error joining new conversation:', err);
      }
    }

    await loadMessages(conversation, 1);
  };

  const handleSendMessage = async () => {
    if (!newMessage.trim() || !selectedConversation) return;

    try {
      const sentMessage = await sendMessage(selectedConversation.id, newMessage.trim());
      
      console.log('[MessagePage] 📤 API Response:', { 
        Id: sentMessage?.Id, 
        Content: sentMessage?.Content,
        CreatedAt: sentMessage?.CreatedAt 
      });
      
      const nextMessage = {
        id: sentMessage?.Id || sentMessage?.id || messages.length + 1,
        senderId: sentMessage?.SenderId || sentMessage?.senderId || currentUser?.Id || currentUser?.id,
        text: sentMessage?.Content || sentMessage?.content || newMessage.trim(),
        timestamp: new Date(sentMessage?.CreatedAt || Date.now()).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }),
        createdAt: new Date(sentMessage?.CreatedAt || Date.now())
      };
      
      console.log('[MessagePage] 🎯 Adding message to state:', { id: nextMessage.id, text: nextMessage.text });
      
      // Add message to display
      setMessages((prev) => {
        const exists = prev.some(m => {
          const match = String(m.id) === String(nextMessage.id);
          if (match) console.log(`[MessagePage] 🔍 Duplicate detected: ${m.id} === ${nextMessage.id}`);
          return match;
        });
        
        if (exists) {
          console.log('[MessagePage] ⚠️ Message already exists, skipping');
          return prev;
        }

        const updated = [...prev, nextMessage];
        console.log('[MessagePage] ✅ Message added, total messages:', updated.length);

        return updated.sort((a, b) => a.createdAt.getTime() - b.createdAt.getTime());
      });
      
      // Update conversations and re-sort by latest message
      setConversations((prev) => {
        const updated = prev.map((item) =>
          item.id === selectedConversation.id
            ? { ...item, lastMessage: nextMessage.text, lastTime: nextMessage.timestamp, lastMessageAt: nextMessage.createdAt.toISOString(), isUnread: false }
            : item
        );
        return sortConversationsByLatest(updated);
      });
      
      setNewMessage('');
      
      // Always scroll to bottom after sending
      scrollToBottom();
    } catch (err) {
      console.error('Error sending message:', err);
    }
  };

  // 🔄 Effect to listen for incoming messages via SignalR
  useEffect(() => {
    if (!selectedConversation) return;

    console.log('[MessagePage] 🎧 Registering message listener for conversation:', selectedConversation.id);

    // Subscribe to incoming messages
    const unsubscribe = messageHubConnection.onMessage((incomingMessage) => {
      console.log('[MessagePage] 📨 Incoming message from SignalR:', incomingMessage);
      
      // Check if this message belongs to current conversation
      const currentUserId = String(currentUser?.Id ?? currentUser?.id);

      const senderId = String(incomingMessage.senderId ?? incomingMessage.SenderId);
      const receiverId = String(incomingMessage.receiverId ?? incomingMessage.ReceiverId);
      const conversationId = String(selectedConversation.id);

      const isForCurrentConversation =
        (senderId === conversationId && receiverId === currentUserId) ||
        (receiverId === conversationId && senderId === currentUserId);

      console.log('[MessagePage] 🔍 Is for current conversation?', { isForCurrentConversation });

      if (isForCurrentConversation) {
        console.log('[MessagePage] ✅ Adding message to current conversation:', incomingMessage);

        const formattedMessage = {
          id: incomingMessage.id ?? incomingMessage.Id,
          senderId: incomingMessage.senderId ?? incomingMessage.SenderId,
          text: incomingMessage.content ?? incomingMessage.Content,
          timestamp: new Date(incomingMessage.createdAt ?? incomingMessage.CreatedAt)
            .toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' }),
          createdAt: new Date(incomingMessage.createdAt ?? incomingMessage.CreatedAt) // ✅ QUAN TRỌNG
        };
        
        // Add message only if not already in list (to avoid duplicates)
        setMessages((prev) => {
          const exists = prev.some(m => String(m.id) === String(formattedMessage.id));
          if (exists) return prev;

          const updated = [...prev, formattedMessage];

          return updated.sort((a, b) => a.createdAt.getTime() - b.createdAt.getTime());
        });

        // Update last message in conversation list and re-sort
        setConversations((prev) => {
          const updated = prev.map((item) =>
            item.id === selectedConversation.id
              ? { ...item, lastMessage: formattedMessage.text, lastTime: formattedMessage.timestamp, lastMessageAt: formattedMessage.createdAt.toISOString() }
              : item
          );
          return sortConversationsByLatest(updated);
        });
        
        // 🎯 Smart scroll: Only scroll if user is already at bottom
        if (isNearBottomRef.current) {
          console.log('[MessagePage] 📍 User at bottom, scrolling...');
          scrollToBottom();
        } else {
          console.log('[MessagePage] 📖 User reading old messages, NOT scrolling');
        }
      } else {
        console.log('[MessagePage] ⏭️ Message is for different conversation, skipping');
      }
    });

    unsubscribeRef.current = unsubscribe;
    console.log('[MessagePage] ✅ Message listener registered');

    return () => {
      if (unsubscribe) {
        console.log('[MessagePage] 🧹 Unregistering message listener');
        unsubscribe();
      }
    };
  }, [selectedConversation, currentUser, scrollToBottom]);

  // 🟢 Listener for UserOnline/UserOffline events to track user presence
  useEffect(() => {
    const handleUserOnline = (event) => {
      const payload = event.detail || {};
      const userId = payload.userId;
      console.log('[MessagePage] 🟢 User went online:', userId);

      setConversations((prev) => {
        const updated = prev.map((conv) => {
          if (conv.id === userId) {
            console.log(`[MessagePage] ✅ Marking ${conv.name} as online`);
            return { ...conv, isActive: true, lastSeenAt: null };
          }
          return conv;
        });

        // Update online friends list
        const online = updated.filter(c => c.isActive && !c.isGroup);
        setOnlineFriends(online);

        return updated;
      });
    };

    const handleUserOffline = (event) => {
      const payload = event.detail || {};
      const userId = payload.userId;
      const lastActiveAt = payload.lastActiveAt;
      console.log('[MessagePage] 🔴 User went offline:', userId, 'Last active:', lastActiveAt);

      setConversations((prev) => {
        const updated = prev.map((conv) => {
          if (conv.id === userId) {
            console.log(`[MessagePage] ✅ Marking ${conv.name} as offline, lastSeenAt: ${lastActiveAt}`);
            return { ...conv, isActive: false, lastSeenAt: lastActiveAt || new Date().toISOString() };
          }
          return conv;
        });

        // Update online friends list
        const online = updated.filter(c => c.isActive && !c.isGroup);
        setOnlineFriends(online);

        return updated;
      });
    };

    window.addEventListener('signalr:user-online', handleUserOnline);
    window.addEventListener('signalr:user-offline', handleUserOffline);

    return () => {
      window.removeEventListener('signalr:user-online', handleUserOnline);
      window.removeEventListener('signalr:user-offline', handleUserOffline);
    };
  }, []);

  // ⏰ Refresh the display every minute to update "X minutes ago" text
  useEffect(() => {
    const interval = setInterval(() => {
      // Force a re-render by triggering a dummy state update
      // This updates the formatLastSeen() output for all conversations
      setConversations(prev => [...prev]);
    }, 60000); // Update every 60 seconds

    return () => clearInterval(interval);
  }, []);

  // 📜 Debounced scroll handler for lazy loading
  const handleMessagesScroll = useCallback((e) => {
    const element = e.target;
    
    // Update "near bottom" status
    const isNearBottom = element.scrollHeight - (element.scrollTop + element.clientHeight) < SCROLL_THRESHOLD;
    isNearBottomRef.current = isNearBottom;

    // Debounce lazy loading trigger
    if (scrollTimeoutRef.current) {
      clearTimeout(scrollTimeoutRef.current);
    }

    scrollTimeoutRef.current = setTimeout(() => {
      // Trigger lazy load only when at top
      if (element.scrollTop < SCROLL_THRESHOLD && hasMoreMessages && !messagesLoading && selectedConversation) {
        console.log('[MessagePage] 📜 Lazy loading older messages...');
        loadMessages(selectedConversation, page + 1);
      }
    }, DEBOUNCE_DELAY);
  }, [hasMoreMessages, messagesLoading, selectedConversation, page, loadMessages]);

  // 🧹 Cleanup scroll timeout on unmount
  useEffect(() => {
    return () => {
      if (scrollTimeoutRef.current) {
        clearTimeout(scrollTimeoutRef.current);
      }
    };
  }, []);

  const filteredConversations = conversations.filter((conv) =>
    conv.name.toLowerCase().includes(searchQuery.toLowerCase())
  );

  const handleLogout = () => {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    window.dispatchEvent(new Event('tokenUpdated'));
    navigate('/login');
  };

  const handleOpenUserProfile = (userId, userName) => {
    console.log(`[MessagePage] 👤 Navigating to profile: ${userName}`);
    navigate(`/user-profile/${userId}`);
  };

  if (loading) {
    return <div className="message-wrapper"><p>Đang tải...</p></div>;
  }

  if (error) {
    return <div className="message-wrapper"><p style={{color: 'red'}}>{error}</p></div>;
  }

  return (
    <div className="message-wrapper">
      <Header onLogout={handleLogout} />
      <div className="message-container">
        {/* Left Sidebar - Conversations */}
        <aside className="message-sidebar-left">
          <div className="message-search-wrapper">
            <input
              type="text"
              placeholder="Tìm kiếm"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="message-search-input"
            />
            <span className="message-search-icon"><i className="fa-solid fa-magnifying-glass"></i></span>
          </div>

          <div className="message-tabs">
            <button className="message-tab active" type="button">
              Tất cả
            </button>
          </div>

          <div className="conversations-list">
            {filteredConversations.length === 0 ? (
              <p className="no-conversations">Không có cuộc trò chuyện</p>
            ) : (
              filteredConversations.map((conversation) => (
                <div
                  key={conversation.id}
                  className={`conversation-item ${selectedConversation?.id === conversation.id ? 'active' : ''} ${conversation.isUnread ? 'unread' : ''}`}
                  onClick={() => handleSelectConversation(conversation)}
                >
                  <div className="conversation-avatar">
                    {conversation.avatarUrl ? (
                      <img src={conversation.avatarUrl} alt={conversation.name} className="conversation-avatar-img" />
                    ) : (
                      <span className="conversation-avatar-fallback">{conversation.name?.charAt(0)?.toUpperCase() || 'U'}</span>
                    )}
                    {conversation.isActive && <span className="online-status"></span>}
                  </div>
                  <div className="conversation-info">
                    <p className="conversation-name">
                      {conversation.name}
                      {conversation.isGroup && conversation.participantCount && (
                        <span className="participant-count"> ({conversation.participantCount})</span>
                      )}
                    </p>
                    <p className="conversation-last">{conversation.lastMessage}</p>
                  </div>
                  <span className="conversation-time">{conversation.lastTime}</span>
                </div>
              ))
            )}
          </div>
        </aside>

        {/* Main Chat Area */}
        <main className="message-main-content">
          {selectedConversation ? (
            <>
              {/* Chat Header */}
              <div className="message-header">
                <div className="message-header-info">
                  <div className="message-header-avatar">
                    {selectedConversation.avatarUrl ? (
                      <img src={selectedConversation.avatarUrl} alt={selectedConversation.name} className="message-header-avatar-img" />
                    ) : (
                      <span className="conversation-avatar-fallback">{selectedConversation.name?.charAt(0)?.toUpperCase() || 'U'}</span>
                    )}
                    {selectedConversation.isActive && <span className="online-status"></span>}
                  </div>
                  <div>
                    <h3 className="message-header-name">{selectedConversation.name}</h3>
                    <p className="message-header-status">
                      {selectedConversation.isActive
                        ? '🟢 Đang hoạt động'
                        : `${formatLastSeen(selectedConversation.lastSeenAt)}`
                      }
                    </p>
                  </div>
                </div>
              </div>

              {/* Messages Area */}
              <div className="messages-area" ref={messagesAreaRef} onScroll={handleMessagesScroll}>
                {messagesLoading && (
                  <div className="loading-indicator">
                    <span>⏳ Đang tải tin nhắn cũ...</span>
                  </div>
                )}
                {messages.map((message) => {
                  const isSentByCurrentUser = String(message.senderId) === String(currentUser?.Id ?? currentUser?.id);
                  return (
                    <div
                      key={message.id}
                      className={`message-item ${isSentByCurrentUser ? 'sent' : 'received'}`}>
                      {!isSentByCurrentUser && (
                        <div className="message-avatar-small">
                          {selectedConversation?.avatarUrl ? (
                            <img src={selectedConversation.avatarUrl} alt={selectedConversation.name} />
                          ) : (
                            <span className="conversation-avatar-fallback">
                              {selectedConversation?.name?.charAt(0)?.toUpperCase() || 'U'}
                            </span>
                          )}
                        </div>
                      )}
                      <div className={`message-bubble ${isSentByCurrentUser ? 'sent-bubble' : 'received-bubble'}`}>
                        <p>{message.text}</p>
                        <span className="message-time">{message.timestamp}</span>
                      </div>
                    </div>
                  );
                })}
              </div>

              {/* Message Input */}
              <div className="message-input-wrapper">
                <input
                  type="text"
                  placeholder="Nhập tin nhắn..."
                  value={newMessage}
                  onChange={(e) => setNewMessage(e.target.value)}
                  onKeyPress={(e) => e.key === 'Enter' && handleSendMessage()}
                  className="message-input"
                />
                <button onClick={handleSendMessage} className="message-send-btn">
                  ➤
                </button>
              </div>
            </>
          ) : (
            <div className="no-conversation-selected">
              <div className="empty-state">
                <div className="empty-icon">💬</div>
                <h2>Chọn cuộc trò chuyện để bắt đầu</h2>
                <p>Chọn một người bạn từ danh sách bên trái để nhắn tin</p>
              </div>
            </div>
          )}
        </main>

        {/* Right Sidebar - Online Friends */}
        <aside className="message-sidebar-right">
          <div className="friends-header">
            <h3>Bạn bè online</h3>
            <span className="friends-count">{onlineFriends.length}</span>
          </div>
          
          <div className="online-friends-list">
            {onlineFriends.length === 0 ? (
              <p className="no-friends">Không có bạn đang online</p>
            ) : (
              onlineFriends.map((friend) => (
                <div
                  key={friend.id}
                  className="online-friend-item"
                  title={`Xem profile của ${friend.name}`}
                  onClick={() => handleOpenUserProfile(friend.id, friend.name)}
                >
                  <div className="friend-avatar">
                    {friend.avatarUrl ? (
                      <img src={friend.avatarUrl} alt={friend.name} className="friend-avatar-img" />
                    ) : (
                      <span className="friend-avatar-fallback">{friend.name?.charAt(0)?.toUpperCase() || 'U'}</span>
                    )}
                    <span className="online-dot"></span>
                  </div>
                  <span className="friend-name">{friend.name}</span>
                </div>
              ))
            )}
          </div>
        </aside>
      </div>

    </div>
  );
}
