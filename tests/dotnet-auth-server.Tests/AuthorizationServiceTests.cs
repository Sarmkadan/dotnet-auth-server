using System;
using System.Threading;
using System.Threading.Tasks;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DotnetAuthServer.Tests;

public class AuthorizationServiceTests
{
    private readonly Mock<IClientRepository> _clientRepoMock = new();
    private readonly Mock<IAuthorizationGrantRepository> _grantRepoMock = new();
    private readonly Mock<ILogger<AuthorizationService>> _loggerMock = new();
    private readonly AuthServerOptions _options = new()
    {
        SupportedScopes = new[] { "openid", "profile", "email" },
        AuthorizationCodeLifetimeSeconds = 300,
        RequirePkceForAllClients = false
    };

    private AuthorizationService CreateService()
        => new AuthorizationService(_options, _clientRepoMock.Object, _grantRepoMock.Object, _loggerMock.Object);

    [Fact]
    public async Task ValidateAuthorizationRequestAsync_InvalidRequest_ThrowsAuthServerException()
    {
        var service = CreateService();
        var request = new AuthorizationRequest(); // missing required fields

        var ex = await Assert.ThrowsAsync<AuthServerException>(() =>
            service.ValidateAuthorizationRequestAsync(request));

        Assert.Equal(Constants.ErrorCodes.InvalidRequest, ex.ErrorCode);
    }

    [Fact]
    public async Task CleanupExpiredGrantsAsync_InvokesRepository()
    {
        var service = CreateService();

        await service.CleanupExpiredGrantsAsync();

        _grantRepoMock.Verify(r => r.DeleteExpiredAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void ValidatePkceCodeVerifier_PlainMatches_ReturnsTrue()
    {
        var service = CreateService();

        var result = service.ValidatePkceCodeVerifier("abc123", "abc123", "plain");

        Assert.True(result);
    }

    [Fact]
    public void ValidatePkceCodeVerifier_S256Valid_ReturnsTrue()
    {
        var service = CreateService();

        // Example from RFC 7636
        var verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";
        var challenge = "E9Melhoa2OwvFrEMTJgu9Q9cGz6j6x5ZK0Z6x0a0a0U";

        var result = service.ValidatePkceCodeVerifier(challenge, verifier, "S256");

        Assert.True(result);
    }

    [Fact]
    public async Task CreateAuthorizationGrantAsync_ReturnsGrantWithExpectedValues()
    {
        var service = CreateService();

        var request = new AuthorizationRequest
        {
            Scope = "openid profile",
            State = "state123",
            Nonce = "nonce123",
            CodeChallenge = "codechallenge",
            CodeChallengeMethod = "S256",
            ResponseType = "code"
        };

        var grant = await service.CreateAuthorizationGrantAsync(
            clientId: "client1",
            userId: "user1",
            grantedScopes: "openid profile",
            redirectUri: "https://app/callback",
            request: request);

        Assert.NotNull(grant);
        Assert.Equal("client1", grant.ClientId);
        Assert.Equal("user1", grant.UserId);
        Assert.Equal(request.Scope, grant.RequestedScopes);
        Assert.Equal(request.State, grant.State);
        Assert.Equal(request.Nonce, grant.Nonce);
        Assert.Equal(request.CodeChallenge, grant.CodeChallenge);
        Assert.Equal(request.CodeChallengeMethod, grant.CodeChallengeMethod);
        Assert.Equal(request.ResponseType, grant.ResponseType);
        Assert.InRange(
            grant.ExpiresAt,
            DateTime.UtcNow,
            DateTime.UtcNow.AddSeconds(_options.AuthorizationCodeLifetimeSeconds + 5));
    }
}
