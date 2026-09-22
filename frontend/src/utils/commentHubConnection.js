import * as signalR from '@microsoft/signalr';

class CommentHubConnection {
  constructor() {
    this.connection = null;
    this.isConnected = false;
    this.commentCreatedListeners = [];
    this.commentUpdatedListeners = [];
    this.commentDeletedListeners = [];
    this.joinedGroups = new Set();
  }

  async connect(token) {
    if (this.isConnected) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/commentHub', {
        accessTokenFactory: () => token,
        skipNegotiation: false,
        withCredentials: true,
        transport: signalR.HttpTransportType.WebSockets
      })
      .withAutomaticReconnect({
        nextRetryDelayInMilliseconds: retryContext => {
          if (retryContext.elapsedMilliseconds < 60000) {
            return [0, 1000, 3000, 5000, 10000][retryContext.previousRetryCount] || 10000;
          }
          return 60000;
        }
      })
      .withHubProtocol(new signalR.JsonHubProtocol())
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    this.connection.on('ReceiveCommentCreated', comment => {
      this.commentCreatedListeners.forEach(listener => listener(comment));
    });

    this.connection.on('ReceiveCommentUpdated', comment => {
      this.commentUpdatedListeners.forEach(listener => listener(comment));
    });

    this.connection.on('ReceiveCommentDeleted', payload => {
      this.commentDeletedListeners.forEach(listener => listener(payload));
    });

    this.connection.onreconnecting(error => {
      console.warn('[CommentHub] Reconnecting...', error?.message);
    });

    this.connection.onreconnected(connectionId => {
      console.log('[CommentHub] Reconnected with ID:', connectionId);
      this.isConnected = true;
      this.joinedGroups.forEach(async (postId) => {
        try {
          await this.joinPostGroup(postId);
        } catch (err) {
          console.error('[CommentHub] Failed to rejoin post group:', postId, err);
        }
      });
    });

    this.connection.onclose(error => {
      console.warn('[CommentHub] Connection closed:', error?.message);
      this.isConnected = false;
    });

    try {
      await this.connection.start();
      this.isConnected = true;
      console.log('[CommentHub] Connected successfully');
    } catch (error) {
      this.isConnected = false;
      console.error('[CommentHub] Connection failed:', error);
      throw error;
    }
  }

  async disconnect() {
    if (!this.connection) return;

    try {
      await this.connection.stop();
      this.isConnected = false;
      this.joinedGroups.clear();
    } catch (error) {
      console.error('[CommentHub] Error disconnecting:', error);
    }
  }

  async joinPostGroup(postId) {
    if (!this.connection || !this.isConnected) {
      throw new Error('CommentHub connection is not established');
    }

    await this.connection.invoke('JoinPostGroup', postId);
    this.joinedGroups.add(postId);
  }

  async leavePostGroup(postId) {
    if (!this.connection || !this.isConnected) {
      return;
    }

    await this.connection.invoke('LeavePostGroup', postId);
    this.joinedGroups.delete(postId);
  }

  onCommentCreated(listener) {
    this.commentCreatedListeners.push(listener);
    return () => {
      this.commentCreatedListeners = this.commentCreatedListeners.filter(l => l !== listener);
    };
  }

  onCommentUpdated(listener) {
    this.commentUpdatedListeners.push(listener);
    return () => {
      this.commentUpdatedListeners = this.commentUpdatedListeners.filter(l => l !== listener);
    };
  }

  onCommentDeleted(listener) {
    this.commentDeletedListeners.push(listener);
    return () => {
      this.commentDeletedListeners = this.commentDeletedListeners.filter(l => l !== listener);
    };
  }

  isActive() {
    return this.isConnected && this.connection?.state === signalR.HubConnectionState.Connected;
  }
}

export const commentHubConnection = new CommentHubConnection();
export default commentHubConnection;
