#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Services;

using System.Text.RegularExpressions;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// Service for user authentication and management
/// </summary>
public sealed class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly AuthServerOptions _options;
    private readonly ILogger<UserService> _logger;
    private readonly PasswordValidationService _passwordValidator;

    public UserService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        AuthServerOptions options,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _refreshTokenRepository = refreshTokenRepository ?? throw new ArgumentNullException(nameof(refreshTokenRepository));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _passwordValidator = new PasswordValidationService(_options, _options.PasswordPolicy);
    }

    /// <summary>
    /// Authenticates a user with username and password
    /// </summary>
    public async Task<User> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be null or whitespace", nameof(username));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or whitespace", nameof(password));

        try
        {
            var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);

            if (user is null)
            {
                _logger.LogWarning("Authentication attempt with invalid username: {Username}", username);
                throw new AuthServerException(
                    Constants.ErrorCodes.InvalidGrant,
                    "Invalid credentials",
                    401);
            }

            if (user.IsLocked())
            {
                _logger.LogWarning("Authentication attempt for locked account: {UserId}", user.UserId);
                throw new AuthServerException(
                    Constants.ErrorCodes.AccessDenied,
                    "Account is locked due to too many failed login attempts",
                    403);
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Authentication attempt for inactive account: {UserId}", user.UserId);
                throw new AuthServerException(
                    Constants.ErrorCodes.AccessDenied,
                    "User account is inactive",
                    403);
            }

            if (!VerifyPassword(password, user.PasswordHash))
            {
                user.RecordFailedLogin(_options.FailedLoginAttemptThreshold);
                await _userRepository.UpdateAsync(user, cancellationToken);
                _logger.LogInformation("Failed login attempt for user: {Username}", username);
                throw new AuthServerException(
                    Constants.ErrorCodes.InvalidGrant,
                    "Invalid credentials",
                    401);
            }

            user.RecordSuccessfulLogin();
            await _userRepository.UpdateAsync(user, cancellationToken);

            _logger.LogInformation("User authenticated successfully: {Username}", username);
            return user;
        }
        catch (AuthServerException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error during user authentication for username: {Username}", username);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Authentication failed due to server error",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Creates a new user
    /// </summary>
    public async Task<User> CreateUserAsync(
        string username,
        string email,
        string password,
        string? fullName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username cannot be null or whitespace", nameof(username));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be null or whitespace", nameof(email));

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be null or whitespace", nameof(password));

        try
        {
            // Validate username format
            if (!IsValidUsername(username))
                throw new AuthServerException(
                    Constants.ErrorCodes.InvalidRequest,
                    "Username must be 3-50 characters, alphanumeric with allowed special characters",
                    400);

            // Validate email format
            if (!IsValidEmail(email))
                throw new AuthServerException(
                    Constants.ErrorCodes.InvalidRequest,
                    "Invalid email format",
                    400);

        // Validate password against policy
        _passwordValidator.ValidateAndThrow(password, username);

            // Check if user already exists
            var existingUser = await _userRepository.GetByUsernameAsync(username, cancellationToken);
            if (existingUser is not null)
                throw new AuthServerException(
                    Constants.ErrorCodes.InvalidRequest,
                    "Username already exists",
                    400);

            var existingEmail = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (existingEmail is not null)
                throw new AuthServerException(
                    Constants.ErrorCodes.InvalidRequest,
                    "Email already registered",
                    400);

            var user = new User
            {
                UserId = Guid.NewGuid().ToString(),
                Username = username,
                Email = email,
                FullName = fullName,
                PasswordHash = HashPassword(password),
                IsActive = true
            };

            if (!user.IsValid())
                throw new AuthServerException(
                    Constants.ErrorCodes.ServerError,
                    "User creation failed validation",
                    500);

            var createdUser = await _userRepository.CreateAsync(user, cancellationToken);
            _logger.LogInformation("User created successfully: {Username}", username);
            return createdUser;
        }
        catch (AuthServerException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error creating user with username: {Username}", username);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "User creation failed due to server error",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Updates user profile information
    /// </summary>
    public async Task<User> UpdateUserAsync(
        User user,
        string? fullName = null,
        Dictionary<string, object>? attributes = null,
        CancellationToken cancellationToken = default)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user));

        try
        {
            if (!string.IsNullOrWhiteSpace(fullName))
                user.FullName = fullName;

            if (attributes is not null)
            {
                foreach (var attr in attributes)
                {
                    user.Attributes[attr.Key] = attr.Value;
                }
            }

            var updatedUser = await _userRepository.UpdateAsync(user, cancellationToken);
            _logger.LogInformation("User updated successfully: {UserId}", user.UserId);
            return updatedUser;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error updating user with ID: {UserId}", user?.UserId);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "User update failed due to server error",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Changes user password
    /// </summary>
    public async Task ChangePasswordAsync(
        User user,
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user));

        if (string.IsNullOrWhiteSpace(currentPassword))
            throw new ArgumentException("Current password cannot be null or whitespace", nameof(currentPassword));

        if (string.IsNullOrWhiteSpace(newPassword))
            throw new ArgumentException("New password cannot be null or whitespace", nameof(newPassword));

        try
        {
            if (!VerifyPassword(currentPassword, user.PasswordHash))
                throw new AuthServerException(
                    Constants.ErrorCodes.InvalidGrant,
                    "Current password is incorrect",
                    401);

            _passwordValidator.ValidateAndThrow(newPassword, user.Username);

            user.PasswordHash = HashPassword(newPassword);
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            await _userRepository.UpdateAsync(user, cancellationToken);

            // Revoke all refresh tokens on password change
            await _refreshTokenRepository.RevokeAllUserTokensAsync(
                user.UserId,
                "Password changed",
                cancellationToken);

            _logger.LogInformation("Password changed successfully for user: {UserId}", user.UserId);
        }
        catch (AuthServerException)
        {
            throw;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error changing password for user: {UserId}", user.UserId);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Password change failed due to server error",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Assigns a role to a user for RBAC
    /// </summary>
    public async Task AssignRoleAsync(
        User user,
        string role,
        CancellationToken cancellationToken = default)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user));

        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be null or whitespace", nameof(role));

        try
        {
            if (user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase))
                return;

            user.Roles.Add(role);
            await _userRepository.UpdateAsync(user, cancellationToken);
            _logger.LogInformation("Role {Role} assigned to user: {UserId}", role, user.UserId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error assigning role {Role} to user: {UserId}", role, user.UserId);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Role assignment failed due to server error",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Removes a role from a user
    /// </summary>
    public async Task RemoveRoleAsync(
        User user,
        string role,
        CancellationToken cancellationToken = default)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user));

        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be null or whitespace", nameof(role));

        try
        {
            user.Roles.Remove(role);
            await _userRepository.UpdateAsync(user, cancellationToken);
            _logger.LogInformation("Role {Role} removed from user: {UserId}", role, user.UserId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error removing role {Role} from user: {UserId}", role, user.UserId);
            throw new AuthServerException(
                Constants.ErrorCodes.ServerError,
                "Role removal failed due to server error",
                500,
                null,
                null,
                ex);
        }
    }

    /// <summary>
    /// Validates username format
    /// </summary>
    private static bool IsValidUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3 || username.Length > 50)
            return false;

        return Regex.IsMatch(username, @"^[a-zA-Z0-9._-]+$");
    }

    /// <summary>
    /// Validates email format
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>

    /// <summary>
    /// Hashes a password using bcrypt with a per-password salt.
    /// </summary>
    private static string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);

    /// <summary>
    /// Verifies a password against its stored hash.
    /// Accepts bcrypt hashes; falls back to the legacy unsalted SHA-256 format
    /// for accounts created before the bcrypt migration.
    /// </summary>
    private static bool VerifyPassword(string password, string hash)
    {
        if (string.IsNullOrEmpty(hash))
            return false;

        if (hash.StartsWith("$2", StringComparison.Ordinal))
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hash);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                return false;
            }
        }

        // Legacy format: base64(SHA256(password)) written by earlier versions.
        var sha256Bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(password));
        var storedBytes = new byte[hash.Length];
        if (!Convert.TryFromBase64String(hash, storedBytes, out var written))
            return false;

        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            sha256Bytes, storedBytes.AsSpan(0, written));
    }
}