using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Services;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Models;

namespace DotnetAuthServer.Benchmarks;

/// <summary>
/// Benchmark class for token introspection operations.
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TokenIntrospectionBenchmarks
{
    private AuthServerOptions _options;
    private TokenIntrospectionHandler _tokenIntrospectionHandler;
    private string _validToken;
    private string _invalidToken;
    private string _expiredToken;

    /// <summary>
    /// Initializes the benchmark setup.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _options = new AuthServerOptions
        {
            JwtSigningKey = "super-secret-key-that-is-at-least-32-characters-long!",
            IssuerUrl = "https://auth.example.com",
            FailedLoginAttemptThreshold = 5,
            AccountLockoutDurationMinutes = 15,
            ClockSkewToleranceSeconds = 30
        };

        var revokedTokenStore = new RevokedTokenStore();
        _tokenIntrospectionHandler = new TokenIntrospectionHandler(_options, revokedTokenStore, NullLogger<TokenIntrospectionHandler>.Instance);

        // Generate a valid token
        var tokenService = new TokenService(
            _options,
            null,
            null,
            null,
            null,
            new LoginRateLimiter(_options, NullLogger<LoginRateLimiter>.Instance)
        );

        var tokenResponse = tokenService.HandleTokenRequestAsync(new TokenRequest
        {
            GrantType = "client_credentials",
            ClientId = "test-client",
            ClientSecret = "secret",
            Scope = "openid"
        }).Result;

        _validToken = tokenResponse.AccessToken;
        _invalidToken = "invalid-token-string";

        // Generate an expired token
        var oldOptions = new AuthServerOptions
        {
            JwtSigningKey = "super-secret-key-that-is-at-least-32-characters-long!",
            IssuerUrl = "https://auth.example.com",
            AccessTokenLifetimeSeconds = -3600 // Expired 1 hour ago
        };

        var expiredTokenService = new TokenService(
            oldOptions,
            null,
            null,
            null,
            null,
            new LoginRateLimiter(oldOptions, NullLogger<LoginRateLimiter>.Instance)
        );

        var expiredTokenResponse = expiredTokenService.HandleTokenRequestAsync(new TokenRequest
        {
            GrantType = "client_credentials",
            ClientId = "test-client",
            ClientSecret = "secret",
            Scope = "openid"
        }).Result;

        _expiredToken = expiredTokenResponse.AccessToken;
    }

    /// <summary>
    /// Benchmarks the introspection of a valid token.
    /// </summary>
    /// <returns>True if the token is active, false otherwise.</returns>
    [Benchmark]
    public bool IntrospectValidToken()
    {
        return _tokenIntrospectionHandler.IntrospectToken(_validToken).Active;
    }

    /// <summary>
    /// Benchmarks the introspection of an invalid token.
    /// </summary>
    /// <returns>True if the token is active, false otherwise.</returns>
    [Benchmark]
    public bool IntrospectInvalidToken()
    {
        return _tokenIntrospectionHandler.IntrospectToken(_invalidToken).Active;
    }

    /// <summary>
    /// Benchmarks the introspection of an expired token.
    /// </summary>
    /// <returns>True if the token is active, false otherwise.</returns>
    [Benchmark]
    public bool IntrospectExpiredToken()
    {
        return _tokenIntrospectionHandler.IntrospectToken(_expiredToken).Active;
    }
}
