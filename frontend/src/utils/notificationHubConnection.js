import * as signalR from "@microsoft/signalr";

let connection = null;
let isConnecting = false;
let handlersBound = false;

function bindRealtimeHandlers(conn) {
  if (handlersBound) return;
  handlersBound = true;

  conn.on("FriendRequestReceived", (friendshipData) => {
    console.log("[NotificationHub] 📬 FriendRequestReceived:", friendshipData);
    window.dispatchEvent(new CustomEvent("signalr:friend-request-received", { detail: friendshipData }));
  });

  conn.on("FriendRequestAccepted", (friendshipData) => {
    console.log("[NotificationHub] ✅ FriendRequestAccepted:", friendshipData);
    window.dispatchEvent(new CustomEvent("signalr:friend-request-accepted", { detail: friendshipData }));
  });

  conn.on("FriendRequestDeclined", (friendshipData) => {
    console.log("[NotificationHub] ❌ FriendRequestDeclined:", friendshipData);
    window.dispatchEvent(new CustomEvent("signalr:friend-request-declined", { detail: friendshipData }));
  });
}

export const startConnection = async () => {
  if (isConnecting || (connection && connection.state === signalR.HubConnectionState.Connected)) {
    console.log('[NotificationHub] Already connected or connecting, skipping');
    return connection;
  }

  isConnecting = true;

  try {
    console.log('[NotificationHub] 🔗 Starting connection...');
    const token = localStorage.getItem("token");

    if (!token) {
      console.warn('[NotificationHub] ⚠️ No token found, cannot connect');
      isConnecting = false;
      return null;
    }

    connection = new signalR.HubConnectionBuilder()
      .withUrl("/notificationHub", {
        accessTokenFactory: () => token,
        withCredentials: true
      })
      .withAutomaticReconnect([0, 1000, 3000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    bindRealtimeHandlers(connection);

    console.log('[NotificationHub] 📡 Calling connection.start()...');
    await connection.start();
    console.log('[NotificationHub] ✅ Connected successfully!');

    const userId = localStorage.getItem("userId");
    if (userId) {
      await connection.invoke("JoinNotificationsGroup", userId).catch(err => {
        console.warn('[NotificationHub] Failed to join notifications group:', err);
      });
    }

    isConnecting = false;
    return connection;
  } catch (err) {
    console.error('[NotificationHub] ❌ Connection error:', err);
    isConnecting = false;
    connection = null;
    handlersBound = false;
    return null;
  }
};

export function resetConnection() {
  if (connection) {
    connection.stop().catch(() => {});
  }
  connection = null;
  handlersBound = false;
}

export const getConnection = () => {
  console.log('[NotificationHub] 🔍 getConnection() called, state:', connection?.state);
  return connection;
};
