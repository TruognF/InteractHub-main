import { useEffect } from 'react';
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { normalizeNotificationPayload, isMessageNotificationType } from '../utils/notificationPayload';

/**
 * Một kết nối notificationHub cho toàn app (JWT auto-join group trên server).
 * Phát sự kiện DOM để HomePage (và màn khác nếu cần) cập nhật danh sách / badge.
 */
export default function NotificationHubBridge({ token }) {
  useEffect(() => {
    if (!token) return undefined;

    const connection = new HubConnectionBuilder()
      .withUrl('/notificationHub', { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    connection.on('ReceiveNotification', (raw) => {
      const notification = normalizeNotificationPayload(raw);
      if (!notification) return;

      window.dispatchEvent(
        new CustomEvent('signalr:notification', { detail: notification })
      );

      if (isMessageNotificationType(notification.Type)) {
        window.dispatchEvent(new CustomEvent('signalr:message-unread'));
      }
    });

    connection
      .start()
      .then(() => {
        const userJson = localStorage.getItem('user');
        if (userJson) {
          try {
            const u = JSON.parse(userJson);
            const uid = u?.Id ?? u?.id;
            if (uid) {
              connection.invoke('JoinNotificationsGroup', uid).catch(() => {});
            }
          } catch {
            /* ignore */
          }
        }
      })
      .catch((err) => console.error('NotificationHub connection error:', err));

    return () => {
      connection.stop().catch(() => {});
    };
  }, [token]);

  return null;
}
