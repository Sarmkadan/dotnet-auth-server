using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DotnetAuthServer.Configuration;

/// <summary>
/// Extension methods for <see cref="DotnetAuthServerOptions"/> that provide convenient accessors
/// and validation helpers for the authorization server configuration.
/// </summary>
public static class DotnetAuthServerOptionsExtensions
{
    /// <summary>
    /// Validates that the authorization server configuration is valid and complete.
    /// </summary>
    /// <param name="options">The options to validate.</param>
    /// <returns>True if the configuration is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    public static bool IsValid(this DotnetAuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.AuthServer?.IsValid() == true
            && options.Cache is not null
            && options.Logging?.IsValid() == true
            && options.Opa is not null
            && !string.IsNullOrWhiteSpace(options.AuthServer?.IssuerUrl)
            && !string.IsNullOrWhiteSpace(options.AuthServer?.JwtSigningKey)
            && options.AuthServer?.AccessTokenLifetimeSeconds > 0
            && options.AuthServer?.RefreshTokenLifetimeSeconds > 0;
    }

    /// <summary>
    /// Gets the effective cache backend type, normalizing the value to a consistent format.
    /// Converts "Memory", "memory", "MEMORY", etc. to "Memory"; "Redis", "redis", "REDIS" to "Redis".
    /// </summary>
    /// <param name="options">The options containing cache configuration.</param>
    /// <returns>The normalized cache backend type.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options.Cache"/> is null.</exception>
    public static string GetEffectiveCacheBackend(this DotnetAuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Cache);

        return options.Cache.Backend.Trim().ToLowerInvariant() switch
        {
            "memory" => "Memory",
            "redis" => "Redis",
            var other => other
        };
    }

    /// <summary>
    /// Determines whether the cache is configured to use Redis as the backend.
    /// </summary>
    /// <param name="options">The options containing cache configuration.</param>
    /// <returns>True if Redis is the cache backend; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options.Cache"/> is null.</exception>
    public static bool UsesRedisCache(this DotnetAuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Cache);

        return string.Equals(
            options.GetEffectiveCacheBackend(),
            "Redis",
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the effective JWT signing algorithm, defaulting to HS256 if not specified.
    /// </summary>
    /// <param name="options">The options containing auth server configuration.</param>
    /// <returns>The JWT signing algorithm to use.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options.AuthServer"/> is null.</exception>
    public static string GetEffectiveJwtAlgorithm(this DotnetAuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.AuthServer);

        return string.IsNullOrWhiteSpace(options.AuthServer.JwtAlgorithm)
            ? "HS256"
            : options.AuthServer.JwtAlgorithm;
    }

    /// <summary>
    /// Gets the collection of supported scopes as a read-only list for thread-safe access.
    /// </summary>
    /// <param name="options">The options containing auth server configuration.</param>
    /// <returns>A read-only list of supported scopes.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options.AuthServer"/> is null.</exception>
    public static IReadOnlyList<string> GetSupportedScopes(this DotnetAuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.AuthServer);

        return ((IList<string>)options.AuthServer.SupportedScopes).AsReadOnly();
    }

    /// <summary>
    /// Gets the collection of supported grant types as a read-only list for thread-safe access.
    /// </summary>
    /// <param name="options">The options containing auth server configuration.</param>
    /// <returns>A read-only list of supported grant types.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options.AuthServer"/> is null.</exception>
    public static IReadOnlyList<string> GetSupportedGrantTypes(this DotnetAuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.AuthServer);

        return ((IList<string>)options.AuthServer.SupportedGrantTypes).AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified scope is supported by the authorization server.
    /// </summary>
    /// <param name="options">The options containing auth server configuration.</param>
    /// <param name="scope">The scope to check.</param>
    /// <returns>True if the scope is supported; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options.AuthServer"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="scope"/> is null or whitespace.</exception>
    public static bool SupportsScope(this DotnetAuthServerOptions options, string scope)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.AuthServer);
        ArgumentException.ThrowIfNullOrWhiteSpace(scope);

        var supportedScopes = options.GetSupportedScopes();
        return supportedScopes.Contains(scope, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Determines whether the specified grant type is supported by the authorization server.
    /// </summary>
    /// <param name="options">The options containing auth server configuration.</param>
    /// <param name="grantType">The grant type to check.</param>
    /// <returns>True if the grant type is supported; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options.AuthServer"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="grantType"/> is null or whitespace.</exception>
    public static bool SupportsGrantType(this DotnetAuthServerOptions options, string grantType)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.AuthServer);
        ArgumentException.ThrowIfNullOrWhiteSpace(grantType);

        var supportedGrants = options.GetSupportedGrantTypes();
        return supportedGrants.Contains(grantType, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the effective minimum log level as a string, accounting for potential null values.
    /// </summary>
    /// <param name="options">The options containing logging configuration.</param>
    /// <returns>The effective minimum log level as a string.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options.Logging"/> is null.</exception>
    public static string GetEffectiveMinimumLogLevel(this DotnetAuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Logging);

        return options.Logging.MinimumLevel.ToString();
    }

    /// <summary>
    /// Determines whether sensitive data logging is enabled.
    /// </summary>
    /// <param name="options">The options containing logging configuration.</param>
    /// <returns>True if sensitive data logging is enabled; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options.Logging"/> is null.</exception>
    public static bool IsSensitiveDataLoggingEnabled(this DotnetAuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Logging);

        return options.Logging.LogSensitiveData;
    }

    /// <summary>
    /// Gets the effective OPA policy path, ensuring it's properly formatted for API calls.
    /// </summary>
    /// <param name="options">The options containing OPA configuration.</param>
    /// <returns>The formatted OPA policy path.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options.Opa"/> is null.</exception>
    public static string GetEffectiveOpaPolicyPath(this DotnetAuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Opa);

        var path = options.Opa.PolicyPath?.Trim() ?? string.Empty;
        return path.StartsWith('/')
            ? path[1..]
            : path;
    }

    /// <summary>
    /// Gets the OPA policy query URL by combining the base URL and policy path.
    /// </summary>
    /// <param name="options">The options containing OPA configuration.</param>
    /// <returns>The complete OPA policy query URL.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options.Opa"/> is null.</exception>
    public static string GetOpaPolicyUrl(this DotnetAuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Opa);

        var baseUrl = options.Opa.BaseUrl?.TrimEnd('/') ?? string.Empty;
        var path = options.GetEffectiveOpaPolicyPath();

        return $"{baseUrl}/v1/data/{path}";
    }

    /// <summary>
    /// Gets the effective access token lifetime in a human-readable format.
    /// </summary>
    /// <param name="options">The options containing auth server configuration.</param>
    /// <returns>A formatted string representing the access token lifetime.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options.AuthServer"/> is null.</exception>
    public static string GetAccessTokenLifetimeDisplay(this DotnetAuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.AuthServer);

        var seconds = options.AuthServer.AccessTokenLifetimeSeconds;
        return FormatTimeSpan(seconds);
    }

    /// <summary>
    /// Gets the effective refresh token lifetime in a human-readable format.
    /// </summary>
    /// <param name="options">The options containing auth server configuration.</param>
    /// <returns>A formatted string representing the refresh token lifetime.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options.AuthServer"/> is null.</exception>
    public static string GetRefreshTokenLifetimeDisplay(this DotnetAuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.AuthServer);

        var seconds = options.AuthServer.RefreshTokenLifetimeSeconds;
        return FormatTimeSpan(seconds);
    }

    /// <summary>
    /// Gets the effective cache default expiration in a human-readable format.
    /// </summary>
    /// <param name="options">The options containing cache configuration.</param>
    /// <returns>A formatted string representing the cache default expiration.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options.Cache"/> is null.</exception>
    public static string GetCacheDefaultExpirationDisplay(this DotnetAuthServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.Cache);

        var seconds = options.Cache.DefaultExpirationSeconds;
        return FormatTimeSpan(seconds);
    }

    private static string FormatTimeSpan(int totalSeconds)
    {
        var timeSpan = TimeSpan.FromSeconds(totalSeconds);
        return timeSpan.TotalDays >= 1
            ? $"{timeSpan.TotalDays:F1} days"
            : timeSpan.TotalHours >= 1
                ? $"{timeSpan.TotalHours:F1} hours"
                : $"{timeSpan.TotalMinutes:F1} minutes";
    }
}