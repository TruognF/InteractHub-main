import * as signalR from "@microsoft/signalr";

let connection = null;
let isConnecting = false;
let handlersBound = false;
const joinedGroups = new Set();

const ensureJoinedRooms = async () => {
  if (!connection || connection.state !== signalR.HubConnectionState.Connected) return;

  try {
    await connection.invoke("JoinFeed");
  } catch (err) {
    console.warn('[PostHub] Failed to re-join feed:', err);
  }

  for (const groupId of joinedGroups) {
    try {
      await connection.invoke("JoinGroup", groupId);
    } catch (err) {
      console.warn(`[PostHub] Failed to re-join group ${groupId}:`, err);
    }
  }
};

function bindRealtimeHandlers(conn) {
  if (handlersBound) return;
  handlersBound = true;

  conn.on("PostCreated", (payload) => {
    window.dispatchEvent(new CustomEvent("signalr:post-created", { detail: payload }));
  });

  conn.on("GroupPostCreated", (payload) => {
    window.dispatchEvent(new CustomEvent("signalr:group-post-created", { detail: payload }));
  });

  conn.on("GroupPostDeleted", (payload) => {
    window.dispatchEvent(new CustomEvent("signalr:group-post-deleted", { detail: payload }));
  });

  conn.on("GroupCommentAdded", (payload) => {
    window.dispatchEvent(new CustomEvent("signalr:group-comment-added", { detail: payload }));
  });

  conn.on("GroupCommentUpdated", (payload) => {
    window.dispatchEvent(new CustomEvent("signalr:group-comment-updated", { detail: payload }));
  });

  conn.on("GroupCommentDeleted", (payload) => {
    window.dispatchEvent(new CustomEvent("signalr:group-comment-deleted", { detail: payload }));
  });

  conn.on("GroupPostLiked", (payload) => {
    window.dispatchEvent(new CustomEvent("signalr:group-post-liked", { detail: payload }));
  });

  conn.on("GroupPostUnliked", (payload) => {
    window.dispatchEvent(new CustomEvent("signalr:group-post-unliked", { detail: payload }));
  });
}

export const startConnection = async () => {
  if (isConnecting || (connection && connection.state === signalR.HubConnectionState.Connected)) {
    console.log('PostHub: Already connected or connecting, skipping');
    return connection;
  }

  isConnecting = true;

  try {
    console.log('[PostHub] 🔗 Starting connection...');
    const token = localStorage.getItem("token");

    if (!token) {
      console.warn('[PostHub] ⚠️ No token found, cannot connect');
      isConnecting = false;
      return null;
    }

    connection = new signalR.HubConnectionBuilder()
      .withUrl("/postHub", {
        accessTokenFactory: () => token,
        withCredentials: true
      })
      .withAutomaticReconnect([0, 1000, 3000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    bindRealtimeHandlers(connection);
    connection.onreconnected(() => {
      ensureJoinedRooms();
    });

    console.log('[PostHub] 📡 Calling connection.start()...');
    await connection.start();
    console.log('[PostHub] ✅ Connected successfully!');

    await ensureJoinedRooms();

    isConnecting = false;
    return connection;
  } catch (err) {
    console.error('[PostHub] ❌ Connection error:', err);
    isConnecting = false;
    connection = null;
    handlersBound = false;
    return null;
  }
};

export function resetPostHubConnection() {
  if (connection) {
    connection.stop().catch(() => {});
  }
  connection = null;
  handlersBound = false;
  joinedGroups.clear();
}

export const getConnection = () => {
  console.log('[PostHub] 🔍 getConnection() called, state:', connection?.state);
  return connection;
};

export async function joinGroupChannel(groupId) {
  if (!groupId) return;
  joinedGroups.add(groupId);
  const conn = await startConnection();
  if (!conn || conn.state !== signalR.HubConnectionState.Connected) return;
  await conn.invoke("JoinGroup", groupId);
}

export async function leaveGroupChannel(groupId) {
  if (!groupId) return;
  joinedGroups.delete(groupId);
  if (!connection || connection.state !== signalR.HubConnectionState.Connected) return;
  await connection.invoke("LeaveGroup", groupId);
}
