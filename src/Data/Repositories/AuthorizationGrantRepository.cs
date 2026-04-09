// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Data.Repositories;

using DotnetAuthServer.Domain.Entities;

/// <summary>
/// Repository for AuthorizationGrant entity operations
/// </summary>
public interface IAuthorizationGrantRepository : IRepository<AuthorizationGrant, string>
{
    /// <summary>
    /// Gets a grant by authorization code
    /// </summary>
    Task<AuthorizationGrant?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all grants for a specific user
    /// </summary>
    Task<IEnumerable<AuthorizationGrant>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all grants for a specific client
    /// </summary>
    Task<IEnumerable<AuthorizationGrant>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes expired grants
    /// </summary>
    Task DeleteExpiredAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of authorization grant repository
/// </summary>
public class AuthorizationGrantRepository : IAuthorizationGrantRepository
{
    private readonly Dictionary<string, AuthorizationGrant> _grants = new(StringComparer.OrdinalIgnoreCase);

    public async Task<AuthorizationGrant?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_grants.TryGetValue(id, out var grant) ? grant : null);
    }

    public async Task<IEnumerable<AuthorizationGrant>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_grants.Values.ToList());
    }

    public async Task<AuthorizationGrant> CreateAsync(AuthorizationGrant entity, CancellationToken cancellationToken = default)
    {
        if (_grants.ContainsKey(entity.GrantId))
            throw new InvalidOperationException($"Grant with ID {entity.GrantId} already exists");

        _grants[entity.GrantId] = entity;
        return await Task.FromResult(entity);
    }

    public async Task<AuthorizationGrant> UpdateAsync(AuthorizationGrant entity, CancellationToken cancellationToken = default)
    {
        if (!_grants.ContainsKey(entity.GrantId))
            throw new InvalidOperationException($"Grant with ID {entity.GrantId} not found");

        _grants[entity.GrantId] = entity;
        return await Task.FromResult(entity);
    }

    public async Task DeleteAsync(AuthorizationGrant entity, CancellationToken cancellationToken = default)
    {
        await DeleteByIdAsync(entity.GrantId, cancellationToken);
    }

    public async Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _grants.Remove(id);
        return await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_grants.ContainsKey(id));
    }

    public async Task<AuthorizationGrant?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var grant = _grants.Values.FirstOrDefault(g =>
            g.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
        return await Task.FromResult(grant);
    }

    public async Task<IEnumerable<AuthorizationGrant>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var grants = _grants.Values.Where(g =>
            g.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase)).ToList();
        return await Task.FromResult(grants);
    }

    public async Task<IEnumerable<AuthorizationGrant>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var grants = _grants.Values.Where(g =>
            g.ClientId.Equals(clientId, StringComparison.OrdinalIgnoreCase)).ToList();
        return await Task.FromResult(grants);
    }

    public async Task DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var expiredIds = _grants.Values
            .Where(g => g.IsExpired())
            .Select(g => g.GrantId)
            .ToList();

        foreach (var id in expiredIds)
        {
            _grants.Remove(id);
        }

        return await Task.CompletedTask;
    }
}
