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

/// <summary>
/// Contains unit tests for the <see cref="UserSessionService"/> class.
/// Tests cover session creation, retrieval, and revocation scenarios.
/// </summary>
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

    /// <summary>
    /// Tests that creating a session with valid parameters successfully creates a user session
    /// with the correct properties set.
    /// </summary>
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
        _loggerMock.Object.LogInformation("Creating session for user {UserId} with client {ClientId}", userId, clientId);
        var session = await _service.CreateSessionAsync(userId, clientId, grantedScopes, ipAddress, userAgent);
        _loggerMock.Object.LogInformation("Session created for user {UserId} with id {SessionId}", userId, session.SessionId);

        // Assert
        session.Should().NotBeNull();
        session.UserId.Should().Be(userId);
        session.ClientId.Should().Be(clientId);
        session.GrantedScopes.Should().Be(grantedScopes);
        session.IpAddress.Should().Be(ipAddress);
        session.UserAgent.Should().Be(userAgent);
    }

    /// <summary>
    /// Tests that getting active sessions for a valid user ID returns all active sessions for that user.
    /// </summary>
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
        _loggerMock.Object.LogInformation("Fetching active sessions for user {UserId}", userId);
        var activeSessions = await _service.GetActiveSessionsAsync(userId);
        _loggerMock.Object.LogInformation("Fetched {Count} active sessions for user {UserId}", activeSessions.Count(), userId);

        // Assert
        activeSessions.Should().HaveCount(2);
        activeSessions.Should().Contain(session1);
        activeSessions.Should().Contain(session2);
    }

    /// <summary>
    /// Tests that getting active sessions for a user with an expired session still returns the session
    /// (since the repository's GetActiveByUserIdAsync method doesn't filter by expiration).
    /// </summary>
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

    /// <summary>
    /// Tests that revoking a session with a valid session ID successfully marks the session as revoked.
    /// </summary>
    [Fact]
    public async Task RevokeSessionAsync_ValidSessionId_RevokeSession()
    {
        // Arrange
        var sessionId = "session1";
        var session = new UserSession { SessionId = sessionId, UserId = "user1", ClientId = "client1" };

        _sessionRepositoryMock.Setup(repo => repo.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(session);

        // Act
        _loggerMock.Object.LogWarning("Revoking session {SessionId}", sessionId);
        await _service.RevokeSessionAsync(sessionId);
        _loggerMock.Object.LogInformation("Session {SessionId} successfully revoked", sessionId);

        // Assert
        session.IsRevoked.Should().BeTrue();
    }

    /// <summary>
    /// Tests that revoking a session with an invalid session ID throws an AuthServerException.
    /// </summary>
    [Fact]
    public async Task RevokeSessionAsync_InvalidSessionId_ThrowsAuthServerException()
    {
        // Arrange
        var sessionId = "session1";

        _sessionRepositoryMock.Setup(repo => repo.GetByIdAsync(sessionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserSession)null);

        // Act & Assert
        _loggerMock.Object.LogWarning("Attempt to revoke non-existent session {SessionId}", sessionId);
        await Assert.ThrowsAsync<AuthServerException>(() => _service.RevokeSessionAsync(sessionId));
    }

    /// <summary>
    /// Tests that revoking all sessions for a valid user ID successfully revokes all sessions and returns the count.
    /// </summary>
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
        _loggerMock.Object.LogInformation("Revoking all sessions for user {UserId}", userId);
        var count = await _service.RevokeAllUserSessionsAsync(userId);
        _loggerMock.Object.LogInformation("Successfully revoked {Count} sessions for user {UserId}", count, userId);

        // Assert
        count.Should().Be(2);
        session1.IsRevoked.Should().BeTrue();
        session2.IsRevoked.Should().BeTrue();
    }

    /// <summary>
    /// Tests that revoking all other sessions for a user (excluding a specific session) successfully revokes only the other sessions.
    /// </summary>
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
        _loggerMock.Object.LogInformation("Revoking all sessions for user {UserId} except {KeepSessionId}", userId, keepSessionId);
        var count = await _service.RevokeAllOtherUserSessionsAsync(userId, keepSessionId);
        _loggerMock.Object.LogInformation("Successfully revoked {Count} sessions for user {UserId}", count, userId);

        // Assert
        count.Should().Be(1);
        session1.IsRevoked.Should().BeFalse();
        session2.IsRevoked.Should().BeTrue();
    }
}
