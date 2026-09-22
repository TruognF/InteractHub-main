/** Chuẩn hóa story từ SignalR (camelCase) hoặc REST (PascalCase). */
export function normalizeStoryPayload(p) {
  if (!p) return null;
  const id = p.Id ?? p.id;
  const userId = p.UserId ?? p.userId;
  if (id == null || !userId) return null;

  const expireRaw = p.ExpireAt ?? p.expireAt;

  return {
    Id: id,
    UserId: userId,
    Content: p.Content ?? p.content ?? '',
    ImageUrl: p.ImageUrl ?? p.imageUrl ?? null,
    CreatedAt: p.CreatedAt ?? p.createdAt ?? null,
    ExpireAt: expireRaw != null ? expireRaw : null,
    UserName: p.UserName ?? p.userName ?? '',
    UserProfilePictureUrl: p.UserProfilePictureUrl ?? p.userProfilePictureUrl ?? null
  };
}

export function isStoryActive(story) {
  if (!story) return false;
  const raw = story.ExpireAt ?? story.expireAt;
  if (!raw) return true;
  return new Date(raw) > new Date();
}
