#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Data.Repositories;

using DotnetAuthServer.Domain.Entities;

/// <summary>
/// Repository interface for managing authenticated user sessions.
/// </summary>
public interface IUserSessionRepository : IRepository<UserSession, string>
{
    /// <summary>
    /// Returns all sessions (active and revoked) belonging to a specific user.
    /// </summary>
    Task<IEnumerable<UserSession>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns only active (non-revoked, non-expired) sessions for a user.
    /// </summary>
    Task<IEnumerable<UserSession>> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all active sessions across all users.
    /// </summary>
    Task<IEnumerable<UserSession>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active sessions for a given user with an optional reason.
    /// </summary>
    Task<int> RevokeAllUserSessionsAsync(string userId, string? reason = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes sessions that have expired beyond the given threshold.
    /// </summary>
    Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of <see cref="IUserSessionRepository"/>.
/// All data is scoped to the lifetime of the process; replace with a persistent
/// store (e.g. database) for production use.
/// </summary>
public sealed class UserSessionRepository : IUserSessionRepository
{
    private readonly Dictionary<string, UserSession> _sessions = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<UserSession?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(_sessions.TryGetValue(id, out var s) ? s : null);

    /// <inheritdoc />
    public Task<IEnumerable<UserSession>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<UserSession>>(_sessions.Values.ToList());

    /// <inheritdoc />
    public Task<UserSession> CreateAsync(UserSession entity, CancellationToken cancellationToken = default)
    {
        if (_sessions.ContainsKey(entity.SessionId))
            throw new InvalidOperationException($"Session {entity.SessionId} already exists");

        _sessions[entity.SessionId] = entity;
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task<UserSession> UpdateAsync(UserSession entity, CancellationToken cancellationToken = default)
    {
        if (!_sessions.ContainsKey(entity.SessionId))
            throw new InvalidOperationException($"Session {entity.SessionId} not found");

        _sessions[entity.SessionId] = entity;
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task DeleteAsync(UserSession entity, CancellationToken cancellationToken = default)
        => DeleteByIdAsync(entity.SessionId, cancellationToken);

    /// <inheritdoc />
    public Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _sessions.Remove(id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(_sessions.ContainsKey(id));

    /// <inheritdoc />
    public Task<IEnumerable<UserSession>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var result = _sessions.Values
            .Where(s => s.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult<IEnumerable<UserSession>>(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<UserSession>> GetActiveByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var result = _sessions.Values
            .Where(s => s.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase) && s.IsActive())
            .ToList();
        return Task.FromResult<IEnumerable<UserSession>>(result);
    }

    /// <inheritdoc />
    public Task<IEnumerable<UserSession>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        var result = _sessions.Values.Where(s => s.IsActive()).ToList();
        return Task.FromResult<IEnumerable<UserSession>>(result);
    }

    /// <inheritdoc />
    public Task<int> RevokeAllUserSessionsAsync(string userId, string? reason = null, CancellationToken cancellationToken = default)
    {
        var toRevoke = _sessions.Values
            .Where(s => s.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase) && s.IsActive())
            .ToList();

        foreach (var session in toRevoke)
            session.Revoke(reason);

        return Task.FromResult(toRevoke.Count);
    }

    /// <inheritdoc />
    public Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expired = _sessions.Values
            .Where(s => s.ExpiresAt <= DateTime.UtcNow)
            .Select(s => s.SessionId)
            .ToList();

        foreach (var id in expired)
            _sessions.Remove(id);

        return Task.FromResult(expired.Count);
    }
}
