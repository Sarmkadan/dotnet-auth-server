#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Data.Repositories;

using System.Collections.Concurrent;
using DotnetAuthServer.Domain.Entities;

/// <summary>
/// Repository for User entity operations
/// </summary>
public interface IUserRepository : IRepository<User, string>
{
    /// <summary>
    /// Gets a user by username
    /// </summary>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all users with a specific role
    /// </summary>
    Task<IEnumerable<User>> GetByRoleAsync(string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active users only
    /// </summary>
    Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches users by name or email
    /// </summary>
    Task<IEnumerable<User>> SearchAsync(string query, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory implementation of user repository
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a user by ID, or null if it does not exist.
    /// </summary>
    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);

        return await Task.FromResult(_users.TryGetValue(id, out var user) ? user : null);
    }

    /// <summary>
    /// Gets all users.
    /// </summary>
    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_users.Values.ToList());
    }

    /// <summary>
    /// Adds a new user; throws if a user with the same ID already exists.
    /// </summary>
    public async Task<User> CreateAsync(User entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (!_users.TryAdd(entity.UserId, entity))
            throw new InvalidOperationException($"User with ID {entity.UserId} already exists");
        return await Task.FromResult(entity);
    }

    /// <summary>
    /// Replaces an existing user and stamps its UpdatedAt time; throws if it does not exist.
    /// </summary>
    public async Task<User> UpdateAsync(User entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        if (!_users.ContainsKey(entity.UserId))
            throw new InvalidOperationException($"User with ID {entity.UserId} not found");

        entity.UpdatedAt = DateTime.UtcNow;
        _users[entity.UserId] = entity;
        return await Task.FromResult(entity);
    }

    /// <summary>
    /// Deletes the specified user.
    /// </summary>
    public async Task DeleteAsync(User entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await DeleteByIdAsync(entity.UserId, cancellationToken);
    }

    /// <summary>
    /// Deletes the user with the given ID, if present.
    /// </summary>
    public Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        _users.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Checks whether a user with the given ID exists.
    /// </summary>
    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        return await Task.FromResult(_users.ContainsKey(id));
    }

    /// <summary>
    /// Gets a user by username (case-insensitive), or null if none matches.
    /// </summary>
    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(username);
        var user = _users.Values.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        return await Task.FromResult(user);
    }

    /// <summary>
    /// Gets a user by email address (case-insensitive), or null if none matches.
    /// </summary>
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(email);
        var user = _users.Values.FirstOrDefault(u =>
            u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return await Task.FromResult(user);
    }

    /// <summary>
    /// Gets all users that have the specified role (case-insensitive).
    /// </summary>
    public async Task<IEnumerable<User>> GetByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(role);
        var users = _users.Values.Where(u =>
            u.Roles.Contains(role, StringComparer.OrdinalIgnoreCase)).ToList();
        return await Task.FromResult(users);
    }

    /// <summary>
    /// Gets all active users.
    /// </summary>
    public async Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = _users.Values.Where(u => u.IsActive).ToList();
        return await Task.FromResult(users);
    }

    /// <summary>
    /// Searches users whose username, email, or full name contains the query (case-insensitive).
    /// </summary>
    public async Task<IEnumerable<User>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(query);
        var lowerQuery = query.ToLower();
        var results = _users.Values.Where(u =>
            u.Username.ToLower().Contains(lowerQuery) ||
            u.Email.ToLower().Contains(lowerQuery) ||
            (u.FullName?.ToLower().Contains(lowerQuery) ?? false)).ToList();
        return await Task.FromResult(results);
    }
}
