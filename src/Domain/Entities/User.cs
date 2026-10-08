#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Entities;

using System.Diagnostics;

/// <summary>
/// Represents a user in the authorization system
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class User
{
    /// <summary>
    /// Unique identifier for the user
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// User's username/login
    /// </summary>
    public string Username { get; set; } = null!;

    /// <summary>
    /// User's email address
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// User's full name
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Hashed password (using bcrypt or PBKDF2)
    /// </summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// Whether the user's email is verified
    /// </summary>
    public bool EmailVerified { get; set; }

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// User's assigned roles for RBAC
    /// </summary>
    public ICollection<string> Roles { get; set; } = [];

    /// <summary>
    /// Attributes for ABAC (Attribute-Based Access Control)
    /// </summary>
    public Dictionary<string, object> Attributes { get; set; } = [];

    /// <summary>
    /// Account creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last account update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last successful login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Number of failed login attempts
    /// </summary>
    public int FailedLoginAttempts { get; set; }

    /// <summary>
    /// Account lockout expiration (null if not locked)
    /// </summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>
    /// Whether the user has an enabled TOTP credential. Mirrors <c>TotpCredential.IsEnabled</c>
    /// and is kept in sync by <c>TotpService</c>.
    /// </summary>
    public bool MfaEnabled { get; set; }

    /// <summary>
    /// Validates the user has all required properties for creation
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(UserId) &&
               !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Email) &&
               !string.IsNullOrWhiteSpace(PasswordHash);
    }

    /// <summary>
    /// Checks if the user account is locked due to too many login attempts
    /// </summary>
    public bool IsLocked()
    {
        if (LockedUntil is null) return false;
        if (DateTime.UtcNow >= LockedUntil)
        {
            LockedUntil = null;
            return false;
        }
        return true;
    }

    /// <summary>
    /// Locks the user account for a specified duration
    /// </summary>
    public void LockAccount(TimeSpan duration)
    {
        LockedUntil = DateTime.UtcNow.Add(duration);
        FailedLoginAttempts = 0;
    }

    /// <summary>
    /// Increments failed login attempts and locks if threshold exceeded
    /// </summary>
    public void RecordFailedLogin(int lockoutThreshold = 5, TimeSpan? lockoutDuration = null)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= lockoutThreshold)
        {
            LockAccount(lockoutDuration ?? TimeSpan.FromMinutes(15));
        }
    }

    /// <summary>
    /// Records a successful login and resets failed attempts
    /// </summary>
    public void RecordSuccessfulLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }

    /// <summary>
    /// Returns a diagnostic representation: identifier, username, lockout state and MFA status.
    /// Email, full name and password hash are deliberately omitted. Unlike <see cref="IsLocked"/>,
    /// this reads the lockout state without clearing an expired lock.
    /// </summary>
    public override string ToString()
    {
        var lockedOut = LockedUntil is { } until && DateTime.UtcNow < until;
        var lockedUntil = LockedUntil is { } expiry ? expiry.ToString("O") : "none";

        return $"User {{ UserId = {UserId}, Username = {Username}, LockedOut = {lockedOut}, LockedUntil = {lockedUntil}, FailedLoginAttempts = {FailedLoginAttempts}, MfaEnabled = {MfaEnabled} }}";
    }
}
