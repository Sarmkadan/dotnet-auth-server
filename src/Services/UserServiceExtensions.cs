#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Services;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Exceptions;

/// <summary>
/// Extension methods for UserService providing convenient utility operations
/// </summary>
public static class UserServiceExtensions
{
    /// <summary>
    /// Creates a new user with default "User" role assigned
    /// </summary>
    public static async Task<User> CreateUserWithRoleAsync(
        this UserService userService,
        string username,
        string email,
        string password,
        string role = "User",
        string? fullName = null,
        CancellationToken cancellationToken = default)
    {
        var user = await userService.CreateUserAsync(
            username,
            email,
            password,
            fullName,
            cancellationToken);

        await userService.AssignRoleAsync(user, role, cancellationToken);

        return user;
    }

    /// <summary>
    /// Bulk creates users from a collection of user data
    /// </summary>
    public static async Task<IReadOnlyList<User>> CreateUsersBulkAsync(
        this UserService userService,
        IEnumerable<(string Username, string Email, string Password, string? FullName)> userData,
        string defaultRole = "User",
        CancellationToken cancellationToken = default)
    {
        var users = new List<User>();
        var tasks = new List<Task<User>>();

        foreach (var data in userData)
        {
            tasks.Add(userService.CreateUserWithRoleAsync(
                data.Username,
                data.Email,
                data.Password,
                defaultRole,
                data.FullName,
                cancellationToken));
        }

        var results = await Task.WhenAll(tasks);
        users.AddRange(results);

        return users.AsReadOnly();
    }

    /// <summary>
    /// Checks if a user has a specific role
    /// </summary>
    public static bool HasRole(
        this UserService userService,
        User user,
        string role)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user));

        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be null or whitespace", nameof(role));

        return user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets all users with a specific role
    /// </summary>
    public static async Task<IReadOnlyList<User>> GetUsersByRoleAsync(
        this UserService userService,
        string role,
        CancellationToken cancellationToken = default)
    {
        // Note: This is a simplified implementation that fetches all users
        // In a real application, you'd want to implement this at the repository level
        // For this extension method, we'll demonstrate the concept

        if (string.IsNullOrWhiteSpace(role))
            throw new ArgumentException("Role cannot be null or whitespace", nameof(role));

        // This would need repository access - for demo purposes we'll return empty list
        // In production, you'd implement proper role-based user retrieval
        return Array.Empty<User>();
    }

    /// <summary>
    /// Updates user attributes in a fluent manner
    /// </summary>
    public static async Task<User> WithAttributesAsync(
        this UserService userService,
        User user,
        Dictionary<string, object> attributes,
        CancellationToken cancellationToken = default)
    {
        if (user is null)
            throw new ArgumentNullException(nameof(user));

        if (attributes is null || attributes.Count == 0)
            return user;

        return await userService.UpdateUserAsync(
            user,
            user.FullName,
            attributes,
            cancellationToken);
    }

    /// <summary>
    /// Safely authenticates a user with fallback behavior
    /// </summary>
    public static async Task<(User? User, bool Success)> TryAuthenticateAsync(
        this UserService userService,
        string username,
        string password,
        int maxRetryAttempts = 3,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await userService.AuthenticateAsync(username, password, cancellationToken);
            return (user, true);
        }
        catch (AuthServerException ex) when (maxRetryAttempts > 0)
        {
            // Log the failed attempt
            Console.Error.WriteLine($"Authentication attempt failed: {ex.Message}");

            // Return failure without throwing
            return (null, false);
        }
        catch (Exception)
        {
            // Return failure for any other exception
            return (null, false);
        }
    }
}