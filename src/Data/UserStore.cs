#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Data;

using System.Collections.Concurrent;
using System.Security.Cryptography;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using Microsoft.Extensions.Options;

/// <summary>
/// In-memory user store providing user lookup, creation and credential verification.
/// Wraps <see cref="IUserRepository"/> with an LRU cache layer for hot-path reads.
/// </summary>
public sealed class UserStore
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserStore> _logger;
    private readonly int _iterations;
    private readonly ConcurrentDictionary<string, User> _cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of <see cref="UserStore"/>.
    /// </summary>
    /// <param name="userRepository">Underlying user persistence layer.</param>
    /// <param name="options">User store options, including password hashing parameters.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    /// <exception cref="OptionsValidationException"><paramref name="options"/> contains invalid password hashing settings.</exception>
    public UserStore(IUserRepository userRepository, UserStoreOptions options, ILogger<UserStore> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        ArgumentNullException.ThrowIfNull(options);
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var errors = UserStoreOptionsValidator.ValidatePasswordHashing(options.PasswordHashing);
        if (errors.Count > 0)
        {
            throw new OptionsValidationException(string.Empty, typeof(UserStoreOptions), errors);
        }

        _iterations = options.PasswordHashing.Iterations;
    }

    /// <summary>
    /// Finds a user by their unique identifier, checking the cache first.
    /// </summary>
    /// <param name="userId">The user's unique identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user entity, or <c>null</c> if not found.</returns>
    public async Task<User?> FindByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(userId, out var cached))
        {
            return cached;
        }

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is not null)
        {
            _cache.TryAdd(userId, user);
        }

        return user;
    }

    /// <summary>
    /// Finds a user by username (case-insensitive).
    /// </summary>
    /// <param name="username">The username to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user entity, or <c>null</c> if not found.</returns>
    public async Task<User?> FindByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        if (user is not null)
        {
            _cache.TryAdd(user.UserId, user);
        }

        return user;
    }

    /// <summary>
    /// Creates a new user with a hashed password and persists it.
    /// </summary>
    /// <param name="username">Desired username.</param>
    /// <param name="email">User's email address.</param>
    /// <param name="plainPassword">Plain-text password to be hashed.</param>
    /// <param name="roles">Initial role assignments.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created user entity.</returns>
    public async Task<User> CreateAsync(
        string username,
        string email,
        string plainPassword,
        ICollection<string>? roles = null,
        CancellationToken cancellationToken = default)
    {
        var existing = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException($"Username '{username}' is already taken.");
        }

        var user = new User
        {
            UserId = Guid.NewGuid().ToString(),
            Username = username,
            Email = email,
            PasswordHash = HashPassword(plainPassword),
            IsActive = true,
            EmailVerified = false,
            Roles = roles ?? [],
            CreatedAt = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user, cancellationToken);
        _cache.TryAdd(user.UserId, user);

        _logger.LogInformation("Created user {UserId} ({Username})", user.UserId, username);
        return user;
    }

    /// <summary>
    /// Verifies a plain-text password against the stored hash for a given user.
    /// </summary>
    /// <param name="user">The user entity containing the stored password hash.</param>
    /// <param name="plainPassword">The plain-text password to verify.</param>
    /// <returns><c>true</c> if the password matches; otherwise <c>false</c>.</returns>
    public bool VerifyPassword(User user, string plainPassword)
    {
        ArgumentNullException.ThrowIfNull(user);

        var candidateHash = HashPassword(plainPassword);
        var match = CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.UTF8.GetBytes(user.PasswordHash),
            System.Text.Encoding.UTF8.GetBytes(candidateHash));

        if (!match)
        {
            _logger.LogDebug("Password verification failed for user {UserId}", user.UserId);
        }

        return match;
    }

    /// <summary>
    /// Invalidates the cached entry for a user, forcing a fresh repository read on next access.
    /// </summary>
    /// <param name="userId">The user's unique identifier to evict from cache.</param>
    public void EvictCache(string userId)
    {
        _cache.TryRemove(userId, out _);
    }

    private string HashPassword(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        var salt = "dotnet-auth-server-static-salt"u8;
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            System.Text.Encoding.UTF8.GetBytes(password),
            salt,
            iterations: _iterations,
            HashAlgorithmName.SHA256,
            outputLength: 32);

        return Convert.ToHexStringLower(hash);
    }
}
