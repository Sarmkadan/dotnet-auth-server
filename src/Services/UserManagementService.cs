#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Exceptions;

/// <summary>
/// Provides administrative CRUD operations over user accounts.
/// This service is intended for privileged, server-side callers (admin API,
/// background jobs). End-user self-service operations live in <see cref="UserService"/>.
/// </summary>
public sealed class UserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IUserSessionRepository _sessionRepository;
    private readonly ILogger<UserManagementService> _logger;
    private readonly AuthServerOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="UserManagementService"/>.
    /// </summary>
    public UserManagementService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IUserSessionRepository sessionRepository,
        ILogger<UserManagementService> logger,
        AuthServerOptions options)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _sessionRepository = sessionRepository;
        _logger = logger;
        _options = options;
    }

    /// <summary>
    /// Returns all registered users.
    /// </summary>
    public async Task<IEnumerable<UserResponse>> GetAllUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users.Select(MapToResponse);
    }

    /// <summary>
    /// Returns the user with the given ID, or throws if not found.
    /// </summary>
    public async Task<UserResponse> GetUserByIdAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new AuthServerException(Constants.ErrorCodes.InvalidRequest, $"User '{userId}' not found", 404);

        return MapToResponse(user);
    }

    /// <summary>
    /// Searches users by username, email, or full name.
    /// </summary>
    public async Task<IEnumerable<UserResponse>> SearchUsersAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllUsersAsync(cancellationToken);

        var users = await _userRepository.SearchAsync(query, cancellationToken);
        return users.Select(MapToResponse);
    }

    /// <summary>
    /// Creates a new user account with optional initial roles.
    /// </summary>
    public async Task<UserResponse> CreateUserAsync(CreateUserRequest request, CancellationToken cancellationToken = default)
    {
        var userService = BuildUserService();
        var user = await userService.CreateUserAsync(
            request.Username,
            request.Email,
            request.Password,
            request.FullName,
            cancellationToken);

        foreach (var role in request.Roles)
            await userService.AssignRoleAsync(user, role, cancellationToken);

        _logger.LogInformation("Admin created user {UserId} ({Username})", user.UserId, user.Username);
        return MapToResponse(user);
    }

    /// <summary>
    /// Updates mutable profile fields on an existing user account.
    /// </summary>
    public async Task<UserResponse> UpdateUserAsync(string userId, UpdateUserRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new AuthServerException(Constants.ErrorCodes.InvalidRequest, $"User '{userId}' not found", 404);

        if (!string.IsNullOrWhiteSpace(request.FullName))
            user.FullName = request.FullName;

        if (request.IsActive.HasValue)
            user.IsActive = request.IsActive.Value;

        if (request.Attributes is not null)
        {
            foreach (var kv in request.Attributes)
                user.Attributes[kv.Key] = kv.Value;
        }

        await _userRepository.UpdateAsync(user, cancellationToken);
        _logger.LogInformation("Admin updated user {UserId}", userId);
        return MapToResponse(user);
    }

    /// <summary>
    /// Permanently deletes a user account and revokes all associated tokens and sessions.
    /// </summary>
    public async Task DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new AuthServerException(Constants.ErrorCodes.InvalidRequest, $"User '{userId}' not found", 404);

        await _refreshTokenRepository.RevokeAllUserTokensAsync(userId, "Account deleted", cancellationToken);
        await _sessionRepository.RevokeAllUserSessionsAsync(userId, "Account deleted", cancellationToken);
        await _userRepository.DeleteAsync(user, cancellationToken);

        _logger.LogInformation("Admin deleted user {UserId}", userId);
    }

    /// <summary>
    /// Assigns a role to a user. Idempotent — no error if the role is already assigned.
    /// </summary>
    public async Task AssignRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new AuthServerException(Constants.ErrorCodes.InvalidRequest, $"User '{userId}' not found", 404);

        await BuildUserService().AssignRoleAsync(user, role, cancellationToken);
        _logger.LogInformation("Admin assigned role {Role} to user {UserId}", role, userId);
    }

    /// <summary>
    /// Removes a role from a user. Idempotent — no error if the role is not assigned.
    /// </summary>
    public async Task RemoveRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new AuthServerException(Constants.ErrorCodes.InvalidRequest, $"User '{userId}' not found", 404);

        await BuildUserService().RemoveRoleAsync(user, role, cancellationToken);
        _logger.LogInformation("Admin removed role {Role} from user {UserId}", role, userId);
    }

    /// <summary>
    /// Locks a user account for the specified duration.
    /// </summary>
    public async Task LockUserAsync(string userId, TimeSpan duration, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new AuthServerException(Constants.ErrorCodes.InvalidRequest, $"User '{userId}' not found", 404);

        user.LockAccount(duration);
        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogWarning("Admin locked user {UserId} for {Duration}", userId, duration);
    }

    /// <summary>
    /// Unlocks a user account by clearing the lockout expiry and failure counter.
    /// </summary>
    public async Task UnlockUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new AuthServerException(Constants.ErrorCodes.InvalidRequest, $"User '{userId}' not found", 404);

        user.LockedUntil = null;
        user.FailedLoginAttempts = 0;
        await _userRepository.UpdateAsync(user, cancellationToken);

        _logger.LogInformation("Admin unlocked user {UserId}", userId);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private UserService BuildUserService()
        => new UserService(_userRepository, _refreshTokenRepository, _options, _logger.WithName<UserService>());

    private static UserResponse MapToResponse(User user) => new UserResponse
    {
        UserId = user.UserId,
        Username = user.Username,
        Email = user.Email,
        FullName = user.FullName,
        IsActive = user.IsActive,
        EmailVerified = user.EmailVerified,
        Roles = user.Roles,
        Attributes = user.Attributes,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
        LastLoginAt = user.LastLoginAt,
        IsLocked = user.IsLocked(),
        LockedUntil = user.LockedUntil
    };
}
