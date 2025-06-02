using System;
using System.Collections.Generic;
using System.Globalization;
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Services;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Models;

namespace DotnetAuthServer.Benchmarks;

/// <summary>
/// Extension methods for <see cref="TokenIntrospectionBenchmarks"/> that provide additional functionality
/// for working with token introspection benchmarks and test data.
/// </summary>
public static class TokenIntrospectionBenchmarksExtensions
{
    /// <summary>
    /// Creates a new instance of <see cref="TokenIntrospectionBenchmarks"/> with default configuration.
    /// </summary>
    /// <returns>A configured instance of <see cref="TokenIntrospectionBenchmarks"/></returns>
    /// <exception cref="ArgumentNullException">Thrown if any required service cannot be created.</exception>
    public static TokenIntrospectionBenchmarks CreateDefaultBenchmarks(this TokenIntrospectionBenchmarks _)
    {
        ArgumentNullException.ThrowIfNull(_);

        var options = new AuthServerOptions
        {
            JwtSigningKey = "super-secret-key-that-is-at-least-32-characters-long!",
            IssuerUrl = "https://auth.example.com",
            FailedLoginAttemptThreshold = 5,
            AccountLockoutDurationMinutes = 15,
            ClockSkewToleranceSeconds = 30
        };

        var revokedTokenStore = new RevokedTokenStore();
        var logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<TokenIntrospectionHandler>.Instance;
        var tokenIntrospectionHandler = new TokenIntrospectionHandler(options, revokedTokenStore, logger);

        var tokenService = new TokenService(
            options,
            null,
            null,
            null,
            null,
            new LoginRateLimiter(options, logger)
        );

        var tokenResponse = tokenService.HandleTokenRequestAsync(new TokenRequest
        {
            GrantType = "client_credentials",
            ClientId = "test-client",
            ClientSecret = "secret",
            Scope = "openid"
        }).Result;

        var expiredOptions = new AuthServerOptions
        {
            JwtSigningKey = "super-secret-key-that-is-at-least-32-characters-long!",
            IssuerUrl = "https://auth.example.com",
            AccessTokenLifetimeSeconds = -3600 // Expired 1 hour ago
        };

        var expiredTokenService = new TokenService(
            expiredOptions,
            null,
            null,
            null,
            null,
            new LoginRateLimiter(expiredOptions, logger)
        );

        var expiredTokenResponse = expiredTokenService.HandleTokenRequestAsync(new TokenRequest
        {
            GrantType = "client_credentials",
            ClientId = "test-client",
            ClientSecret = "secret",
            Scope = "openid"
        }).Result;

        return new TokenIntrospectionBenchmarks
        {
            _options = options,
            _tokenIntrospectionHandler = tokenIntrospectionHandler,
            _validToken = tokenResponse.AccessToken,
            _invalidToken = "invalid-token-string",
            _expiredToken = expiredTokenResponse.AccessToken
        };
    }

    /// <summary>
    /// Gets the token introspection results for all token types (valid, invalid, expired).
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>A read-only list of token introspection results.</returns>
    /// <exception cref="ArgumentNullException">Thrown if benchmarks is null.</exception>
    public static IReadOnlyList<TokenIntrospectionResult> GetAllTokenResults(this TokenIntrospectionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        return new List<TokenIntrospectionResult>
        {
            benchmarks.IntrospectValidTokenResult(),
            benchmarks.IntrospectInvalidTokenResult(),
            benchmarks.IntrospectExpiredTokenResult()
        }.AsReadOnly();
    }

    /// <summary>
    /// Gets the token introspection result for a valid token.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>The token introspection result.</returns>
    /// <exception cref="ArgumentNullException">Thrown if benchmarks is null.</exception>
    public static TokenIntrospectionResult IntrospectValidTokenResult(this TokenIntrospectionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        return benchmarks._tokenIntrospectionHandler.IntrospectToken(benchmarks._validToken);
    }

    /// <summary>
    /// Gets the token introspection result for an invalid token.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>The token introspection result.</returns>
    /// <exception cref="ArgumentNullException">Thrown if benchmarks is null.</exception>
    public static TokenIntrospectionResult IntrospectInvalidTokenResult(this TokenIntrospectionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        return benchmarks._tokenIntrospectionHandler.IntrospectToken(benchmarks._invalidToken);
    }

    /// <summary>
    /// Gets the token introspection result for an expired token.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>The token introspection result.</returns>
    /// <exception cref="ArgumentNullException">Thrown if benchmarks is null.</exception>
    public static TokenIntrospectionResult IntrospectExpiredTokenResult(this TokenIntrospectionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        return benchmarks._tokenIntrospectionHandler.IntrospectToken(benchmarks._expiredToken);
    }

    /// <summary>
    /// Gets the active status for all token types as a dictionary.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>A dictionary mapping token type names to their active status.</returns>
    /// <exception cref="ArgumentNullException">Thrown if benchmarks is null.</exception>
    public static IReadOnlyDictionary<string, bool> GetTokenActiveStatus(this TokenIntrospectionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        return new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["ValidToken"] = benchmarks.IntrospectValidToken(),
            ["InvalidToken"] = benchmarks.IntrospectInvalidToken(),
            ["ExpiredToken"] = benchmarks.IntrospectExpiredToken()
        };
    }

    /// <summary>
    /// Gets a summary string representing the token introspection results.
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance.</param>
    /// <returns>A formatted summary string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if benchmarks is null.</exception>
    public static string GetResultsSummary(this TokenIntrospectionBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        var validResult = benchmarks.IntrospectValidTokenResult();
        var invalidResult = benchmarks.IntrospectInvalidTokenResult();
        var expiredResult = benchmarks.IntrospectExpiredTokenResult();

        return string.Create(CultureInfo.InvariantCulture, $@"Token Introspection Results:
Valid Token:   {(validResult.Active ? "ACTIVE" : "INACTIVE")}
Invalid Token: {(invalidResult.Active ? "ACTIVE" : "INACTIVE")}
Expired Token: {(expiredResult.Active ? "ACTIVE" : "INACTIVE")}
");
    }
}