/** Chuẩn hóa post từ SignalR PostHub (camelCase) để khớp shape API (PascalCase). */
export function normalizeFeedPostFromRealtime(raw) {
  if (!raw) return null;
  const id = raw.Id ?? raw.id;
  const userId = raw.UserId ?? raw.userId;
  if (id == null || userId == null) return null;

  const shared = raw.SharedPost ?? raw.sharedPost;
  let SharedPost = null;
  if (shared && (shared.Id ?? shared.id) != null) {
    const sid = shared.Id ?? shared.id;
    SharedPost = {
      Id: sid,
      Content: shared.Content ?? shared.content ?? '',
      ImageUrl: shared.ImageUrl ?? shared.imageUrl ?? null,
      CreatedAt: shared.CreatedAt ?? shared.createdAt ?? null,
      UpdatedAt: shared.UpdatedAt ?? shared.updatedAt ?? null,
      UserId: shared.UserId ?? shared.userId,
      UserName: shared.UserName ?? shared.userName ?? null,
      UserFullName: shared.UserFullName ?? shared.userFullName ?? null,
      UserProfilePictureUrl: shared.UserProfilePictureUrl ?? shared.userProfilePictureUrl ?? null,
      LikesCount: shared.LikesCount ?? shared.likesCount ?? 0,
      CommentsCount: shared.CommentsCount ?? shared.commentsCount ?? 0
    };
  }

  return {
    Id: id,
    GroupId: raw.GroupId ?? raw.groupId ?? null,
    Content: raw.Content ?? raw.content ?? '',
    ImageUrl: raw.ImageUrl ?? raw.imageUrl ?? null,
    CreatedAt: raw.CreatedAt ?? raw.createdAt ?? null,
    UpdatedAt: raw.UpdatedAt ?? raw.updatedAt ?? null,
    UserId: userId,
    UserName: raw.UserName ?? raw.userName ?? null,
    UserFullName: raw.UserFullName ?? raw.userFullName ?? null,
    UserProfilePictureUrl: raw.UserProfilePictureUrl ?? raw.userProfilePictureUrl ?? null,
    LikesCount: raw.LikesCount ?? raw.likesCount ?? 0,
    CommentsCount: raw.CommentsCount ?? raw.commentsCount ?? 0,
    LikedByUserIds: raw.LikedByUserIds ?? raw.likedByUserIds ?? [],
    IsShared: raw.IsShared ?? raw.isShared ?? !!SharedPost,
    SharedPostId: raw.SharedPostId ?? raw.sharedPostId ?? null,
    SharedPost
  };
}
