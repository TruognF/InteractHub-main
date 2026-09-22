/**
 * SignalR JSON uses camelCase; REST uses PascalCase — normalize for UI state.
 */
export function normalizeNotificationPayload(raw) {
  if (!raw) return null;
  const id = raw.Id ?? raw.id;
  if (id == null) return null;
  const type = String(raw.Type ?? raw.type ?? '');
  return {
    Id: id,
    Content: raw.Content ?? raw.content ?? '',
    IsRead: raw.IsRead ?? raw.isRead ?? false,
    Type: type,
    UserId: raw.UserId ?? raw.userId,
    RelatedUserId: raw.RelatedUserId ?? raw.relatedUserId ?? null,
    RelatedEntityId: raw.RelatedEntityId ?? raw.relatedEntityId ?? null,
    CreatedAt: raw.CreatedAt ?? raw.createdAt
  };
}

export function isMessageNotificationType(type) {
  return String(type || '').toLowerCase() === 'message';
}
