using System.Collections.Concurrent;
using InteractHub.Application.Interfaces;

namespace InteractHub.Infrastructure.Service;

public class UserPresenceService : IUserPresenceService
{
    private readonly ConcurrentDictionary<string, int> _connections = new();
    private readonly ConcurrentDictionary<string, DateTime> _lastSeenAtUtc = new();

    public bool UserConnected(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        var next = _connections.AddOrUpdate(userId, 1, (_, current) => current + 1);
        return next == 1;
    }

    public (bool BecameOffline, DateTime LastSeenAtUtc) UserDisconnected(string userId)
    {
        var now = DateTime.UtcNow;
        if (string.IsNullOrWhiteSpace(userId))
            return (false, now);

        if (!_connections.TryGetValue(userId, out var current))
            return (false, now);

        if (current <= 1)
        {
            _connections.TryRemove(userId, out _);
            _lastSeenAtUtc[userId] = now;
            return (true, now);
        }

        _connections[userId] = current - 1;
        return (false, now);
    }

    public bool IsOnline(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return false;

        return _connections.TryGetValue(userId, out var count) && count > 0;
    }

    public DateTime? GetLastSeenAtUtc(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return null;

        return _lastSeenAtUtc.TryGetValue(userId, out var lastSeen) ? lastSeen : null;
    }
}
