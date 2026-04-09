// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Data.Repositories;

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
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly Dictionary<string, RefreshToken> _tokens = new(StringComparer.OrdinalIgnoreCase);

    public async Task<RefreshToken?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_tokens.TryGetValue(id, out var token) ? token : null);
    }

    public async Task<IEnumerable<RefreshToken>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_tokens.Values.ToList());
    }

    public async Task<RefreshToken> CreateAsync(RefreshToken entity, CancellationToken cancellationToken = default)
    {
        if (_tokens.ContainsKey(entity.TokenId))
            throw new InvalidOperationException($"Token with ID {entity.TokenId} already exists");

        _tokens[entity.TokenId] = entity;
        return await Task.FromResult(entity);
    }

    public async Task<RefreshToken> UpdateAsync(RefreshToken entity, CancellationToken cancellationToken = default)
    {
        if (!_tokens.ContainsKey(entity.TokenId))
            throw new InvalidOperationException($"Token with ID {entity.TokenId} not found");

        entity.UpdatedAt = DateTime.UtcNow;
        _tokens[entity.TokenId] = entity;
        return await Task.FromResult(entity);
    }

    public async Task DeleteAsync(RefreshToken entity, CancellationToken cancellationToken = default)
    {
        await DeleteByIdAsync(entity.TokenId, cancellationToken);
    }

    public async Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _tokens.Remove(id);
        return await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_tokens.ContainsKey(id));
    }

    public async Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var token = _tokens.Values.FirstOrDefault(t =>
            t.TokenHash.Equals(tokenHash, StringComparison.OrdinalIgnoreCase));
        return await Task.FromResult(token);
    }

    public async Task<IEnumerable<RefreshToken>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var tokens = _tokens.Values.Where(t =>
            t.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase)).ToList();
        return await Task.FromResult(tokens);
    }

    public async Task<IEnumerable<RefreshToken>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var tokens = _tokens.Values.Where(t =>
            t.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase)).ToList();
        return await Task.FromResult(tokens);
    }

    public async Task<IEnumerable<RefreshToken>> GetValidTokensByUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var tokens = _tokens.Values.Where(t =>
            t.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase) &&
            t.IsValid()).ToList();
        return await Task.FromResult(tokens);
    }

    public async Task RevokeAllUserTokensAsync(string userId, string reason, CancellationToken cancellationToken = default)
    {
        var userTokens = _tokens.Values.Where(t =>
            t.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase)).ToList();

        foreach (var token in userTokens)
        {
            token.Revoke(reason);
        }

        return await Task.CompletedTask;
    }

    public async Task DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expiredIds = _tokens.Values
            .Where(t => t.IsExpired())
            .Select(t => t.TokenId)
            .ToList();

        foreach (var id in expiredIds)
        {
            _tokens.Remove(id);
        }

        return await Task.CompletedTask;
    }
}
