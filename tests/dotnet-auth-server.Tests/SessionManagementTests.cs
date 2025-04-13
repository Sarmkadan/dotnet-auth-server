#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Tests;

using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public sealed class SessionManagementTests
{
    private readonly UserSessionRepository _sessionRepository;
    private readonly UserSessionService _service;

    public SessionManagementTests()
    {
        _sessionRepository = new UserSessionRepository();
        var logger = new Mock<ILogger<UserSessionService>>().Object;
        var options = new AuthServerOptions
        {
            IssuerUrl = "https://auth.example.com",
            JwtSigningKey = new string('x', 32),
            RefreshTokenLifetimeSeconds = 3600,
            DatabaseConnectionString = ""
        };

        _service = new UserSessionService(_sessionRepository, logger, options);
    }

    [Fact]
    public async Task CreateSession_WithValidParams_ReturnsActiveSession()
    {
        // Arrange & Act
        var session = await _service.CreateSessionAsync(
            userId: "user1",
            clientId: "client1",
            grantedScopes: "openid profile",
            ipAddress: "127.0.0.1",
            userAgent: "TestAgent/1.0");

        // Assert
        session.Should().NotBeNull();
        session.UserId.Should().Be("user1");
        session.ClientId.Should().Be("client1");
        session.GrantedScopes.Should().Be("openid profile");
        session.IpAddress.Should().Be("127.0.0.1");
        session.IsActive().Should().BeTrue("a freshly created session should be active");
        session.IsRevoked.Should().BeFalse();
        session.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task RevokeSession_ExistingSession_MarksAsRevoked()
    {
        // Arrange
        var session = await _service.CreateSessionAsync("user2", "client1", "openid");

        // Act
        await _service.RevokeSessionAsync(session.SessionId, "admin request");

        // Assert
        var all = await _service.GetAllSessionsAsync("user2");
        var revoked = all.First(s => s.SessionId == session.SessionId);

        revoked.IsRevoked.Should().BeTrue("a revoked session must be marked as such");
        revoked.RevocationReason.Should().Be("admin request");
        revoked.IsActive().Should().BeFalse("a revoked session cannot be active");
    }

    [Fact]
    public async Task RevokeSession_NonExistentSession_ThrowsAuthServerException()
    {
        // Arrange & Act
        Func<Task> act = async () =>
            await _service.RevokeSessionAsync("nonexistent-session-id");

        // Assert
        await act.Should().ThrowAsync<AuthServerException>()
            .Where(ex => ex.StatusCode == 404);
    }

    [Fact]
    public async Task GetActiveSessions_AfterRevoke_ExcludesRevokedSession()
    {
        // Arrange
        var session1 = await _service.CreateSessionAsync("user3", "client1", "openid");
        var session2 = await _service.CreateSessionAsync("user3", "client2", "profile");

        await _service.RevokeSessionAsync(session1.SessionId, "test");

        // Act
        var active = (await _service.GetActiveSessionsAsync("user3")).ToList();

        // Assert
        active.Should().HaveCount(1, "only the non-revoked session should appear in active sessions");
        active[0].SessionId.Should().Be(session2.SessionId);
    }

    [Fact]
    public async Task RevokeAllUserSessions_RevokesOnlyThatUsersActiveSessions()
    {
        // Arrange
        await _service.CreateSessionAsync("userA", "client1", "openid");
        await _service.CreateSessionAsync("userA", "client2", "profile");
        await _service.CreateSessionAsync("userB", "client1", "openid"); // must not be revoked

        // Act
        var revokedCount = await _service.RevokeAllUserSessionsAsync("userA", "password changed");

        // Assert
        revokedCount.Should().Be(2, "both sessions for userA should be revoked");

        var userBSessions = (await _service.GetActiveSessionsAsync("userB")).ToList();
        userBSessions.Should().HaveCount(1, "userB's session must not be affected");
    }

    [Fact]
    public async Task GetStats_ReflectsSessionCounts()
    {
        // Arrange
        var s1 = await _service.CreateSessionAsync("statUser1", "client1", "openid");
        var s2 = await _service.CreateSessionAsync("statUser1", "client2", "profile");
        await _service.RevokeSessionAsync(s1.SessionId);

        // Act
        var stats = await _service.GetStatsAsync();

        // Assert
        stats.TotalSessions.Should().BeGreaterThanOrEqualTo(2);
        stats.ActiveSessions.Should().BeGreaterThanOrEqualTo(1);
        stats.RevokedSessions.Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public void UserSession_IsActive_WhenExpired_ReturnsFalse()
    {
        // Arrange
        var session = new UserSession
        {
            UserId = "u1",
            ClientId = "c1",
            GrantedScopes = "openid",
            ExpiresAt = DateTime.UtcNow.AddSeconds(-1) // already expired
        };

        // Assert
        session.IsActive().Should().BeFalse("an expired session is no longer active");
    }

    [Fact]
    public void UserSession_Revoke_SetsRevocationFieldsCorrectly()
    {
        // Arrange
        var session = new UserSession
        {
            UserId = "u2",
            ClientId = "c1",
            GrantedScopes = "openid",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Act
        session.Revoke("security policy");

        // Assert
        session.IsRevoked.Should().BeTrue();
        session.RevocationReason.Should().Be("security policy");
        session.IsActive().Should().BeFalse("a revoked session with future expiry must still be inactive");
    }
}
