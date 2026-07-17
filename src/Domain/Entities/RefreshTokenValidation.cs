#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace DotnetAuthServer.Domain.Entities;

/// <summary>
/// Provides validation helpers for <see cref="RefreshToken"/> instances
/// </summary>
public static class RefreshTokenValidation
{
    private const string DateTimeDefaultError = "{0} must be a valid date, but was default(DateTime)";
    private const string NonEmptyStringError = "{0} must be a non-empty string";

    /// <summary>
    /// Validates a refresh token and returns a list of human-readable problems
    /// </summary>
    /// <param name="value">The refresh token to validate</param>
    /// <returns>List of validation errors; empty if valid</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this RefreshToken? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate required string properties
        ValidateNonEmptyString(value.TokenId, nameof(value.TokenId), errors);
        ValidateNonEmptyString(value.TokenHash, nameof(value.TokenHash), errors);
        ValidateNonEmptyString(value.ClientId, nameof(value.ClientId), errors);
        ValidateNonEmptyString(value.UserId, nameof(value.UserId), errors);
        ValidateNonEmptyString(value.GrantedScopes, nameof(value.GrantedScopes), errors);

        // Validate optional string properties
        ValidateNonEmptyString(value.PreviousTokenHash, nameof(value.PreviousTokenHash), errors);
        ValidateNonEmptyString(value.RevocationReason, nameof(value.RevocationReason), errors);
        ValidateNonEmptyString(value.IssuedToDeviceId, nameof(value.IssuedToDeviceId), errors);

        // Validate Version (must be positive)
        if (value.Version <= 0)
        {
            errors.Add($"Version must be a positive integer, but was {value.Version}");
        }

        // Validate UsageCount (must be non-negative)
        if (value.UsageCount < 0)
        {
            errors.Add($"UsageCount must be non-negative, but was {value.UsageCount}");
        }

        // Validate required dates
        ValidateDateNotDefault(value.ExpiresAt, nameof(value.ExpiresAt), errors);
        ValidateDateNotDefault(value.CreatedAt, nameof(value.CreatedAt), errors);
        ValidateDateNotDefault(value.UpdatedAt, nameof(value.UpdatedAt), errors);

        // Validate RevokedAt if not null
        if (value.RevokedAt.HasValue)
        {
            ValidateDateNotDefault(value.RevokedAt.Value, nameof(value.RevokedAt), errors);

            // RevokedAt must be after CreatedAt
            if (value.RevokedAt.Value <= value.CreatedAt)
            {
                errors.Add("RevokedAt must be after CreatedAt");
            }
        }

        // Validate LastUsedAt if not null
        if (value.LastUsedAt.HasValue)
        {
            ValidateDateNotDefault(value.LastUsedAt.Value, nameof(value.LastUsedAt), errors);

            // LastUsedAt must be after CreatedAt
            if (value.LastUsedAt.Value <= value.CreatedAt)
            {
                errors.Add("LastUsedAt must be after CreatedAt");
            }

            // LastUsedAt must be before or equal to UpdatedAt
            if (value.LastUsedAt.Value > value.UpdatedAt)
            {
                errors.Add("LastUsedAt must be before or equal to UpdatedAt");
            }
        }

        // Validate expiration is in the future
        if (value.ExpiresAt <= DateTime.UtcNow)
        {
            errors.Add("ExpiresAt must be in the future");
        }

        // Validate RevokedAt and RevocationReason consistency
        if (value.IsRevoked && value.RevokedAt is null)
        {
            errors.Add("IsRevoked is true but RevokedAt is null");
        }

        if (value.IsRevoked && value.RevocationReason is null)
        {
            errors.Add("IsRevoked is true but RevocationReason is null");
        }

        if (!value.IsRevoked && value.RevokedAt is not null)
        {
            errors.Add("IsRevoked is false but RevokedAt has a value");
        }

        if (!value.IsRevoked && value.RevocationReason is not null)
        {
            errors.Add("IsRevoked is false but RevocationReason has a value");
        }

        // Validate UsageCount consistency with LastUsedAt
        if (value.LastUsedAt is null && value.UsageCount > 0)
        {
            errors.Add("UsageCount is greater than 0 but LastUsedAt is null");
        }

        if (value.LastUsedAt is not null && value.UsageCount == 0)
        {
            errors.Add("UsageCount is 0 but LastUsedAt has a value");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Checks if a refresh token is valid
    /// </summary>
    /// <param name="value">The refresh token to check</param>
    /// <returns>True if valid; false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static bool IsValid(this RefreshToken? value)
        => Validate(value).Count == 0;

    /// <summary>
    /// Ensures a refresh token is valid, throwing an exception with all validation errors if not
    /// </summary>
    /// <param name="value">The refresh token to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when the refresh token is invalid</exception>
    public static void EnsureValid(this RefreshToken? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"Refresh token is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
    }

    private static void ValidateNonEmptyString(string? value, string propertyName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(string.Format(NonEmptyStringError, propertyName));
        }
    }

    private static void ValidateDateNotDefault(DateTime date, string propertyName, List<string> errors)
    {
        if (date == default)
        {
            errors.Add(string.Format(DateTimeDefaultError, propertyName));
        }
    }
}