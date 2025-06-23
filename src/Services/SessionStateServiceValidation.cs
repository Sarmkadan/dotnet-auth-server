#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Services;

using System.Collections.Frozen;
using System.Globalization;
using DotnetAuthServer.Extensions;

/// <summary>
/// Provides validation helpers for <see cref="SessionState"/> instances.
/// Validates session state properties according to business rules and constraints.
/// </summary>
public static class SessionStateServiceValidation
{
    private static readonly FrozenSet<string> _requiredNonEmptyFields =
        new[] { "StateId", "ClientId", "RedirectUri", "RequestedScopes" }.ToFrozenSet(StringComparer.Ordinal);

    private const int MaxScopeLength = 1024;
    private const int MaxClientIdLength = 256;
    private const int MaxRedirectUriLength = 2048;
    private const int MaxNonceLength = 256;
    private const int MaxUserIdLength = 256;
    private const int MaxGrantedScopesLength = 2048;

    /// <summary>
    /// Validates all properties of a <see cref="SessionState"/> instance.
    /// Returns a list of human-readable validation problems, or an empty list if valid.
    /// </summary>
    /// <param name="value">The session state instance to validate.</param>
    /// <returns>A read-only list of validation error messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this SessionState? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate required non-empty string fields
        ValidateNonEmptyString(nameof(value.StateId), value.StateId, errors);
        ValidateNonEmptyString(nameof(value.ClientId), value.ClientId, errors);
        ValidateNonEmptyString(nameof(value.RedirectUri), value.RedirectUri, errors);
        ValidateNonEmptyString(nameof(value.RequestedScopes), value.RequestedScopes, errors);

        // Validate string length constraints
        ValidateStringLength(nameof(value.ClientId), value.ClientId, MaxClientIdLength, errors);
        ValidateStringLength(nameof(value.RedirectUri), value.RedirectUri, MaxRedirectUriLength, errors);
        ValidateStringLength(nameof(value.RequestedScopes), value.RequestedScopes, MaxScopeLength, errors);

        if (value.GrantedScopes is not null)
        {
            ValidateStringLength(nameof(value.GrantedScopes), value.GrantedScopes, MaxGrantedScopesLength, errors);
        }

        if (value.Nonce is not null)
        {
            ValidateStringLength(nameof(value.Nonce), value.Nonce, MaxNonceLength, errors);
        }

        if (value.UserId is not null)
        {
            ValidateStringLength(nameof(value.UserId), value.UserId, MaxUserIdLength, errors);
        }

        // Validate date ranges
        if (value.CreatedAt == default)
        {
            errors.Add($"{nameof(value.CreatedAt)} must be set to a non-default DateTime value.");
        }
        else if (value.CreatedAt.Kind != DateTimeKind.Utc)
        {
            errors.Add($"{nameof(value.CreatedAt)} must be in UTC timezone, but was {value.CreatedAt.Kind}.");
        }

        if (value.ExpiresAt == default)
        {
            errors.Add($"{nameof(value.ExpiresAt)} must be set to a non-default DateTime value.");
        }
        else if (value.ExpiresAt.Kind != DateTimeKind.Utc)
        {
            errors.Add($"{nameof(value.ExpiresAt)} must be in UTC timezone, but was {value.ExpiresAt.Kind}.");
        }
        else if (value.ExpiresAt <= value.CreatedAt)
        {
            errors.Add($"{nameof(value.ExpiresAt)} must be after {nameof(value.CreatedAt)}.");
        }

        if (value.LastUpdatedAt.HasValue)
        {
            if (value.LastUpdatedAt.Value.Kind != DateTimeKind.Utc)
            {
                errors.Add($"{nameof(value.LastUpdatedAt)} must be in UTC timezone if set, but was {value.LastUpdatedAt.Value.Kind}.");
            }
            else if (value.LastUpdatedAt.Value < value.CreatedAt)
            {
                errors.Add($"{nameof(value.LastUpdatedAt)} must be after {nameof(value.CreatedAt)} if set.");
            }
        }

        // Validate business logic constraints
        if (value.ExpiresAt.IsExpired())
        {
            errors.Add($"{nameof(value.ExpiresAt)} value indicates the session has already expired.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="SessionState"/> instance is valid.
    /// </summary>
    /// <param name="value">The session state instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this SessionState? value)
    {
        return value is not null && Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="SessionState"/> instance is valid.
    /// Throws an <see cref="ArgumentException"/> with detailed validation messages if invalid.
    /// </summary>
    /// <param name="value">The session state instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is invalid, containing validation error messages.</exception>
    public static void EnsureValid(this SessionState value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"SessionState validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    private static void ValidateNonEmptyString(string fieldName, string fieldValue, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(fieldValue))
        {
            errors.Add($"{fieldName} must be a non-empty, non-whitespace string.");
        }
    }

    private static void ValidateStringLength(string fieldName, string fieldValue, int maxLength, List<string> errors)
    {
        if (fieldValue.Length > maxLength)
        {
            errors.Add($"{fieldName} length must not exceed {maxLength} characters, but was {fieldValue.Length}.");
        }
    }
}