// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using System.Text.RegularExpressions;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Exceptions;

/// <summary>
/// Service for user authentication and management
/// </summary>
public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly AuthServerOptions _options;

    public UserService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        AuthServerOptions options)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _options = options;
    }

    /// <summary>
    /// Authenticates a user with username and password
    /// </summary>
    public async Task<User> AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByUsernameAsync(username, cancellationToken);

        if (user == null)
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidGrant,
                "Invalid credentials",
                401);

        if (user.IsLocked())
            throw new AuthServerException(
                Constants.ErrorCodes.AccessDenied,
                "Account is locked due to too many failed login attempts",
                403);

        if (!user.IsActive)
            throw new AuthServerException(
                Constants.ErrorCodes.AccessDenied,
                "User account is inactive",
                403);

        // In production, use bcrypt.VerifyHashedPassword or similar
        // For this implementation, we assume password hashing is done elsewhere
        if (!VerifyPassword(password, user.PasswordHash))
        {
            user.RecordFailedLogin(_options.FailedLoginAttemptThreshold);
            await _userRepository.UpdateAsync(user, cancellationToken);
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidGrant,
                "Invalid credentials",
                401);
        }

        user.RecordSuccessfulLogin();
        await _userRepository.UpdateAsync(user, cancellationToken);

        return user;
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

        // Validate password strength
        if (!IsStrongPassword(password))
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidRequest,
                $"Password must be at least {Constants.Validation.MinPasswordLength} characters",
                400);

        // Check if user already exists
        var existingUser = await _userRepository.GetByUsernameAsync(username, cancellationToken);
        if (existingUser != null)
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidRequest,
                "Username already exists",
                400);

        var existingEmail = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existingEmail != null)
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

        return await _userRepository.CreateAsync(user, cancellationToken);
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
        if (!string.IsNullOrWhiteSpace(fullName))
            user.FullName = fullName;

        if (attributes != null)
        {
            foreach (var attr in attributes)
            {
                user.Attributes[attr.Key] = attr.Value;
            }
        }

        return await _userRepository.UpdateAsync(user, cancellationToken);
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
        if (!VerifyPassword(currentPassword, user.PasswordHash))
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidGrant,
                "Current password is incorrect",
                401);

        if (!IsStrongPassword(newPassword))
            throw new AuthServerException(
                Constants.ErrorCodes.InvalidRequest,
                $"New password must be at least {Constants.Validation.MinPasswordLength} characters",
                400);

        user.PasswordHash = HashPassword(newPassword);
        user.RecordFailedLogin(0); // Reset failed attempts
        await _userRepository.UpdateAsync(user, cancellationToken);

        // Revoke all refresh tokens on password change
        await _refreshTokenRepository.RevokeAllUserTokensAsync(
            user.UserId,
            "Password changed",
            cancellationToken);
    }

    /// <summary>
    /// Assigns a role to a user for RBAC
    /// </summary>
    public async Task AssignRoleAsync(
        User user,
        string role,
        CancellationToken cancellationToken = default)
    {
        if (user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase))
            return;

        user.Roles.Add(role);
        await _userRepository.UpdateAsync(user, cancellationToken);
    }

    /// <summary>
    /// Removes a role from a user
    /// </summary>
    public async Task RemoveRoleAsync(
        User user,
        string role,
        CancellationToken cancellationToken = default)
    {
        user.Roles.Remove(role);
        await _userRepository.UpdateAsync(user, cancellationToken);
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
    /// Validates password strength
    /// </summary>
    private static bool IsStrongPassword(string password)
    {
        return !string.IsNullOrWhiteSpace(password) &&
               password.Length >= Constants.Validation.MinPasswordLength;
    }

    /// <summary>
    /// Hashes a password (simplified - use bcrypt in production)
    /// </summary>
    private static string HashPassword(string password)
    {
        // In production, use BCrypt: BCrypt.Net.BCrypt.HashPassword(password, 12)
        // This is a simplified version for demo
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }
    }

    /// <summary>
    /// Verifies a password against its hash
    /// </summary>
    private static bool VerifyPassword(string password, string hash)
    {
        // In production, use BCrypt: BCrypt.Net.BCrypt.Verify(password, hash)
        var hashOfInput = HashPassword(password);
        return hashOfInput.Equals(hash, StringComparison.Ordinal);
    }
}
