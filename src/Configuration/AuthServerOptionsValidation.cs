#nullable enable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DotnetAuthServer.Configuration;

/// <summary>
/// Provides validation helpers for <see cref="AuthServerOptions"/> configuration options.
/// </summary>
public static class AuthServerOptionsValidation
{
    private const int MinJwtSigningKeyLength = 32;
    private const int MaxJwtAlgorithmLength = 50;
    private const int MaxUrlLength = 2048;
    private const int MaxConnectionStringLength = 2048;
    private const int MaxGrantTypeLength = 100;
    private const int MaxScopeLength = 500;
    private const int MaxClientIdLength = 256;

    /// <summary>
    /// Validates the specified <see cref="AuthServerOptions"/> instance.
    /// Returns a list of human-readable validation problems, or an empty list if valid.
    /// </summary>
    /// <param name="value">The options to validate.</param>
    /// <returns>A read-only list of validation error messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this AuthServerOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate IssuerUrl
        if (string.IsNullOrWhiteSpace(value.IssuerUrl))
        {
            errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.IssuerUrl)} must be a non-empty, non-whitespace string.");
        }
        else if (value.IssuerUrl.Length > MaxUrlLength)
        {
            errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.IssuerUrl)} length must not exceed {MaxUrlLength} characters, but was {value.IssuerUrl.Length}.");
        }
        else if (!Uri.TryCreate(value.IssuerUrl, UriKind.Absolute, out _) || !(value.IssuerUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || value.IssuerUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.IssuerUrl)} must be a valid absolute HTTP/HTTPS URL.");
        }

        // Validate JwtSigningKey
        if (string.IsNullOrWhiteSpace(value.JwtSigningKey))
        {
            errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.JwtSigningKey)} must be a non-empty, non-whitespace string.");
        }
        else if (value.JwtSigningKey.Length < MinJwtSigningKeyLength)
        {
            errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.JwtSigningKey)} must be at least {MinJwtSigningKeyLength} characters long for security, but was {value.JwtSigningKey.Length}.");
        }

        // Validate JwtAlgorithm
        if (string.IsNullOrWhiteSpace(value.JwtAlgorithm))
        {
            errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.JwtAlgorithm)} must be a non-empty, non-whitespace string.");
        }
        else if (value.JwtAlgorithm.Length > MaxJwtAlgorithmLength)
        {
            errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.JwtAlgorithm)} length must not exceed {MaxJwtAlgorithmLength} characters, but was {value.JwtAlgorithm.Length}.");
        }

        // Validate token lifetimes
        if (value.AccessTokenLifetimeSeconds <= 0)
        {
            errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.AccessTokenLifetimeSeconds)} must be a positive integer, but was {value.AccessTokenLifetimeSeconds}.");
        }

        if (value.RefreshTokenLifetimeSeconds <= 0)
        {
            errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.RefreshTokenLifetimeSeconds)} must be a positive integer, but was {value.RefreshTokenLifetimeSeconds}.");
        }

        if (value.AuthorizationCodeLifetimeSeconds <= 0)
        {
            errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.AuthorizationCodeLifetimeSeconds)} must be a positive integer, but was {value.AuthorizationCodeLifetimeSeconds}.");
        }

        // Validate MaxRefreshTokenGenerations
        if (value.MaxRefreshTokenGenerations <= 0)
        {
            errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.MaxRefreshTokenGenerations)} must be a positive integer, but was {value.MaxRefreshTokenGenerations}.");
        }

        // Validate ClockSkewToleranceSeconds
        if (value.ClockSkewToleranceSeconds < 0)
        {
            errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.ClockSkewToleranceSeconds)} must be a non-negative integer, but was {value.ClockSkewToleranceSeconds}.");
        }

        // Validate DatabaseConnectionString
        if (string.IsNullOrWhiteSpace(value.DatabaseConnectionString))
        {
            errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.DatabaseConnectionString)} must be a non-empty, non-whitespace string.");
        }
        else if (value.DatabaseConnectionString.Length > MaxConnectionStringLength)
        {
            errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.DatabaseConnectionString)} length must not exceed {MaxConnectionStringLength} characters, but was {value.DatabaseConnectionString.Length}.");
        }

        // Validate FailedLoginAttemptThreshold
        if (value.FailedLoginAttemptThreshold <= 0)
        {
            errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.FailedLoginAttemptThreshold)} must be a positive integer, but was {value.FailedLoginAttemptThreshold}.");
        }

        // Validate AccountLockoutDurationMinutes
        if (value.AccountLockoutDurationMinutes < 0)
        {
            errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.AccountLockoutDurationMinutes)} must be a non-negative integer, but was {value.AccountLockoutDurationMinutes}.");
        }

        // Validate SupportedScopes collection
        if (value.SupportedScopes is null)
        {
            errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.SupportedScopes)} must not be null.");
        }
        else
        {
            if (value.SupportedScopes.Count == 0)
            {
                errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.SupportedScopes)} must contain at least one scope.");
            }

            for (var i = 0; i < value.SupportedScopes.Count; i++)
            {
                var scope = value.SupportedScopes.ElementAt(i);
                if (string.IsNullOrWhiteSpace(scope))
                {
                    errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.SupportedScopes)}[{i}] must be a non-empty, non-whitespace string.");
                }
                else if (scope.Length > MaxScopeLength)
                {
                    errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.SupportedScopes)}[{i}] length must not exceed {MaxScopeLength} characters, but was {scope.Length}.");
                }
            }
        }

        // Validate SupportedGrantTypes collection
        if (value.SupportedGrantTypes is null)
        {
            errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.SupportedGrantTypes)} must not be null.");
        }
        else
        {
            if (value.SupportedGrantTypes.Count == 0)
            {
                errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.SupportedGrantTypes)} must contain at least one grant type.");
            }

            for (var i = 0; i < value.SupportedGrantTypes.Count; i++)
            {
                var grantType = value.SupportedGrantTypes.ElementAt(i);
                if (string.IsNullOrWhiteSpace(grantType))
                {
                    errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.SupportedGrantTypes)}[{i}] must be a non-empty, non-whitespace string.");
                }
                else if (grantType.Length > MaxGrantTypeLength)
                {
                    errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.SupportedGrantTypes)}[{i}] length must not exceed {MaxGrantTypeLength} characters, but was {grantType.Length}.");
                }
                else if (!IsValidGrantType(grantType))
                {
                    errors.Add($"{nameof(AuthServerOptions)}.{nameof(value.SupportedGrantTypes)}[{i}] has invalid grant type: '{grantType}'. Valid values are: {string.Join(", ", GetValidGrantTypes())}.");
                }
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="AuthServerOptions"/> instance is valid.
    /// </summary>
    /// <param name="value">The options to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this AuthServerOptions value) => value is not null && Validate(value).Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="AuthServerOptions"/> instance is valid.
    /// </summary>
    /// <param name="value">The options to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the instance is invalid, containing a list of validation errors.</exception>
    public static void EnsureValid(this AuthServerOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"AuthServerOptions validation failed. Details:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}",
                nameof(value));
        }
    }

    /// <summary>
    /// Checks if a grant type string is valid.
    /// </summary>
    /// <param name="grantType">The grant type to validate.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    private static bool IsValidGrantType(string grantType)
    {
        return grantType switch
        {
            Constants.GrantTypes.AuthorizationCode => true,
            Constants.GrantTypes.RefreshToken => true,
            Constants.GrantTypes.ClientCredentials => true,
            Constants.GrantTypes.Password => true,
            Constants.GrantTypes.Implicit => true,
            Constants.GrantTypes.Hybrid => true,
            Constants.GrantTypes.TokenExchange => true,
            _ => false
        };
    }

    /// <summary>
    /// Gets a comma-separated list of valid grant types for error messages.
    /// </summary>
    /// <returns>A string containing all valid grant types.</returns>
    private static string GetValidGrantTypes()
    {
        return $"{Constants.GrantTypes.AuthorizationCode}, {Constants.GrantTypes.RefreshToken}, {Constants.GrantTypes.ClientCredentials}, {Constants.GrantTypes.Password}, {Constants.GrantTypes.Implicit}, {Constants.GrantTypes.Hybrid}, {Constants.GrantTypes.TokenExchange}";
    }
}