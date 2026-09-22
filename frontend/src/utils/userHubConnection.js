import * as signalR from "@microsoft/signalr";

let connection = null;
let isConnecting = false;
let handlersBound = false;

function bindRealtimeHandlers(conn) {
  if (handlersBound) return;
  handlersBound = true;

  conn.on("UserProfileUpdated", (userData) => {
    console.log("[UserHub] 📢 UserProfileUpdated received:", userData);
    window.dispatchEvent(new CustomEvent("signalr:user-profile-updated", { detail: userData }));
  });

  conn.on("UserOnline", (payload) => {
    console.log("[UserHub] 🟢 UserOnline received:", payload);
    window.dispatchEvent(new CustomEvent("signalr:user-online", { detail: payload }));
  });

  conn.on("UserOffline", (payload) => {
    console.log("[UserHub] 🔴 UserOffline received:", payload);
    window.dispatchEvent(new CustomEvent("signalr:user-offline", { detail: payload }));
  });
}

export const startConnection = async () => {
  if (isConnecting || (connection && connection.state === signalR.HubConnectionState.Connected)) {
    console.log('[UserHub] Already connected or connecting, skipping');
    return connection;
  }

  isConnecting = true;

  try {
    console.log('[UserHub] 🔗 Starting connection...');
    const token = localStorage.getItem("token");

    if (!token) {
      console.warn('[UserHub] ⚠️ No token found, cannot connect');
      isConnecting = false;
      return null;
    }

    connection = new signalR.HubConnectionBuilder()
      .withUrl("/userHub", {
        accessTokenFactory: () => token,
        withCredentials: true
      })
      .withAutomaticReconnect([0, 1000, 3000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    bindRealtimeHandlers(connection);

    console.log('[UserHub] 📡 Calling connection.start()...');
    await connection.start();
    console.log('[UserHub] ✅ Connected successfully!');

    isConnecting = false;
    return connection;
  } catch (err) {
    console.error('[UserHub] ❌ Connection error:', err);
    isConnecting = false;
    connection = null;
    handlersBound = false;
    return null;
  }
};

export function resetUserHubConnection() {
  if (connection) {
    connection.stop().catch(() => {});
  }
  connection = null;
  handlersBound = false;
}

export const getConnection = () => {
  console.log('[UserHub] 🔍 getConnection() called, state:', connection?.state);
  return connection;
};
