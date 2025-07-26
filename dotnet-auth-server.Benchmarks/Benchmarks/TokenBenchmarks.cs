using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using DotnetAuthServer.Services;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Security;
using DotnetAuthServer.Caching;
using DotnetAuthServer.Data.Repositories;
using Moq;

namespace DotnetAuthServer.Benchmarks;

/// <summary>
/// Benchmark tests for token-related operations in the DotnetAuthServer.
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TokenBenchmarks
{
    private AuthServerOptions _options;
    private TokenIntrospectionHandler _tokenIntrospectionHandler;
    private PkceValidationService _pkceValidationService;
    private ClientValidationService _clientValidationService;
    private TokenService _tokenService;
    
    private string _codeVerifier;
    private string _codeChallenge;
    private Client _client;
    private TokenRequest _tokenRequest;
    
    private Mock<IUserRepository> _userRepositoryMock;
    private Mock<IClientRepository> _clientRepositoryMock;
    private Mock<IAuthorizationGrantRepository> _grantRepositoryMock;
    private Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private Mock<ICacheService> _cacheServiceMock;

    /// <summary>
    /// Initializes benchmark environment, configures options, mocks, and prepares token request.
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

        _pkceValidationService = new PkceValidationService(_options, NullLogger<PkceValidationService>.Instance);
        _codeVerifier = _pkceValidationService.GenerateCodeVerifier();
        _codeChallenge = _pkceValidationService.GenerateCodeChallenge(_codeVerifier);

        _userRepositoryMock = new Mock<IUserRepository>();
        _clientRepositoryMock = new Mock<IClientRepository>();
        _grantRepositoryMock = new Mock<IAuthorizationGrantRepository>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _cacheServiceMock = new Mock<ICacheService>();

        _clientValidationService = new ClientValidationService(_clientRepositoryMock.Object, _cacheServiceMock.Object, NullLogger<ClientValidationService>.Instance);

        _tokenService = new TokenService(
            _options,
            _userRepositoryMock.Object,
            _clientRepositoryMock.Object,
            _grantRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            new LoginRateLimiter(_options, NullLogger<LoginRateLimiter>.Instance)
        );

        _client = new Client
        {
            ClientId = "test-client",
            IsActive = true,
            IsConfidential = true,
            ClientSecretHash = "secret",
            AllowedGrantTypes = new List<string> { "client_credentials" },
            AllowedScopes = new List<string> { "openid" }
        };
        _clientRepositoryMock.Setup(x => x.GetActiveClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(_client);
        _clientRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(_client);

        _tokenRequest = new TokenRequest
        {
            GrantType = "client_credentials",
            ClientId = "test-client",
            ClientSecret = "secret",
            Scope = "openid"
        };
    }

    /// <summary>
    /// Benchmarks token introspection by calling IntrospectToken with an invalid token.
    /// </summary>
    /// <returns>The introspection response for the invalid token.</returns>
    [Benchmark]
    public IntrospectionResponse IntrospectToken()
    {
        return _tokenIntrospectionHandler.IntrospectToken("invalid-token");
    }

    /// <summary>
    /// Benchmarks PKCE validation by verifying the code verifier against the code challenge.
    /// </summary>
    /// <returns>True if the verifier matches the challenge; otherwise false.</returns>
    [Benchmark]
    public bool ValidatePkce()
    {
        return _pkceValidationService.ValidateCodeVerifier(_codeVerifier, _codeChallenge);
    }

    /// <summary>
    /// Benchmarks client credential validation by invoking ValidateClientCredentialsAsync.
    /// </summary>
    /// <returns>A task that resolves to the validated client.</returns>
    [Benchmark]
    public async Task<Client> ValidateClientCredentials()
    {
        return await _clientValidationService.ValidateClientCredentialsAsync("test-client", "secret");
    }

    /// <summary>
    /// Benchmarks handling of client credentials grant by calling HandleTokenRequestAsync.
    /// </summary>
    /// <returns>A task that resolves to the token response.</returns>
    [Benchmark]
    public async Task<TokenResponse> HandleClientCredentialsGrant()
    {
        return await _tokenService.HandleTokenRequestAsync(_tokenRequest);
    }
}
