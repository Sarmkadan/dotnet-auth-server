// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Data.Repositories;

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
public class ClientRepository : IClientRepository
{
    private readonly Dictionary<string, Client> _clients = new(StringComparer.OrdinalIgnoreCase);

    public async Task<Client?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_clients.TryGetValue(id, out var client) ? client : null);
    }

    public async Task<IEnumerable<Client>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_clients.Values.ToList());
    }

    public async Task<Client> CreateAsync(Client entity, CancellationToken cancellationToken = default)
    {
        if (_clients.ContainsKey(entity.ClientId))
            throw new InvalidOperationException($"Client with ID {entity.ClientId} already exists");

        _clients[entity.ClientId] = entity;
        return await Task.FromResult(entity);
    }

    public async Task<Client> UpdateAsync(Client entity, CancellationToken cancellationToken = default)
    {
        if (!_clients.ContainsKey(entity.ClientId))
            throw new InvalidOperationException($"Client with ID {entity.ClientId} not found");

        entity.UpdatedAt = DateTime.UtcNow;
        _clients[entity.ClientId] = entity;
        return await Task.FromResult(entity);
    }

    public async Task DeleteAsync(Client entity, CancellationToken cancellationToken = default)
    {
        await DeleteByIdAsync(entity.ClientId, cancellationToken);
    }

    public async Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _clients.Remove(id);
        return await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_clients.ContainsKey(id));
    }

    public async Task<Client?> GetActiveClientAsync(string clientId, CancellationToken cancellationToken = default)
    {
        var client = await GetByIdAsync(clientId, cancellationToken);
        return await Task.FromResult(client?.IsActive == true ? client : null);
    }

    public async Task<IEnumerable<Client>> GetActiveClientsAsync(CancellationToken cancellationToken = default)
    {
        var clients = _clients.Values.Where(c => c.IsActive).ToList();
        return await Task.FromResult(clients);
    }

    public async Task<IEnumerable<Client>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var lowerQuery = query.ToLower();
        var results = _clients.Values.Where(c =>
            c.ClientName.ToLower().Contains(lowerQuery) ||
            c.ClientId.ToLower().Contains(lowerQuery) ||
            (c.Description?.ToLower().Contains(lowerQuery) ?? false)).ToList();
        return await Task.FromResult(results);
    }
}
