#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetAuthServer.Domain.Entities;

/// <summary>
/// Provides extension methods for the <see cref="User"/> entity to enhance functionality
/// with additional domain-specific operations.
/// </summary>
public static class UserExtensions
{
    /// <summary>
    /// Checks if the user has a specific role.
    /// </summary>
    /// <param name="user">The user instance.</param>
    /// <param name="role">The role to check for.</param>
    /// <returns>True if user has the role; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="role"/> is null or whitespace.</exception>
    public static bool HasRole(this User user, string role)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        return user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the user has any of the specified roles.
    /// </summary>
    /// <param name="user">The user instance.</param>
    /// <param name="roles">Collection of roles to check.</param>
    /// <returns>True if user has any of the roles; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> or <paramref name="roles"/> is null.</exception>
    public static bool HasAnyRole(this User user, IEnumerable<string> roles)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roles);

        return roles.Any(role => user.HasRole(role));
    }

    /// <summary>
    /// Gets an attribute value by key with optional default value.
    /// </summary>
    /// <typeparam name="T">Type of the attribute value.</typeparam>
    /// <param name="user">The user instance.</param>
    /// <param name="key">The attribute key.</param>
    /// <param name="defaultValue">Default value if key not found.</param>
    /// <returns>The attribute value or default if not found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or whitespace.</exception>
    public static T? GetAttribute<T>(this User user, string key, T? defaultValue = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (user.Attributes.TryGetValue(key, out var value) && value is T typedValue)
        {
            return typedValue;
        }

        return defaultValue;
    }

    /// <summary>
    /// Sets an attribute value.
    /// </summary>
    /// <param name="user">The user instance.</param>
    /// <param name="key">The attribute key.</param>
    /// <param name="value">The attribute value.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/>, <paramref name="key"/>, or <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is null or whitespace.</exception>
    public static void SetAttribute(this User user, string key, object value)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        user.Attributes[key] = value;
        user.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if the user is an administrator.
    /// </summary>
    /// <param name="user">The user instance.</param>
    /// <returns>True if user has Admin role; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is null.</exception>
    public static bool IsAdmin(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.HasRole("Admin") || user.HasRole("Administrator");
    }

    /// <summary>
    /// Gets the display name for the user (FullName if available, otherwise Username).
    /// </summary>
    /// <param name="user">The user instance.</param>
    /// <returns>Display name string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is null.</exception>
    public static string GetDisplayName(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.FullName ?? user.Username;
    }

    /// <summary>
    /// Checks if the user's email is verified, account is active, and not locked.
    /// </summary>
    /// <param name="user">The user instance.</param>
    /// <returns>True if user can authenticate; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is null.</exception>
    public static bool CanAuthenticate(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.EmailVerified && user.IsActive && !user.IsLocked();
    }

    /// <summary>
    /// Gets the time elapsed since last login in seconds.
    /// </summary>
    /// <param name="user">The user instance.</param>
    /// <returns>Seconds since last login, or null if never logged in.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="user"/> is null.</exception>
    public static long? SecondsSinceLastLogin(this User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return user.LastLoginAt.HasValue
            ? (long)(DateTime.UtcNow - user.LastLoginAt.Value).TotalSeconds
            : null;
    }
}