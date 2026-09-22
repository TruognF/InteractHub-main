namespace InteractHub.Application.Interfaces;

public interface IUserPresenceService
{
    bool UserConnected(string userId);
    (bool BecameOffline, DateTime LastSeenAtUtc) UserDisconnected(string userId);
    bool IsOnline(string userId);
    DateTime? GetLastSeenAtUtc(string userId);
}
