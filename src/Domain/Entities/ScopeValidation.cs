#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Entities;

/// <summary>
/// Provides validation helpers for <see cref="Scope"/> entities
/// </summary>
public static class ScopeValidation
{
    /// <summary>
    /// Validates a Scope entity and returns any validation problems
    /// </summary>
    /// <param name="value">The scope to validate</param>
    /// <returns>An empty list if valid, otherwise a list of human-readable problems</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this Scope value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate ScopeId
        if (string.IsNullOrWhiteSpace(value.ScopeId))
        {
            problems.Add("ScopeId cannot be null or whitespace");
        }
        else if (value.ScopeId.Length > 128)
        {
            problems.Add("ScopeId exceeds maximum length of 128 characters");
        }
        else if (value.ScopeId.Any(static c => char.IsWhiteSpace(c)))
        {
            problems.Add("ScopeId cannot contain whitespace characters");
        }

        // Validate DisplayName
        if (string.IsNullOrWhiteSpace(value.DisplayName))
        {
            problems.Add("DisplayName cannot be null or whitespace");
        }
        else if (value.DisplayName.Length > 256)
        {
            problems.Add("DisplayName exceeds maximum length of 256 characters");
        }

        // Validate Description
        if (string.IsNullOrWhiteSpace(value.Description))
        {
            problems.Add("Description cannot be null or whitespace");
        }
        else if (value.Description.Length > 2048)
        {
            problems.Add("Description exceeds maximum length of 2048 characters");
        }

        // Validate timestamps
        if (value.CreatedAt == default)
        {
            problems.Add("CreatedAt must be set to a valid DateTime");
        }
        else if (value.CreatedAt > DateTime.UtcNow.AddMinutes(5))
        {
            problems.Add("CreatedAt cannot be in the future");
        }

        if (value.UpdatedAt == default)
        {
            problems.Add("UpdatedAt must be set to a valid DateTime");
        }
        else if (value.UpdatedAt > DateTime.UtcNow.AddMinutes(5))
        {
            problems.Add("UpdatedAt cannot be in the future");
        }

        // Validate claims collections
        if (value.IdTokenClaims is null)
        {
            problems.Add("IdTokenClaims collection cannot be null");
        }
        else if (value.IdTokenClaims.Any(static claim => string.IsNullOrWhiteSpace(claim)))
        {
            problems.Add("IdTokenClaims collection contains null or whitespace entries");
        }

        if (value.AccessTokenClaims is null)
        {
            problems.Add("AccessTokenClaims collection cannot be null");
        }
        else if (value.AccessTokenClaims.Any(static claim => string.IsNullOrWhiteSpace(claim)))
        {
            problems.Add("AccessTokenClaims collection contains null or whitespace entries");
        }

        if (value.AllowedRoles is null)
        {
            problems.Add("AllowedRoles collection cannot be null");
        }
        else if (value.AllowedRoles.Any(static role => string.IsNullOrWhiteSpace(role)))
        {
            problems.Add("AllowedRoles collection contains null or whitespace entries");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a Scope entity is valid
    /// </summary>
    /// <param name="value">The scope to check</param>
    /// <returns>True if the scope is valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static bool IsValid(this Scope value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that a Scope entity is valid, throwing an exception if it is not
    /// </summary>
    /// <param name="value">The scope to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when the scope is invalid, containing a list of problems</exception>
    public static void EnsureValid(this Scope value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Scope validation failed:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }
}
