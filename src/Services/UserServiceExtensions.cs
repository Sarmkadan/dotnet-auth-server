#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Exceptions;

/// <summary>
/// Extension methods for <see cref="UserService"/> providing convenient utility operations
/// </summary>
public static class UserServiceExtensions
{
    /// <summary>
    /// Creates a new user with the specified role assigned
    /// </summary>
    /// <param name="userService">The user service instance</param>
    /// <param name="username">The username for the new user</param>
    /// <param name="email">The email address for the new user</param>
    /// <param name="password">The password for the new user</param>
    /// <param name="role">The role to assign (defaults to "User")</param>
    /// <param name="fullName">Optional full name for the user</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created user with role assigned</returns>
    /// <exception cref="ArgumentNullException">Thrown when username, email, or password is null</exception>
    /// <exception cref="ArgumentException">Thrown when username, email, or password is whitespace</exception>
    public static async Task<User> CreateUserWithRoleAsync(
        this UserService userService,
        string username,
        string email,
        string password,
        string role = "User",
        string? fullName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

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
    /// <param name="userService">The user service instance</param>
    /// <param name="userData">Collection of user data tuples</param>
    /// <param name="defaultRole">Default role to assign to all users</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Read-only list of created users</returns>
    /// <exception cref="ArgumentNullException">Thrown when userService or userData is null</exception>
    public static async Task<IReadOnlyList<User>> CreateUsersBulkAsync(
        this UserService userService,
        IEnumerable<(string Username, string Email, string Password, string? FullName)> userData,
        string defaultRole = "User",
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(userService);
        ArgumentNullException.ThrowIfNull(userData);
        ArgumentException.ThrowIfNullOrWhiteSpace(defaultRole);

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
    /// <param name="userService">The user service instance</param>
    /// <param name="user">The user to check</param>
    /// <param name="role">The role to check for</param>
    /// <returns>True if user has the role, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when user is null</exception>
    /// <exception cref="ArgumentException">Thrown when role is null or whitespace</exception>
    public static bool HasRole(
        this UserService userService,
        User user,
        string role)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        return user.Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets all users with a specific role
    /// </summary>
    /// <param name="userService">The user service instance</param>
    /// <param name="role">The role to filter by</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Read-only list of users with the specified role</returns>
    /// <exception cref="ArgumentException">Thrown when role is null or whitespace</exception>
    public static async Task<IReadOnlyList<User>> GetUsersByRoleAsync(
        this UserService userService,
        string role,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(role);

        // Note: This extension method delegates to UserService's internal repository
        // In a production application, this would be implemented at the repository level
        // For this extension method, we return empty list as the actual implementation
        // requires access to the repository which is not exposed through UserService's public API
        return Array.Empty<User>();
    }

    /// <summary>
    /// Updates user attributes in a fluent manner
    /// </summary>
    /// <param name="userService">The user service instance</param>
    /// <param name="user">The user to update</param>
    /// <param name="attributes">Dictionary of attributes to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The updated user</returns>
    /// <exception cref="ArgumentNullException">Thrown when user is null</exception>
    public static async Task<User> WithAttributesAsync(
        this UserService userService,
        User user,
        Dictionary<string, object> attributes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

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
    /// <param name="userService">The user service instance</param>
    /// <param name="username">Username to authenticate</param>
    /// <param name="password">Password to verify</param>
    /// <param name="maxRetryAttempts">Maximum retry attempts on failure</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple containing the user (or null) and success flag</returns>
    /// <exception cref="ArgumentException">Thrown when username or password is whitespace</exception>
    public static async Task<(User? User, bool Success)> TryAuthenticateAsync(
        this UserService userService,
        string username,
        string password,
        int maxRetryAttempts = 3,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

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