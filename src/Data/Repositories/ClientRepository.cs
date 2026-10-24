#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Data.Repositories;

using System.Collections.Concurrent;
using DotnetAuthServer.Domain.Entities;

/// <summary>
/// Repository for Client entity operations
/// </summary>
public interface IClientRepository : IRepository<Client, string>
{
    /// <summary>
    /// Gets a client by ID with validation
    /// </summary>
    Task<Client?> GetActiveClientAsync(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active clients
    /// </summary>
    Task<IEnumerable<Client>> GetActiveClientsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches clients by name
    /// </summary>
    Task<IEnumerable<Client>> SearchAsync(string query, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of client repository
/// </summary>
public sealed class ClientRepository : IClientRepository
{
    private readonly ConcurrentDictionary<string, Client> _clients = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a client by its client ID, or null if it does not exist.
    /// </summary>
    public async Task<Client?> GetByIdAsync(string clientId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(clientId);
        return await Task.FromResult(_clients.TryGetValue(clientId, out var client) ? client : null);
    }

    /// <summary>
    /// Gets all registered clients.
    /// </summary>
    public async Task<IEnumerable<Client>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_clients.Values.ToList());
    }

    /// <summary>
    /// Adds a new client; throws if a client with the same ID already exists.
    /// </summary>
    public async Task<Client> CreateAsync(Client entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (!_clients.TryAdd(entity.ClientId, entity))
            throw new InvalidOperationException($"Client with ID {entity.ClientId} already exists");
        return await Task.FromResult(entity);
    }

    /// <summary>
    /// Replaces an existing client and stamps its UpdatedAt time; throws if the client does not exist.
    /// </summary>
    public async Task<Client> UpdateAsync(Client entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (!_clients.ContainsKey(entity.ClientId))
            throw new InvalidOperationException($"Client with ID {entity.ClientId} not found");

        entity.UpdatedAt = DateTime.UtcNow;
        _clients[entity.ClientId] = entity;
        return await Task.FromResult(entity);
    }

    /// <summary>
    /// Deletes the specified client.
    /// </summary>
    public async Task DeleteAsync(Client entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await DeleteByIdAsync(entity.ClientId, cancellationToken);
    }

    /// <summary>
    /// Deletes the client with the given ID, if present.
    /// </summary>
    public Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        _clients.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks whether a client with the given ID exists.
    /// </summary>
    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        return await Task.FromResult(_clients.ContainsKey(id));
    }

    /// <summary>
    /// Gets an active client by ID, or null if the client is missing or inactive.
    /// </summary>
    public async Task<Client?> GetActiveClientAsync(string clientId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(clientId);
        var client = await GetByIdAsync(clientId, cancellationToken);
        return await Task.FromResult(client?.IsActive == true ? client : null);
    }

    /// <summary>
    /// Gets all active clients.
    /// </summary>
    public async Task<IEnumerable<Client>> GetActiveClientsAsync(CancellationToken cancellationToken = default)
    {
        var clients = _clients.Values.Where(c => c.IsActive).ToList();
        return await Task.FromResult(clients);
    }

    /// <summary>
    /// Searches clients whose name, client ID, or description contains the query (case-insensitive).
    /// </summary>
    public async Task<IEnumerable<Client>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(query);
        var lowerQuery = query.ToLower();
        var results = _clients.Values.Where(c =>
            c.ClientName.ToLower().Contains(lowerQuery) ||
            c.ClientId.ToLower().Contains(lowerQuery) ||
            (c.Description?.ToLower().Contains(lowerQuery) ?? false)).ToList();
        return await Task.FromResult(results);
    }
}
