#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Data.Repositories;

using DotnetAuthServer.Domain.Entities;

/// <summary>
/// Repository interface for TOTP credentials.
/// </summary>
public interface ITotpCredentialRepository : IRepository<TotpCredential, string>
{
    /// <summary>
    /// Returns the TOTP credential for the given user, or null if one has not been set up.
    /// </summary>
    Task<TotpCredential?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the TOTP credential for the given user, if one exists.
    /// </summary>
    Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of <see cref="ITotpCredentialRepository"/>.
/// </summary>
public sealed class TotpCredentialRepository : ITotpCredentialRepository
{
    private readonly Dictionary<string, TotpCredential> _credentials = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public Task<TotpCredential?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(_credentials.TryGetValue(id, out var c) ? c : null);

    /// <inheritdoc />
    public Task<IEnumerable<TotpCredential>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<TotpCredential>>(_credentials.Values.ToList());

    /// <inheritdoc />
    public Task<TotpCredential> CreateAsync(TotpCredential entity, CancellationToken cancellationToken = default)
    {
        if (_credentials.ContainsKey(entity.Id))
            throw new InvalidOperationException($"TOTP credential {entity.Id} already exists");

        _credentials[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task<TotpCredential> UpdateAsync(TotpCredential entity, CancellationToken cancellationToken = default)
    {
        if (!_credentials.ContainsKey(entity.Id))
            throw new InvalidOperationException($"TOTP credential {entity.Id} not found");

        _credentials[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    /// <inheritdoc />
    public Task DeleteAsync(TotpCredential entity, CancellationToken cancellationToken = default)
        => DeleteByIdAsync(entity.Id, cancellationToken);

    /// <inheritdoc />
    public Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _credentials.Remove(id);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
        => Task.FromResult(_credentials.ContainsKey(id));

    /// <inheritdoc />
    public Task<TotpCredential?> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var credential = _credentials.Values
            .FirstOrDefault(c => c.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(credential);
    }

    /// <inheritdoc />
    public Task DeleteByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var toRemove = _credentials.Values
            .Where(c => c.UserId.Equals(userId, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Id)
            .ToList();

        foreach (var id in toRemove)
            _credentials.Remove(id);

        return Task.CompletedTask;
    }
}
