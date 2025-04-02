// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Data.Repositories;

using System.Collections.Generic;
using DotnetAuthServer.Domain.Entities;

/// <summary>
/// In-memory repository implementation for consent records.
/// Stores user consent data for audit and permission tracking.
/// In production, should be persisted to a database.
/// </summary>
public class ConsentRepository : IConsentRepository
{
    private readonly Dictionary<string, Consent> _consents = new();

    public Task<Consent?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _consents.TryGetValue(id, out var consent);
        return Task.FromResult(consent);
    }

    public Task<IEnumerable<Consent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_consents.Values.AsEnumerable());
    }

    public Task<Consent?> CreateAsync(Consent entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            return Task.FromResult<Consent?>(null);

        var id = Guid.NewGuid().ToString();
        entity.ConsentId = id;
        _consents[id] = entity;

        return Task.FromResult<Consent?>(entity);
    }

    public Task<bool> UpdateAsync(Consent entity, CancellationToken cancellationToken = default)
    {
        if (entity == null || string.IsNullOrWhiteSpace(entity.ConsentId))
            return Task.FromResult(false);

        if (!_consents.ContainsKey(entity.ConsentId))
            return Task.FromResult(false);

        _consents[entity.ConsentId] = entity;
        return Task.FromResult(true);
    }

    public Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(id))
            return Task.FromResult(false);

        return Task.FromResult(_consents.Remove(id));
    }

    public Task<IEnumerable<Consent>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _consents.Values.Where(c => c.UserId == userId).AsEnumerable());
    }

    public Task<Consent?> GetByUserAndClientAsync(
        string userId,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        var consent = _consents.Values.FirstOrDefault(c =>
            c.UserId == userId && c.ClientId == clientId);

        return Task.FromResult(consent);
    }

    public Task<IEnumerable<Consent>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            _consents.Values.Where(c => c.ClientId == clientId).AsEnumerable());
    }

    public Task<int> RevokeUserConsentsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var consentsToRemove = _consents.Values.Where(c => c.UserId == userId).ToList();
        var count = 0;

        foreach (var consent in consentsToRemove)
        {
            if (_consents.Remove(consent.ConsentId))
            {
                count++;
            }
        }

        return Task.FromResult(count);
    }

    public Task<bool> RevokeConsentAsync(
        string userId,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        var consent = _consents.Values.FirstOrDefault(c =>
            c.UserId == userId && c.ClientId == clientId);

        if (consent == null)
            return Task.FromResult(false);

        return Task.FromResult(_consents.Remove(consent.ConsentId));
    }
}
