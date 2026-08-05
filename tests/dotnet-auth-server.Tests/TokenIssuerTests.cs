using Moq;
using FluentAssertions;
using DotnetAuthServer.Services;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Security;
using Microsoft.Extensions.Logging;
using Xunit;

/// <summary>
/// Tests for the TokenIssuer class.
/// </summary>
public sealed class TokenIssuerTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IClientRepository> _clientRepositoryMock;
    private readonly Mock<IAuthorizationGrantRepository> _grantRepositoryMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly LoginRateLimiter _loginRateLimiter;
    private readonly AuthServerOptions _options;
    private readonly TokenIssuer _tokenIssuer;

    public TokenIssuerTests()
    {
        _options = new AuthServerOptions
        {
            IssuerUrl = "https://auth.example.com",
            JwtSigningKey = "0123456789abcdef0123456789abcdef",
            DatabaseConnectionString = "unused",
            AccessTokenLifetimeSeconds = 3600,
            RefreshTokenLifetimeSeconds = 2592000
        };

        _userRepositoryMock = new Mock<IUserRepository>();
        _clientRepositoryMock = new Mock<IClientRepository>();
        _grantRepositoryMock = new Mock<IAuthorizationGrantRepository>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
        _loginRateLimiter = new LoginRateLimiter(_options, Mock.Of<ILogger<LoginRateLimiter>>());

        _tokenIssuer = new TokenIssuer(
            _options,
            _userRepositoryMock.Object,
            _clientRepositoryMock.Object,
            _grantRepositoryMock.Object,
            _refreshTokenRepositoryMock.Object,
            _loginRateLimiter,
            Mock.Of<ILogger<TokenIssuer>>());
    }

    [Fact]
    public async Task HandleTokenRequestAsync_NullRequest_ThrowsArgumentNullException()
    {
        Func<Task> act = () => _tokenIssuer.HandleTokenRequestAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task HandleTokenRequestAsync_MissingGrantType_ThrowsAuthServerException()
    {
        var request = new TokenRequest { ClientId = "client-1", GrantType = null };

        Func<Task> act = () => _tokenIssuer.HandleTokenRequestAsync(request);

        var ex = await act.Should().ThrowAsync<AuthServerException>();
        ex.Which.ErrorCode.Should().Be(Constants.ErrorCodes.InvalidRequest);
    }

    [Fact]
    public async Task HandleTokenRequestAsync_UnsupportedGrantType_ThrowsAuthServerException()
    {
        var request = new TokenRequest { ClientId = "client-1", GrantType = "unsupported_grant" };

        Func<Task> act = () => _tokenIssuer.HandleTokenRequestAsync(request);

        var ex = await act.Should().ThrowAsync<AuthServerException>();
        ex.Which.ErrorCode.Should().Be(Constants.ErrorCodes.UnsupportedGrantType);
    }

    [Fact]
    public async Task HandleTokenRequestAsync_ClientCredentialsHappyPath_ReturnsTokenResponse()
    {
        var client = new Client
        {
            ClientId = "client-1",
            ClientName = "Client One",
            IsConfidential = false,
            IsActive = true
        };

        _clientRepositoryMock
            .Setup(r => r.GetActiveClientAsync("client-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        var request = new TokenRequest
        {
            ClientId = "client-1",
            GrantType = Constants.GrantTypes.ClientCredentials,
            Scope = "read write"
        };

        var response = await _tokenIssuer.HandleTokenRequestAsync(request);

        response.Should().NotBeNull();
        response.AccessToken.Should().NotBeNullOrWhiteSpace();
        response.TokenType.Should().Be(Constants.TokenTypes.Bearer);
        response.Scope.Should().Be("read write");
    }

    [Fact]
    public async Task HandleTokenRequestAsync_ClientCredentialsConfidentialWithoutSecret_ThrowsInvalidClientException()
    {
        var client = new Client
        {
            ClientId = "client-1",
            ClientName = "Client One",
            IsConfidential = true,
            IsActive = true
        };

        _clientRepositoryMock
            .Setup(r => r.GetActiveClientAsync("client-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(client);

        var request = new TokenRequest
        {
            ClientId = "client-1",
            GrantType = Constants.GrantTypes.ClientCredentials,
            ClientSecret = null
        };

        Func<Task> act = () => _tokenIssuer.HandleTokenRequestAsync(request);

        await act.Should().ThrowAsync<InvalidClientException>();
    }

    [Fact]
    public void ValidateClientSecret_PublicClient_ReturnsTrueRegardlessOfSecret()
    {
        var client = new Client { ClientId = "c1", ClientName = "c1", IsConfidential = false };

        var result = _tokenIssuer.ValidateClientSecret(client, null);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateClientSecret_ConfidentialClientWithMatchingHash_ReturnsTrue()
    {
        const string secret = "s3cr3t-value";
        var hash = _tokenIssuer.HashClientSecret(secret);
        var client = new Client
        {
            ClientId = "c1",
            ClientName = "c1",
            IsConfidential = true,
            ClientSecretHash = hash
        };

        var result = _tokenIssuer.ValidateClientSecret(client, secret);

        result.Should().BeTrue();
    }

    [Fact]
    public void ValidateClientSecret_ConfidentialClientWithWrongSecret_ReturnsFalse()
    {
        var client = new Client
        {
            ClientId = "c1",
            ClientName = "c1",
            IsConfidential = true,
            ClientSecretHash = _tokenIssuer.HashClientSecret("correct-secret")
        };

        var result = _tokenIssuer.ValidateClientSecret(client, "wrong-secret");

        result.Should().BeFalse();
    }

    [Fact]
    public void HashClientSecret_SameInput_IsDeterministicAndDiffersForDifferentInput()
    {
        var hash1 = _tokenIssuer.HashClientSecret("secret-a");
        var hash2 = _tokenIssuer.HashClientSecret("secret-a");
        var hash3 = _tokenIssuer.HashClientSecret("secret-b");

        hash1.Should().Be(hash2);
        hash1.Should().NotBe(hash3);
        hash1.Should().NotBeNullOrWhiteSpace();
    }
}
