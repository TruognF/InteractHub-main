import * as signalR from '@microsoft/signalr';

let connection = null;
let isConnecting = false;
let handlersBound = false;

function bindRealtimeHandlers(conn) {
  if (handlersBound) return;
  handlersBound = true;
  conn.on('StoryCreated', (payload) => {
    window.dispatchEvent(new CustomEvent('signalr:story-created', { detail: payload }));
  });
  conn.on('StoryDeleted', (payload) => {
    window.dispatchEvent(new CustomEvent('signalr:story-deleted', { detail: payload }));
  });
}

export const startStoryConnection = async () => {
  if (isConnecting || (connection && connection.state === signalR.HubConnectionState.Connected)) {
    return connection;
  }

  isConnecting = true;

  try {
    const token = localStorage.getItem('token');
    if (!token) {
      isConnecting = false;
      return null;
    }

    connection = new signalR.HubConnectionBuilder()
      .withUrl('/storyHub', {
        accessTokenFactory: () => token,
        withCredentials: true
      })
      .withAutomaticReconnect([0, 1000, 3000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    bindRealtimeHandlers(connection);

    await connection.start();
    await connection.invoke('JoinStoriesFeed');

    isConnecting = false;
    return connection;
  } catch (err) {
    console.error('[StoryHub] connection error:', err);
    isConnecting = false;
    connection = null;
    handlersBound = false;
    return null;
  }
};

/** Reset singleton (e.g. logout) — next start will recreate connection and re-bind handlers. */
export function resetStoryHubConnection() {
  if (connection) {
    connection.stop().catch(() => {});
  }
  connection = null;
  handlersBound = false;
}
