#nullable enable

using System;
using System.Collections.Generic;

namespace DotnetAuthServer.Configuration;

/// <summary>
/// Extension methods for <see cref="AuthServerOptions"/> providing convenient configuration helpers
/// </summary>
public static class AuthServerOptionsExtensions
{
    /// <summary>
    /// Validates that the required configuration values are properly set
    /// </summary>
    /// <param name="options">The auth server options to validate</param>
    /// <returns>True if validation passes</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null or when required properties are null or empty</exception>
    public static bool Validate(this AuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(options.IssuerUrl))
        {
            throw new ArgumentNullException(nameof(options.IssuerUrl), "IssuerUrl must be configured");
        }

        if (string.IsNullOrWhiteSpace(options.JwtSigningKey))
        {
            throw new ArgumentNullException(nameof(options.JwtSigningKey), "JWT signing key must be configured");
        }

        if (string.IsNullOrWhiteSpace(options.JwtAlgorithm))
        {
            throw new ArgumentNullException(nameof(options.JwtAlgorithm), "JWT algorithm must be specified");
        }

        if (options.AccessTokenLifetimeSeconds <= 0)
        {
            throw new ArgumentException("AccessTokenLifetimeSeconds must be greater than 0", nameof(options.AccessTokenLifetimeSeconds));
        }

        if (options.SupportedScopes is not { Count: > 0 })
        {
            throw new ArgumentException("At least one supported scope must be configured", nameof(options.SupportedScopes));
        }

        if (options.SupportedGrantTypes is not { Count: > 0 })
        {
            throw new ArgumentException("At least one supported grant type must be configured", nameof(options.SupportedGrantTypes));
        }

        return true;
    }

    /// <summary>
    /// Checks if a specific scope is supported by the authorization server
    /// </summary>
    /// <param name="options">The auth server options</param>
    /// <param name="scope">The scope to check</param>
    /// <returns>True if the scope is supported, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="scope"/> is null</exception>
    public static bool SupportsScope(this AuthServerOptions options, string scope)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(scope);

        return options.SupportedScopes.Contains(scope, StringComparer.Ordinal);
    }

    /// <summary>
    /// Checks if a specific grant type is supported by the authorization server
    /// </summary>
    /// <param name="options">The auth server options</param>
    /// <param name="grantType">The grant type to check</param>
    /// <returns>True if the grant type is supported, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="grantType"/> is null</exception>
    public static bool SupportsGrantType(this AuthServerOptions options, string grantType)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(grantType);

        return options.SupportedGrantTypes.Contains(grantType, StringComparer.Ordinal);
    }

    /// <summary>
    /// Gets the access token lifetime as a TimeSpan for easier time-based calculations
    /// </summary>
    /// <param name="options">The auth server options</param>
    /// <returns>TimeSpan representing the access token lifetime</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
    public static TimeSpan GetAccessTokenLifetime(this AuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return TimeSpan.FromSeconds(options.AccessTokenLifetimeSeconds);
    }

    /// <summary>
    /// Gets the refresh token lifetime as a TimeSpan for easier time-based calculations
    /// </summary>
    /// <param name="options">The auth server options</param>
    /// <returns>TimeSpan representing the refresh token lifetime</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
    public static TimeSpan GetRefreshTokenLifetime(this AuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return TimeSpan.FromSeconds(options.RefreshTokenLifetimeSeconds);
    }

    /// <summary>
    /// Gets the authorization code lifetime as a TimeSpan for easier time-based calculations
    /// </summary>
    /// <param name="options">The auth server options</param>
    /// <returns>TimeSpan representing the authorization code lifetime</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
    public static TimeSpan GetAuthorizationCodeLifetime(this AuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return TimeSpan.FromSeconds(options.AuthorizationCodeLifetimeSeconds);
    }

    /// <summary>
    /// Gets the clock skew tolerance as a TimeSpan for easier time-based calculations
    /// </summary>
    /// <param name="options">The auth server options</param>
    /// <returns>TimeSpan representing the clock skew tolerance</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
    public static TimeSpan GetClockSkewTolerance(this AuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return TimeSpan.FromSeconds(options.ClockSkewToleranceSeconds);
    }

    /// <summary>
    /// Gets the account lockout duration as a TimeSpan for easier time-based calculations
    /// </summary>
    /// <param name="options">The auth server options</param>
    /// <returns>TimeSpan representing the account lockout duration</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
    public static TimeSpan GetAccountLockoutDuration(this AuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return TimeSpan.FromMinutes(options.AccountLockoutDurationMinutes);
    }

    /// <summary>
    /// Checks if PKCE is required for a specific client based on the global setting
    /// </summary>
    /// <param name="options">The auth server options</param>
    /// <returns>True if PKCE is required for all clients, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
    public static bool IsPkceRequired(this AuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.RequirePkceForAllClients;
    }

    /// <summary>
    /// Checks if token rotation is enabled for refresh tokens
    /// </summary>
    /// <param name="options">The auth server options</param>
    /// <returns>True if auto-rotation is enabled, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
    public static bool IsTokenRotationEnabled(this AuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.AutoRefreshTokenRotation;
    }

    /// <summary>
    /// Gets the maximum number of times a refresh token can be used before being invalidated
    /// </summary>
    /// <param name="options">The auth server options</param>
    /// <returns>The maximum refresh token generations allowed</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
    public static int GetMaxRefreshTokenGenerations(this AuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.MaxRefreshTokenGenerations;
    }

    /// <summary>
    /// Gets the failed login attempt threshold before account lockout
    /// </summary>
    /// <param name="options">The auth server options</param>
    /// <returns>The number of failed attempts allowed before lockout</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
    public static int GetFailedLoginAttemptThreshold(this AuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.FailedLoginAttemptThreshold;
    }

    /// <summary>
    /// Checks if user consent is required before issuing tokens
    /// </summary>
    /// <param name="options">The auth server options</param>
    /// <returns>True if user consent is required, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
    public static bool IsUserConsentRequired(this AuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.RequireUserConsent;
    }

    /// <summary>
    /// Checks if in-memory database is configured (useful for testing scenarios)
    /// </summary>
    /// <param name="options">The auth server options</param>
    /// <returns>True if in-memory database is enabled, false otherwise</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null</exception>
    public static bool UsesInMemoryDatabase(this AuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return options.UseInMemoryDatabase;
    }
}