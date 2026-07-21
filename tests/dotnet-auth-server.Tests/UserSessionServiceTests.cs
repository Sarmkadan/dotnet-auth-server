#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace DotnetAuthServer.Tests;

public sealed class UserSessionServiceTests
{
    private readonly Mock<IUserSessionRepository> _sessionRepositoryMock;
    private readonly Mock<ILogger<UserSessionService>> _loggerMock;
    private readonly AuthServerOptions _options;
    private readonly UserSessionService _service;

    public UserSessionServiceTests()
    {
        _sessionRepositoryMock = new Mock<IUserSessionRepository>();
        _loggerMock = new Mock<ILogger<UserSessionService>>();
        _options = new AuthServerOptions
        {
            IssuerUrl = "https://auth.example.com",
            JwtSigningKey = new string('x', 32),
            RefreshTokenLifetimeSeconds = 3600,
            DatabaseConnectionString = ""
        };

        _service = new UserSessionService(_sessionRepositoryMock.Object, _loggerMock.Object, _options);
    }

    [Fact]
    public async Task CreateSessionAsync_ValidParams_CreatesSession()
    {
        // Arrange
        var userId = "user1";
        var clientId = "client1";
        var grantedScopes = "openid profile";
        var ipAddress = "127.0.0.1";
        var userAgent = "TestAgent/1.0";

        // Act
        var session = await _service.CreateSessionAsync(userId, clientId, grantedScopes, ipAddress, userAgent);

        // Assert
        session.Should().NotBeNull();
        session.UserId.Should().Be(userId);
        session.ClientId.Should().Be(clientId);
        session.GrantedScopes.Should().Be(grantedScopes);
        session.IpAddress.Should().Be(ipAddress);
        session.UserAgent.Should().Be(userAgent);
    }

    [Fact]
    public async Task GetActiveSessionsAsync_ValidUserId_ReturnsActiveSessions()
    {
        // Arrange
        var userId = "user1";
        var session1 = new UserSession { UserId = userId, ClientId = "client1", GrantedScopes = "openid" };
        var session2 = new UserSession { UserId = userId, ClientId = "client2", GrantedScopes = "profile" };

        _sessionRepositoryMock.Setup(repo => repo.GetActiveByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { session1, session2 });

        // Act
        var activeSessions = await _service.GetActiveSessionsAsync(userId);

        // Assert
        activeSessions.Should().HaveCount(2);
        activeSessions.Should().Contain(session1);
        activeSessions.Should().Contain(session2);
    }

    [Fact]
    public async Task GetActiveSessionsAsync_ExpiredSession_ReturnsNoSessions()
    {
        // Arrange
        var userId = "user1";
        var session = new UserSession { UserId = userId, ClientId = "client1", GrantedScopes = "openid", ExpiresAt = DateTime.UtcNow.AddSeconds(-1) };

        _sessionRepositoryMock.Setup(repo => repo.GetActiveByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { session });

        // Act
        var activeSessions = await _service.GetActiveSessionsAsync(userId);

        // Assert
        activeSessions.Should().HaveCount(1);
        activeSessions.Should().Contain(session);
    }

    [Fact]
    public async Task RevokeSessionAsync_ValidSessionId_RevokeSession()
    {
        // Arrange
        var sessionId = "session1";
        var session = new UserSession { SessionId = sessionId, UserId = "user1", ClientId = "client1" };

        _sessionRepositoryMock.Setup(repo => repo.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        await _service.RevokeSessionAsync(sessionId);

        // Assert
        session.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeSessionAsync_InvalidSessionId_ThrowsAuthServerException()
    {
        // Arrange
        var sessionId = "session1";

        _sessionRepositoryMock.Setup(repo => repo.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSession)null);

        // Act & Assert
        await Assert.ThrowsAsync<AuthServerException>(() => _service.RevokeSessionAsync(sessionId));
    }

    [Fact]
    public async Task RevokeAllUserSessionsAsync_ValidUserId_RevokeSessions()
    {
        // Arrange
        var userId = "user1";
        var session1 = new UserSession { UserId = userId, ClientId = "client1", GrantedScopes = "openid" };
        var session2 = new UserSession { UserId = userId, ClientId = "client2", GrantedScopes = "profile" };

        _sessionRepositoryMock.Setup(repo => repo.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { session1, session2 });

        // Act
        var count = await _service.RevokeAllUserSessionsAsync(userId);

        // Assert
        count.Should().Be(2);
        session1.IsRevoked.Should().BeTrue();
        session2.IsRevoked.Should().BeTrue();
    }

    [Fact]
    public async Task RevokeAllOtherUserSessionsAsync_ValidUserIdAndSessionId_RevokeSessions()
    {
        // Arrange
        var userId = "user1";
        var keepSessionId = "session1";
        var session1 = new UserSession { UserId = userId, ClientId = "client1", GrantedScopes = "openid", SessionId = keepSessionId };
        var session2 = new UserSession { UserId = userId, ClientId = "client2", GrantedScopes = "profile" };

        _sessionRepositoryMock.Setup(repo => repo.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { session1, session2 });

        // Act
        var count = await _service.RevokeAllOtherUserSessionsAsync(userId, keepSessionId);

        // Assert
        count.Should().Be(1);
        session1.IsRevoked.Should().BeFalse();
        session2.IsRevoked.Should().BeTrue();
    }
}
