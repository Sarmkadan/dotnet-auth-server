#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Services;

using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public sealed class DynamicClientRegistrationServiceTests
{
    private readonly Mock<IClientRepository> _mockClientRepository;
    private readonly AuthServerOptions _options;
    private readonly Mock<ILogger<DynamicClientRegistrationService>> _mockLogger;
    private readonly DynamicClientRegistrationService _service;

    public DynamicClientRegistrationServiceTests()
    {
        _mockClientRepository = new Mock<IClientRepository>();
        _options = new AuthServerOptions
        {
            IssuerUrl = "https://auth.example.com",
            JwtSigningKey = "supersecretkeywithatleast32chars",
            SupportedScopes = [Constants.Scopes.OpenId, Constants.Scopes.Profile, "api.read"],
            RequirePkceForAllClients = true,
            RequireUserConsent = true
        };
        _mockLogger = new Mock<ILogger<DynamicClientRegistrationService>>();
        _service = new DynamicClientRegistrationService(
            _mockClientRepository.Object,
            _options,
            _mockLogger.Object);
    }

    [Fact]
    public async Task RegisterAsync_ValidPublicClient_ReturnsRegistrationResponse()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = "Test Public Client",
            GrantTypes = [Constants.GrantTypes.AuthorizationCode],
            RedirectUris = ["https://client.example.com/callback"],
            TokenEndpointAuthMethod = "none",
            ResponseTypes = ["code"]
        };

        // Act
        var response = await _service.RegisterAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.ClientId.Should().NotBeNullOrWhiteSpace();
        response.ClientSecret.Should().BeNull();
        response.ClientName.Should().Be("Test Public Client");
        response.GrantTypes.Should().Contain(Constants.GrantTypes.AuthorizationCode);
        response.RedirectUris.Should().Contain("https://client.example.com/callback");
        response.TokenEndpointAuthMethod.Should().Be("none");
        response.ClientIdIssuedAt.Should().BeGreaterThan(0);
        response.ClientSecretExpiresAt.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_ValidConfidentialClient_ReturnsRegistrationResponseWithSecret()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = "Test Confidential Client",
            GrantTypes = [Constants.GrantTypes.AuthorizationCode],
            RedirectUris = ["https://client.example.com/callback"],
            TokenEndpointAuthMethod = "client_secret_basic"
        };

        // Act
        var response = await _service.RegisterAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.ClientId.Should().NotBeNullOrWhiteSpace();
        response.ClientSecret.Should().NotBeNullOrWhiteSpace();
        response.ClientSecretExpiresAt.Should().Be(0);
        response.ClientName.Should().Be("Test Confidential Client");
        response.GrantTypes.Should().Contain(Constants.GrantTypes.AuthorizationCode);
        response.TokenEndpointAuthMethod.Should().Be("client_secret_basic");
        response.ClientIdIssuedAt.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task RegisterAsync_ClientWithScopes_ReturnsFilteredScopes()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = "Test Client With Scopes",
            GrantTypes = [Constants.GrantTypes.AuthorizationCode],
            RedirectUris = ["https://client.example.com/callback"],
            Scope = "openid profile email api.read api.write"
        };

        // Act
        var response = await _service.RegisterAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Scope.Should().NotBeNull();
        response.Scope.Should().Be("openid profile api.read"); // api.write not in supported scopes
        response.GrantTypes.Should().Contain(Constants.GrantTypes.AuthorizationCode);
    }

    [Fact]
    public async Task RegisterAsync_ClientWithContactsAndUris_ReturnsCompleteResponse()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = "Test Client With Contacts",
            GrantTypes = [Constants.GrantTypes.AuthorizationCode],
            RedirectUris = ["https://client.example.com/callback"],
            Contacts = ["admin@client.com", "support@client.com"],
            LogoUri = "https://client.example.com/logo.png",
            PolicyUri = "https://client.example.com/policy",
            TosUri = "https://client.example.com/tos"
        };

        // Act
        var response = await _service.RegisterAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Contacts.Should().HaveCount(2);
        response.Contacts.Should().Contain("admin@client.com");
        response.LogoUri.Should().Be("https://client.example.com/logo.png");
        response.PolicyUri.Should().Be("https://client.example.com/policy");
        response.TosUri.Should().Be("https://client.example.com/tos");
    }

    [Fact]
    public async Task RegisterAsync_ClientWithMultipleRedirectUris_ReturnsAllUris()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = "Test Client With Multiple URIs",
            GrantTypes = [Constants.GrantTypes.AuthorizationCode],
            RedirectUris = [
                "https://client.example.com/callback1",
                "https://client.example.com/callback2",
                "https://client.example.com/callback3"
            ]
        };

        // Act
        var response = await _service.RegisterAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.RedirectUris.Should().HaveCount(3);
        response.RedirectUris.Should().Contain("https://client.example.com/callback1");
        response.RedirectUris.Should().Contain("https://client.example.com/callback2");
        response.RedirectUris.Should().Contain("https://client.example.com/callback3");
    }

    [Fact]
    public async Task RegisterAsync_ClientWithMultipleGrantTypes_ReturnsAllGrantTypes()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = "Test Client With Multiple Grant Types",
            GrantTypes = [
                Constants.GrantTypes.AuthorizationCode,
                Constants.GrantTypes.RefreshToken,
                Constants.GrantTypes.ClientCredentials
            ],
            RedirectUris = ["https://client.example.com/callback"]
        };

        // Act
        var response = await _service.RegisterAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.GrantTypes.Should().HaveCount(3);
        response.GrantTypes.Should().Contain(Constants.GrantTypes.AuthorizationCode);
        response.GrantTypes.Should().Contain(Constants.GrantTypes.RefreshToken);
        response.GrantTypes.Should().Contain(Constants.GrantTypes.ClientCredentials);
    }

    [Fact]
    public async Task RegisterAsync_InvalidRedirectUri_ThrowsAuthServerException()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = "Test Client",
            GrantTypes = [Constants.GrantTypes.AuthorizationCode],
            RedirectUris = ["not-a-valid-uri"],
            TokenEndpointAuthMethod = "client_secret_basic"
        };

        // Act
        Func<Task> act = async () => await _service.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthServerException>()
            .Where(e => e.ErrorCode == Constants.ErrorCodes.InvalidRequest)
            .WithMessage("*redirect_uri*not a valid absolute URI*");
    }

    [Fact]
    public async Task RegisterAsync_EmptyRedirectUri_ThrowsAuthServerException()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = "Test Client",
            GrantTypes = [Constants.GrantTypes.AuthorizationCode],
            RedirectUris = [],
            TokenEndpointAuthMethod = "client_secret_basic"
        };

        // Act
        Func<Task> act = async () => await _service.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthServerException>()
            .Where(e => e.ErrorCode == Constants.ErrorCodes.InvalidRequest);
    }

    [Fact]
    public async Task RegisterAsync_InvalidGrantType_ThrowsAuthServerException()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = "Test Client",
            GrantTypes = ["invalid_grant_type"],
            RedirectUris = ["https://client.example.com/callback"],
            TokenEndpointAuthMethod = "client_secret_basic"
        };

        // Act
        Func<Task> act = async () => await _service.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthServerException>()
            .Where(e => e.ErrorCode == Constants.ErrorCodes.InvalidRequest)
            .WithMessage("*not supported*");
    }

    [Fact]
    public async Task RegisterAsync_InvalidAuthMethod_ThrowsAuthServerException()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = "Test Client",
            GrantTypes = [Constants.GrantTypes.AuthorizationCode],
            RedirectUris = ["https://client.example.com/callback"],
            TokenEndpointAuthMethod = "invalid_method"
        };

        // Act
        Func<Task> act = async () => await _service.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthServerException>()
            .Where(e => e.ErrorCode == Constants.ErrorCodes.InvalidRequest)
            .WithMessage("*token_endpoint_auth_method*not supported*");
    }

    [Fact]
    public async Task RegisterAsync_MissingClientName_ThrowsAuthServerException()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = null,
            GrantTypes = [Constants.GrantTypes.AuthorizationCode],
            RedirectUris = ["https://client.example.com/callback"],
            TokenEndpointAuthMethod = "client_secret_basic"
        };

        // Act
        Func<Task> act = async () => await _service.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthServerException>()
            .Where(e => e.ErrorCode == Constants.ErrorCodes.InvalidRequest)
            .WithMessage("*client_name is required*");
    }

    [Fact]
    public async Task RegisterAsync_ImplicitGrantWithoutRedirectUri_ThrowsAuthServerException()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = "Test Client",
            GrantTypes = [Constants.GrantTypes.Implicit],
            RedirectUris = [],
            TokenEndpointAuthMethod = "none"
        };

        // Act
        Func<Task> act = async () => await _service.RegisterAsync(request);

        // Assert
        await act.Should().ThrowAsync<AuthServerException>()
            .Where(e => e.ErrorCode == Constants.ErrorCodes.InvalidRequest);
    }

    [Fact]
    public async Task RegisterAsync_ClientSavedToRepository()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = "Test Client To Save",
            GrantTypes = [Constants.GrantTypes.AuthorizationCode],
            RedirectUris = ["https://client.example.com/callback"],
            TokenEndpointAuthMethod = "client_secret_basic"
        };

        var createdClient = default(Client);
        _mockClientRepository.Setup(x => x.CreateAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()))
            .Callback<Client, CancellationToken>((client, _) => createdClient = client)
            .ReturnsAsync((Client client, CancellationToken _) => client);

        // Act
        var response = await _service.RegisterAsync(request);

        // Assert
        _mockClientRepository.Verify(x => x.CreateAsync(It.IsAny<Client>(), It.IsAny<CancellationToken>()), Times.Once);
        createdClient.Should().NotBeNull();
        createdClient.ClientName.Should().Be("Test Client To Save");
        createdClient.ClientId.Should().NotBeNullOrWhiteSpace();
        createdClient.IsConfidential.Should().BeTrue();
        createdClient.RedirectUris.Should().Contain("https://client.example.com/callback");
        createdClient.AllowedGrantTypes.Should().Contain(Constants.GrantTypes.AuthorizationCode);
        createdClient.RequirePkce.Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_PublicClientPkceAlwaysRequired()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = "Test Public Client",
            GrantTypes = [Constants.GrantTypes.AuthorizationCode],
            RedirectUris = ["https://client.example.com/callback"],
            TokenEndpointAuthMethod = "none"
        };

        // Act
        var response = await _service.RegisterAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.ClientId.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RegisterAsync_ConfidentialClientPkceBasedOnServerConfig()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = "Test Confidential Client",
            GrantTypes = [Constants.GrantTypes.AuthorizationCode],
            RedirectUris = ["https://client.example.com/callback"],
            TokenEndpointAuthMethod = "client_secret_basic"
        };

        // Act
        var response = await _service.RegisterAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.ClientId.Should().NotBeNullOrWhiteSpace();
        // RequirePkceForAllClients is true by default, so confidential clients should also require PKCE
    }

    [Fact]
    public async Task RegisterAsync_ClientWithNoScopes_UsesDefaultScopes()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = "Test Client",
            GrantTypes = [Constants.GrantTypes.ClientCredentials],
            RedirectUris = [],
            TokenEndpointAuthMethod = "client_secret_basic"
        };

        // Act
        var response = await _service.RegisterAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.Scope.Should().NotBeNull();
        response.Scope.Should().Contain("openid"); // Should use default scopes
    }

    [Fact]
    public async Task RegisterAsync_ClientIdIsValidGuidFormat()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = "Test Client",
            GrantTypes = [Constants.GrantTypes.AuthorizationCode],
            RedirectUris = ["https://client.example.com/callback"],
            TokenEndpointAuthMethod = "client_secret_basic"
        };

        // Act
        var response = await _service.RegisterAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.ClientId.Should().MatchRegex("^[a-f0-9]{32}$");
    }

    [Fact]
    public async Task RegisterAsync_ClientSecretIsUrlSafeBase64()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = "Test Client",
            GrantTypes = [Constants.GrantTypes.AuthorizationCode],
            RedirectUris = ["https://client.example.com/callback"],
            TokenEndpointAuthMethod = "client_secret_basic"
        };

        // Act
        var response = await _service.RegisterAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.ClientSecret.Should().NotBeNullOrWhiteSpace();
        response.ClientSecret.Should().MatchRegex("^[a-zA-Z0-9_-]+$");
    }

    [Fact]
    public async Task RegisterAsync_ResponseTypesPreservedInResponse()
    {
        // Arrange
        var request = new ClientRegistrationRequest
        {
            ClientName = "Test Client",
            GrantTypes = [Constants.GrantTypes.AuthorizationCode],
            RedirectUris = ["https://client.example.com/callback"],
            ResponseTypes = ["code", "id_token"]
        };

        // Act
        var response = await _service.RegisterAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.ResponseTypes.Should().HaveCount(2);
        response.ResponseTypes.Should().Contain("code");
        response.ResponseTypes.Should().Contain("id_token");
    }
}
