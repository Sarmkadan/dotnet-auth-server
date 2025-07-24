#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Data.Repositories;

using System.Collections.Concurrent;
using System.Collections.Generic;
using DotnetAuthServer.Domain.Entities;

/// <summary>
/// In-memory repository implementation for consent records.
/// Stores user consent data for audit and permission tracking.
/// In production, should be persisted to a database.
/// </summary>
public sealed class ConsentRepository : IConsentRepository
{
    private readonly ConcurrentDictionary<string, Consent> _consents = new();

    public Task<Consent?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _consents.TryGetValue(id, out var consent);
        return Task.FromResult(consent);
    }

    public Task<IEnumerable<Consent>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_consents.Values.AsEnumerable());
    }

    public Task<Consent> CreateAsync(Consent entity, CancellationToken cancellationToken = default)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        var id = string.IsNullOrWhiteSpace(entity.ConsentId) ? Guid.NewGuid().ToString() : entity.ConsentId;
        entity.ConsentId = id;
        _consents[id] = entity;

        return Task.FromResult(entity);
    }

    public Task<Consent> UpdateAsync(Consent entity, CancellationToken cancellationToken = default)
    {
        if (entity is null || string.IsNullOrWhiteSpace(entity.ConsentId))
            throw new ArgumentException("Consent must have a valid ID", nameof(entity));

        if (!_consents.ContainsKey(entity.ConsentId))
            throw new InvalidOperationException($"Consent with ID {entity.ConsentId} not found");

        _consents[entity.ConsentId] = entity;
        return Task.FromResult(entity);
    }

    public Task DeleteAsync(Consent entity, CancellationToken cancellationToken = default)
    {
        if (entity is not null && !string.IsNullOrWhiteSpace(entity.ConsentId))
            _consents.TryRemove(entity.ConsentId, out _);
        return Task.CompletedTask;
    }

    public Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(id))
            _consents.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(!string.IsNullOrWhiteSpace(id) && _consents.ContainsKey(id));
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
            if (_consents.TryRemove(consent.ConsentId, out _))
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

        if (consent is null)
            return Task.FromResult(false);

        return Task.FromResult(_consents.TryRemove(consent.ConsentId, out _));
    }
}
