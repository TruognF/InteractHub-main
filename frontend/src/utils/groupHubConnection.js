import * as signalR from "@microsoft/signalr";

let connection = null;
let isConnecting = false;
let handlersBound = false;

function bindRealtimeHandlers(conn) {
  if (handlersBound) return;
  handlersBound = true;
  conn.on("GroupCreated", (payload) => {
    window.dispatchEvent(new CustomEvent("signalr:group-created", { detail: payload }));
  });
  conn.on("GroupUpdated", (groupId) => {
    window.dispatchEvent(new CustomEvent("signalr:group-updated", { detail: groupId }));
  });
  conn.on("GroupMemberCountUpdated", (payload) => {
    console.log("[GroupHub] 📊 GroupMemberCountUpdated received:", payload);
    window.dispatchEvent(new CustomEvent("signalr:group-member-count-updated", { detail: payload }));
  });
}

export const startConnection = async () => {
  if (isConnecting || (connection && connection.state === signalR.HubConnectionState.Connected)) {
    return connection;
  }

  isConnecting = true;

  try {
    const token = localStorage.getItem("token");

    if (!token) {
      isConnecting = false;
      return null;
    }

    connection = new signalR.HubConnectionBuilder()
      .withUrl("/groupHub", {
        accessTokenFactory: () => token,
        withCredentials: true
      })
      .withAutomaticReconnect([0, 1000, 3000, 5000, 10000])
      .configureLogging(signalR.LogLevel.Information)
      .build();

    bindRealtimeHandlers(connection);

    await connection.start();

    isConnecting = false;
    return connection;
  } catch (err) {
    console.error('[GroupHub] Connection error:', err);
    isConnecting = false;
    connection = null;
    handlersBound = false;
    return null;
  }
};

export function resetGroupHubConnection() {
  if (connection) {
    connection.stop().catch(() => {});
  }
  connection = null;
  handlersBound = false;
}

export const getConnection = () => {
  return connection;
};
