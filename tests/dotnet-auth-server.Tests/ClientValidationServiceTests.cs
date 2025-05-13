using Moq;
using FluentAssertions;
using DotnetAuthServer.Services;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Caching;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Exceptions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace DotnetAuthServer.Tests;

public sealed class ClientValidationServiceTests
{
    private readonly Mock<IClientRepository> _clientRepositoryMock;
    private readonly Mock<ICacheService> _cacheServiceMock;
    private readonly Mock<ILogger<ClientValidationService>> _loggerMock;
    private readonly ClientValidationService _service;

    public ClientValidationServiceTests()
    {
        _clientRepositoryMock = new Mock<IClientRepository>();
        _cacheServiceMock = new Mock<ICacheService>();
        _loggerMock = new Mock<ILogger<ClientValidationService>>();
        _service = new ClientValidationService(_clientRepositoryMock.Object, _cacheServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ValidateClientCredentialsAsync_ValidConfidentialClient_ReturnsClient()
    {
        // Arrange
        var clientId = "test-client";
        var clientSecret = "secret";
        var client = new Client
        {
            ClientId = clientId,
            IsActive = true,
            IsConfidential = true,
            ClientSecretHash = clientSecret
        };

        _cacheServiceMock.Setup(c => c.GetAsync<Client>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((Client?)null);
        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId, It.IsAny<CancellationToken>()))
                             .ReturnsAsync(client);

        // Act
        var result = await _service.ValidateClientCredentialsAsync(clientId, clientSecret);

        // Assert
        result.Should().Be(client);
    }

    [Fact]
    public async Task ValidateClientCredentialsAsync_InvalidClientId_ThrowsInvalidClientException()
    {
        // Arrange
        var clientId = "unknown";
        _cacheServiceMock.Setup(c => c.GetAsync<Client>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((Client?)null);
        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId, It.IsAny<CancellationToken>()))
                             .ReturnsAsync((Client?)null);

        // Act
        Func<Task> act = () => _service.ValidateClientCredentialsAsync(clientId, "secret");

        // Assert
        await act.Should().ThrowAsync<InvalidClientException>();
    }

    [Fact]
    public async Task ValidateClientCredentialsAsync_InvalidSecret_ThrowsInvalidClientException()
    {
        // Arrange
        var clientId = "test-client";
        var client = new Client
        {
            ClientId = clientId,
            IsActive = true,
            IsConfidential = true,
            ClientSecretHash = "correct-secret"
        };

        _cacheServiceMock.Setup(c => c.GetAsync<Client>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((Client?)null);
        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId, It.IsAny<CancellationToken>()))
                             .ReturnsAsync(client);

        // Act
        Func<Task> act = () => _service.ValidateClientCredentialsAsync(clientId, "wrong-secret");

        // Assert
        await act.Should().ThrowAsync<InvalidClientException>();
    }

    [Fact]
    public async Task ValidateRedirectUriAsync_ValidUri_DoesNotThrow()
    {
        // Arrange
        var clientId = "test-client";
        var redirectUri = "https://app.com/callback";
        var client = new Client
        {
            ClientId = clientId,
            RedirectUris = new List<string> { redirectUri }
        };

        _cacheServiceMock.Setup(c => c.GetAsync<Client>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((Client?)null);
        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId, It.IsAny<CancellationToken>()))
                             .ReturnsAsync(client);

        // Act
        Func<Task> act = () => _service.ValidateRedirectUriAsync(clientId, redirectUri);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ValidateRedirectUriAsync_InvalidUri_ThrowsInvalidClientException()
    {
        // Arrange
        var clientId = "test-client";
        var redirectUri = "https://other.com/callback";
        var client = new Client
        {
            ClientId = clientId,
            RedirectUris = new List<string> { "https://app.com/callback" }
        };

        _cacheServiceMock.Setup(c => c.GetAsync<Client>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((Client?)null);
        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId, It.IsAny<CancellationToken>()))
                             .ReturnsAsync(client);

        // Act
        Func<Task> act = () => _service.ValidateRedirectUriAsync(clientId, redirectUri);

        // Assert
        await act.Should().ThrowAsync<InvalidClientException>();
    }

    [Fact]
    public async Task ValidateScopesAsync_InvalidScopes_ThrowsInvalidScopeException()
    {
        // Arrange
        var clientId = "test-client";
        var client = new Client
        {
            ClientId = clientId,
            AllowedScopes = new List<string> { "openid", "profile" }
        };

        _cacheServiceMock.Setup(c => c.GetAsync<Client>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                         .ReturnsAsync((Client?)null);
        _clientRepositoryMock.Setup(r => r.GetByIdAsync(clientId, It.IsAny<CancellationToken>()))
                             .ReturnsAsync(client);

        // Act
        Func<Task> act = () => _service.ValidateScopesAsync(clientId, new List<string> { "email", "invalid-scope" });

        // Assert
        await act.Should().ThrowAsync<InvalidScopeException>();
    }
}
