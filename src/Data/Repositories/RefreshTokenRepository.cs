#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Data.Repositories;

using System.Collections.Concurrent;
using DotnetAuthServer.Domain.Entities;

/// <summary>
/// Repository for RefreshToken entity operations
/// </summary>
public interface IRefreshTokenRepository : IRepository<RefreshToken, string>
{
    /// <summary>
    /// Gets a refresh token by token hash
    /// </summary>
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all refresh tokens for a specific user
    /// </summary>
    Task<IEnumerable<RefreshToken>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all refresh tokens for a specific client
    /// </summary>
    Task<IEnumerable<RefreshToken>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all valid (non-revoked, non-expired) tokens for a user
    /// </summary>
    Task<IEnumerable<RefreshToken>> GetValidTokensByUserAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all refresh tokens for a user (e.g., after password change)
    /// </summary>
    Task RevokeAllUserTokensAsync(string userId, string reason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired refresh tokens
    /// </summary>
    Task DeleteExpiredAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of refresh token repository
/// </summary>
public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ConcurrentDictionary<string, RefreshToken> _tokens = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a refresh token by its ID, or null if it does not exist.
    /// </summary>
    public async Task<RefreshToken?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        return await Task.FromResult(_tokens.TryGetValue(id, out var token) ? token : null);
    }

    /// <summary>
    /// Gets all refresh tokens.
    /// </summary>
    public async Task<IEnumerable<RefreshToken>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_tokens.Values.ToList());
    }

    /// <summary>
    /// Adds a new refresh token; throws if a token with the same ID already exists.
    /// </summary>
    public async Task<RefreshToken> CreateAsync(RefreshToken entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (!_tokens.TryAdd(entity.TokenId, entity))
            throw new InvalidOperationException($"Token with ID {entity.TokenId} already exists");
        return await Task.FromResult(entity);
    }

    /// <summary>
    /// Replaces an existing refresh token and stamps its UpdatedAt time; throws if it does not exist.
    /// </summary>
    public async Task<RefreshToken> UpdateAsync(RefreshToken entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (!_tokens.ContainsKey(entity.TokenId))
            throw new InvalidOperationException($"Token with ID {entity.TokenId} not found");

        entity.UpdatedAt = DateTime.UtcNow;
        _tokens[entity.TokenId] = entity;
        return await Task.FromResult(entity);
    }

    /// <summary>
    /// Deletes the specified refresh token.
    /// </summary>
    public async Task DeleteAsync(RefreshToken entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await DeleteByIdAsync(entity.TokenId, cancellationToken);
    }

    /// <summary>
    /// Deletes the refresh token with the given ID, if present.
    /// </summary>
    public Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        _tokens.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks whether a refresh token with the given ID exists.
    /// </summary>
    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        return await Task.FromResult(_tokens.ContainsKey(id));
    }

    /// <summary>
    /// Gets the refresh token with the given hash, or null if none matches.
    /// </summary>
    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(tokenHash);
        var token = _tokens.Values.FirstOrDefault(t =>
            t.TokenHash.Equals(tokenHash, StringComparison.OrdinalIgnoreCase));
        return await Task.FromResult(token);
    }

    /// <summary>
    /// Gets all refresh tokens issued to the specified user.
    /// </summary>
    public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        var tokens = _tokens.Values.Where(t =>
            t.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase)).ToList();
        return await Task.FromResult(tokens);
    }

    /// <summary>
    /// Gets all refresh tokens issued to the specified client.
    /// </summary>
    public async Task<IEnumerable<RefreshToken>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(clientId);
        var tokens = _tokens.Values.Where(t =>
            t.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase)).ToList();
        return await Task.FromResult(tokens);
    }

    /// <summary>
    /// Gets the user's refresh tokens that are currently valid.
    /// </summary>
    public async Task<IEnumerable<RefreshToken>> GetValidTokensByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        var tokens = _tokens.Values.Where(t =>
            t.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase) &&
            t.IsValid()).ToList();
        return await Task.FromResult(tokens);
    }

    /// <summary>
    /// Revokes all refresh tokens of the specified user, recording the given reason.
    /// </summary>
    public Task RevokeAllUserTokensAsync(string userId, string reason, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(userId);
        ArgumentException.ThrowIfNullOrEmpty(reason);
        var userTokens = _tokens.Values.Where(t =>
            t.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var token in userTokens)
        {
            token.Revoke(reason);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Deletes all refresh tokens that have expired.
    /// </summary>
    public Task DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expiredIds = _tokens.Values
            .Where(t => t.IsExpired())
            .Select(t => t.TokenId)
            .ToList();

        foreach (var id in expiredIds)
        {
            _tokens.TryRemove(id, out _);
        }

        return Task.CompletedTask;
    }
}