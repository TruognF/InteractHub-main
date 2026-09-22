import * as signalR from '@microsoft/signalr';

class MessageHubConnection {
  constructor() {
    this.connection = null;
    this.isConnected = false;
    this.messageListeners = [];
    this.presenceListeners = [];
    this.currentConversationUserId = null;
  }

  /**
   * Establish connection to SignalR MessageHub
   * @param {string} token - JWT token for authentication
   * @returns {Promise<void>}
   */
  async connect(token) {
    if (this.isConnected) {
      console.log('[MessageHub] ✓ Already connected');
      return;
    }

    try {
      console.log('[MessageHub] 🔗 Starting connection to MessageHub...');
      
      this.connection = new signalR.HubConnectionBuilder()
        .withUrl('/messageHub', {
          // Use accessTokenFactory for WebSocket (SignalR will pass token via Authorization header or query string)
          accessTokenFactory: () => {
            console.log('[MessageHub] 🔑 Access token factory called');
            return token;
          },
          skipNegotiation: false,
          withCredentials: true,
          transport: signalR.HttpTransportType.WebSockets
        })
        .withAutomaticReconnect({
          nextRetryDelayInMilliseconds: retryContext => {
            if (retryContext.elapsedMilliseconds < 60000) {
              // Exponential backoff: 0ms, 1s, 3s, 5s, 10s
              return [0, 1000, 3000, 5000, 10000][retryContext.previousRetryCount] || 10000;
            } else {
              return 60000; // After 1 minute, retry every 60 seconds
            }
          }
        })
        .withHubProtocol(new signalR.JsonHubProtocol())
        .configureLogging(signalR.LogLevel.Debug)
        .build();

      console.log('[MessageHub] 📡 Registering event handlers...');

      // Handle incoming messages
      this.connection.on('ReceiveMessage', (message) => {
        console.log('[MessageHub] ✉️ Incoming message received:', message);
        this.messageListeners.forEach(listener => listener(message));
      });

      // Handle user online status
      this.connection.on('UserOnline', (data) => {
        console.log('[MessageHub] 🟢 User online:', data.UserId);
        this.presenceListeners.forEach(listener => listener({ userId: data.UserId, status: 'online' }));
      });

      // Handle user offline status
      this.connection.on('UserOffline', (data) => {
        console.log('[MessageHub] 🔴 User offline:', data.UserId);
        this.presenceListeners.forEach(listener =>
          listener({ userId: data.UserId, status: 'offline', lastSeenAt: data.LastSeenAt || data.lastSeenAt })
        );
      });

      // Handle connection state changes
      this.connection.onreconnecting((error) => {
        console.warn('[MessageHub] ⚠️ Reconnecting...', error?.message);
      });

      this.connection.onreconnected((connectionId) => {
        console.log('[MessageHub] ✅ Reconnected with ID:', connectionId);
        this.isConnected = true;
      });

      this.connection.onclose((error) => {
        console.warn('[MessageHub] ❌ Connection closed:', error?.message);
        this.isConnected = false;
      });

      console.log('[MessageHub] 🚀 Calling start()...');
      await this.connection.start();
      this.isConnected = true;
      console.log('[MessageHub] ✅ Connected successfully! State:', this.connection.state, 'HubConnectionState.Connected:', signalR.HubConnectionState.Connected);
    } catch (error) {
      console.error('[MessageHub] ❌ Connection failed:', error);
      this.isConnected = false;
      throw error;
    }
  }

  /**
   * Disconnect from SignalR MessageHub
   * @returns {Promise<void>}
   */
  async disconnect() {
    if (!this.connection) {
      return;
    }

    try {
      await this.connection.stop();
      this.isConnected = false;
      console.log('MessageHub disconnected');
    } catch (error) {
      console.error('Error disconnecting from MessageHub:', error);
    }
  }

  /**
   * Join a conversation group to receive real-time messages
   * @param {string} userId - The other user's ID in the conversation
   * @returns {Promise<void>}
   */
  async joinConversation(userId) {
    if (!this.connection) {
      console.warn('[MessageHub] ⚠️ Connection not initialized, cannot join conversation');
      return;
    }

    if (!this.isConnected) {
      console.warn('[MessageHub] ⚠️ Not connected, cannot join conversation');
      return;
    }

    // Store the current conversation so we can rejoin after reconnect
    this.currentConversationUserId = userId;

    try {
      console.log('[MessageHub] 📞 Invoking JoinConversation with userId:', userId);
      await this.connection.invoke('JoinConversation', userId);
      console.log('[MessageHub] ✅ Successfully joined conversation for user:', userId);
    } catch (error) {
      console.error('[MessageHub] ❌ Failed to join conversation:', error);
      throw error;
    }
  }

  /**
   * Leave a conversation group
   * @param {string} userId - The other user's ID in the conversation
   * @returns {Promise<void>}
   */
  async leaveConversation(userId) {
    if (!this.connection) {
      console.warn('[MessageHub] ⚠️ Connection not initialized, cannot leave conversation');
      return;
    }

    if (!this.isConnected) {
      console.warn('[MessageHub] ⚠️ Not connected, cannot leave conversation');
      return;
    }

    try {
      console.log('[MessageHub] 📞 Invoking LeaveConversation with userId:', userId);
      await this.connection.invoke('LeaveConversation', userId);
      console.log('[MessageHub] ✅ Successfully left conversation for user:', userId);
    } catch (error) {
      console.error('[MessageHub] ❌ Failed to leave conversation:', error);
      throw error;
    }
  }

  /**
   * Join a group conversation
   * @param {number} groupId - The group ID
   * @returns {Promise<void>}
   */
  async joinGroupConversation(groupId) {
    if (!this.connection) {
      console.warn('[MessageHub] ⚠️ Connection not initialized, cannot join group conversation');
      return;
    }

    if (!this.isConnected) {
      console.warn('[MessageHub] ⚠️ Not connected, cannot join group conversation');
      return;
    }

    try {
      console.log('[MessageHub] 📞 Invoking JoinGroupConversation with groupId:', groupId);
      await this.connection.invoke('JoinGroupConversation', groupId);
      console.log('[MessageHub] ✅ Successfully joined group conversation for group:', groupId);
    } catch (error) {
      console.error('[MessageHub] ❌ Failed to join group conversation:', error);
      throw error;
    }
  }

  /**
   * Leave a group conversation
   * @param {number} groupId - The group ID
   * @returns {Promise<void>}
   */
  async leaveGroupConversation(groupId) {
    if (!this.connection) {
      console.warn('[MessageHub] ⚠️ Connection not initialized, cannot leave group conversation');
      return;
    }

    if (!this.isConnected) {
      console.warn('[MessageHub] ⚠️ Not connected, cannot leave group conversation');
      return;
    }

    try {
      console.log('[MessageHub] 📞 Invoking LeaveGroupConversation with groupId:', groupId);
      await this.connection.invoke('LeaveGroupConversation', groupId);
      console.log('[MessageHub] ✅ Successfully left group conversation for group:', groupId);
    } catch (error) {
      console.error('[MessageHub] ❌ Failed to leave group conversation:', error);
      throw error;
    }
  }

  /**
   * Subscribe to incoming messages
   * @param {Function} listener - Callback function to handle incoming messages
   * @returns {Function} - Function to unsubscribe
   */
  onMessage(listener) {
    console.log('[MessageHub] 📌 Registering message listener');
    this.messageListeners.push(listener);
    
    // Return unsubscribe function
    return () => {
      console.log('[MessageHub] 📌 Unregistering message listener');
      this.messageListeners = this.messageListeners.filter(l => l !== listener);
    };
  }

  /**
   * Subscribe to presence updates (online/offline)
   * @param {Function} listener - Callback function to handle presence changes
   * @returns {Function} - Function to unsubscribe
   */
  onPresence(listener) {
    console.log('[MessageHub] 👥 Registering presence listener');
    this.presenceListeners.push(listener);
    
    // Return unsubscribe function
    return () => {
      console.log('[MessageHub] 👥 Unregistering presence listener');
      this.presenceListeners = this.presenceListeners.filter(l => l !== listener);
    };
  }

  /**
   * Check if connection is active
   * @returns {boolean}
   */
  isActive() {
    const isConnected = this.isConnected && this.connection?.state === signalR.HubConnectionState.Connected;
    console.log('[MessageHub] 🔍 isActive() check:', { isConnected: this.isConnected, state: this.connection?.state, HubConnectionState_Connected: signalR.HubConnectionState.Connected, result: isConnected });
    return isConnected;
  }
}

// Export singleton instance
export const messageHubConnection = new MessageHubConnection();

export default messageHubConnection;
