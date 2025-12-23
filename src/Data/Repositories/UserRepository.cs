// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Data.Repositories;

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
public class UserRepository : IUserRepository
{
    private readonly Dictionary<string, User> _users = new(StringComparer.OrdinalIgnoreCase);

    public async Task<User?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_users.TryGetValue(id, out var user) ? user : null);
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_users.Values.ToList());
    }

    public async Task<User> CreateAsync(User entity, CancellationToken cancellationToken = default)
    {
        if (_users.ContainsKey(entity.UserId))
            throw new InvalidOperationException($"User with ID {entity.UserId} already exists");

        _users[entity.UserId] = entity;
        return await Task.FromResult(entity);
    }

    public async Task<User> UpdateAsync(User entity, CancellationToken cancellationToken = default)
    {
        if (!_users.ContainsKey(entity.UserId))
            throw new InvalidOperationException($"User with ID {entity.UserId} not found");

        entity.UpdatedAt = DateTime.UtcNow;
        _users[entity.UserId] = entity;
        return await Task.FromResult(entity);
    }

    public async Task DeleteAsync(User entity, CancellationToken cancellationToken = default)
    {
        await DeleteByIdAsync(entity.UserId, cancellationToken);
    }

    public async Task DeleteByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        _users.Remove(id);
        return await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(_users.ContainsKey(id));
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = _users.Values.FirstOrDefault(u =>
            u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        return await Task.FromResult(user);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = _users.Values.FirstOrDefault(u =>
            u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        return await Task.FromResult(user);
    }

    public async Task<IEnumerable<User>> GetByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        var users = _users.Values.Where(u =>
            u.Roles.Contains(role, StringComparer.OrdinalIgnoreCase)).ToList();
        return await Task.FromResult(users);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = _users.Values.Where(u => u.IsActive).ToList();
        return await Task.FromResult(users);
    }

    public async Task<IEnumerable<User>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var lowerQuery = query.ToLower();
        var results = _users.Values.Where(u =>
            u.Username.ToLower().Contains(lowerQuery) ||
            u.Email.ToLower().Contains(lowerQuery) ||
            (u.FullName?.ToLower().Contains(lowerQuery) ?? false)).ToList();
        return await Task.FromResult(results);
    }
}
