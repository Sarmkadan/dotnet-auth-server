using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Data.Repositories;
using Moq;

namespace DotnetAuthServer.Benchmarks;

/// <summary>
/// Benchmark class for token revocation operations.
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TokenRevocationBenchmarks
{
    private AuthServerOptions _options;
    private TokenRevocationHandler _tokenRevocationHandler;
    private string _accessToken;
    private string _refreshToken;
    private Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private Mock<IAuthorizationGrantRepository> _grantRepositoryMock;

    /// <summary>
    /// Sets up the benchmark environment.
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
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _grantRepositoryMock = new Mock<IAuthorizationGrantRepository>();

        _tokenRevocationHandler = new TokenRevocationHandler(
            _refreshTokenRepositoryMock.Object,
            _grantRepositoryMock.Object,
            revokedTokenStore,
            _options,
            NullLogger<TokenRevocationHandler>.Instance
        );

        // Generate tokens
        var tokenService = new TokenService(
            _options,
            null,
            null,
            null,
            _refreshTokenRepositoryMock.Object,
            new LoginRateLimiter(_options, NullLogger<LoginRateLimiter>.Instance)
        );

        var tokenResponse = tokenService.HandleTokenRequestAsync(new TokenRequest
        {
            GrantType = "client_credentials",
            ClientId = "test-client",
            ClientSecret = "secret",
            Scope = "openid"
        }).Result;

        _accessToken = tokenResponse.AccessToken;
        _refreshToken = tokenResponse.RefreshToken;

        // Setup mock to return a valid refresh token
        var refreshTokenEntity = new RefreshToken
        {
            Token = _refreshToken,
            UserId = "user123",
            ClientId = "test-client",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        _refreshTokenRepositoryMock.Setup(x => x.GetByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshTokenEntity);
        _refreshTokenRepositoryMock.Setup(x => x.DeleteAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    /// <summary>
    /// Revokes an access token.
    /// </summary>
    /// <returns>True if the token was successfully revoked, false otherwise.</returns>
    [Benchmark]
    public async Task<bool> RevokeAccessToken()
    {
        var result = await _tokenRevocationHandler.RevokeTokenAsync(_accessToken, "access_token");
        return result.Success;
    }

    /// <summary>
    /// Revokes a refresh token.
    /// </summary>
    /// <returns>True if the token was successfully revoked, false otherwise.</returns>
    [Benchmark]
    public async Task<bool> RevokeRefreshToken()
    {
        var result = await _tokenRevocationHandler.RevokeTokenAsync(_refreshToken, "refresh_token");
        return result.Success;
    }

    /// <summary>
    /// Attempts to revoke an invalid token.
    /// </summary>
    /// <returns>True if the token was successfully revoked, false otherwise.</returns>
    [Benchmark]
    public async Task<bool> RevokeInvalidToken()
    {
        var result = await _tokenRevocationHandler.RevokeTokenAsync("invalid-token-string", "access_token");
        return result.Success;
    }
}
