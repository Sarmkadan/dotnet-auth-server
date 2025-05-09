#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Request model for creating a new user account.
/// </summary>
public sealed class CreateUserRequest
{
    /// <summary>
    /// Unique username (3–50 alphanumeric characters, dots, underscores or hyphens).
    /// </summary>
    [Required]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = null!;

    /// <summary>
    /// Valid email address for the account.
    /// </summary>
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    /// <summary>
    /// Plain-text password; must meet the configured minimum-length policy.
    /// </summary>
    [Required]
    [MinLength(8)]
    public string Password { get; set; } = null!;

    /// <summary>
    /// Optional display name shown in tokens and the UI.
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Initial roles to assign. Roles are case-insensitive strings (e.g. "admin", "user").
    /// </summary>
    public ICollection<string> Roles { get; set; } = [];
}

/// <summary>
/// Request model for updating an existing user account.
/// All properties are optional — only non-null values are applied.
/// </summary>
public sealed class UpdateUserRequest
{
    /// <summary>New display name.</summary>
    public string? FullName { get; set; }

    /// <summary>Activates or deactivates the account.</summary>
    public bool? IsActive { get; set; }

    /// <summary>
    /// ABAC attribute bag to merge into the existing attributes dictionary.
    /// Existing keys are overwritten; keys absent from this payload are kept.
    /// </summary>
    public Dictionary<string, object>? Attributes { get; set; }
}

/// <summary>
/// Request model for assigning a role to a user.
/// </summary>
public sealed class AssignRoleRequest
{
    /// <summary>Role name to assign (case-insensitive).</summary>
    [Required]
    [StringLength(100, MinimumLength = 1)]
    public string Role { get; set; } = null!;
}

/// <summary>
/// Request model for changing a user's password.
/// </summary>
public sealed class ChangePasswordRequest
{
    /// <summary>The user's current password for verification.</summary>
    [Required]
    public string CurrentPassword { get; set; } = null!;

    /// <summary>The new password; must meet the configured minimum-length policy.</summary>
    [Required]
    [MinLength(8)]
    public string NewPassword { get; set; } = null!;
}

/// <summary>
/// Read-only projection of a User entity for API responses.
/// Sensitive fields (password hash, TOTP secret) are excluded.
/// </summary>
public sealed class UserResponse
{
    /// <summary>Unique user identifier.</summary>
    public string UserId { get; set; } = null!;

    /// <summary>Login username.</summary>
    public string Username { get; set; } = null!;

    /// <summary>Email address.</summary>
    public string Email { get; set; } = null!;

    /// <summary>Optional display name.</summary>
    public string? FullName { get; set; }

    /// <summary>Whether the account is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Whether the email has been verified.</summary>
    public bool EmailVerified { get; set; }

    /// <summary>RBAC roles assigned to the user.</summary>
    public ICollection<string> Roles { get; set; } = [];

    /// <summary>ABAC attribute bag.</summary>
    public Dictionary<string, object> Attributes { get; set; } = [];

    /// <summary>Account creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Last modification timestamp (UTC).</summary>
    public DateTime UpdatedAt { get; set; }

    /// <summary>Last successful login timestamp (UTC), or null if never logged in.</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>Whether the account is currently locked out.</summary>
    public bool IsLocked { get; set; }

    /// <summary>Lock expiry timestamp (UTC), or null when not locked.</summary>
    public DateTime? LockedUntil { get; set; }
}
