#nullable enable
using System;
using System.Reflection;
using System.Threading;
using DotnetAuthServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DotnetAuthServer.Tests.Services;

public class SessionStateServiceTests
{
    private readonly ILogger<SessionStateService> _logger = NullLogger<SessionStateService>.Instance;

    private SessionStateService CreateService()
        => new SessionStateService(_logger);

    private static void SetSessionExpiration(SessionStateService service, string stateId, TimeSpan offset)
    {
        // Use reflection to get the private _sessions field and modify the ExpiresAt value
        var field = typeof(SessionStateService).GetField("_sessions", BindingFlags.NonPublic | BindingFlags.Instance);
        var dict = (System.Collections.Concurrent.ConcurrentDictionary<string, SessionState>)field!.GetValue(service)!;
        if (dict.TryGetValue(stateId, out var session))
        {
            session.ExpiresAt = DateTime.UtcNow.Add(offset);
        }
    }

    [Fact]
    public void CreateSession_ReturnsStateId_And_StoresSession()
    {
        // Arrange
        var service = CreateService();

        // Act
        var stateId = service.CreateSession("client1", "https://example.com/cb", "openid profile");

        // Assert
        stateId.Should().NotBeNullOrWhiteSpace();
        service.GetActiveSessionCount().Should().Be(1);

        var session = service.GetSession(stateId);
        session.Should().NotBeNull();
        session!.ClientId.Should().Be("client1");
        session.RedirectUri.Should().Be("https://example.com/cb");
        session.RequestedScopes.Should().Be("openid profile");
        session.StateId.Should().Be(stateId);
    }

    [Fact]
    public void GetSession_NullOrWhiteSpace_ReturnsNull()
    {
        var service = CreateService();

        service.GetSession(null!).Should().BeNull();
        service.GetSession("").Should().BeNull();
        service.GetSession("   ").Should().BeNull();
    }

    [Fact]
    public void GetSession_NotFound_ReturnsNull()
    {
        var service = CreateService();

        service.GetSession("nonexistent").Should().BeNull();
    }

    [Fact]
    public void GetSession_Expired_ReturnsNull_And_RemovesSession()
    {
        var service = CreateService();
        var stateId = service.CreateSession("c", "uri", "s");

        // Force expiration to the past
        SetSessionExpiration(service, stateId, TimeSpan.FromMinutes(-20));

        var session = service.GetSession(stateId);
        session.Should().BeNull();

        // Verify it was removed
        service.GetActiveSessionCount().Should().Be(0);
    }

    [Fact]
    public void CompleteSession_ValidId_ReturnsTrue_And_RemovesSession()
    {
        var service = CreateService();
        var stateId = service.CreateSession("c", "uri", "s");

        var result = service.CompleteSession(stateId);
        result.Should().BeTrue();

        // Subsequent calls should return false
        service.CompleteSession(stateId).Should().BeFalse();

        service.GetSession(stateId).Should().BeNull();
        service.GetActiveSessionCount().Should().Be(0);
    }

    [Fact]
    public void CompleteSession_NullOrEmpty_ReturnsFalse()
    {
        var service = CreateService();

        service.CompleteSession(null!).Should().BeFalse();
        service.CompleteSession("").Should().BeFalse();
        service.CompleteSession("   ").Should().BeFalse();
    }

    [Fact]
    public void UpdateSession_ValidId_UpdatesFields()
    {
        var service = CreateService();
        var stateId = service.CreateSession("c", "uri", "s");

        var updated = service.UpdateSession(stateId, userId: "user123", grantedScopes: "openid");
        updated.Should().BeTrue();

        var session = service.GetSession(stateId);
        session!.UserId.Should().Be("user123");
        session.GrantedScopes.Should().Be("openid");
        session.LastUpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateSession_InvalidId_ReturnsFalse()
    {
        var service = CreateService();

        service.UpdateSession("nonexistent", userId: "u", grantedScopes: "s")
            .Should().BeFalse();
    }

    [Fact]
    public void CleanupExpiredSessions_RemovesOnlyExpired()
    {
        var service = CreateService();

        // Create two sessions
        var freshId = service.CreateSession("c1", "uri1", "s1");
        var staleId = service.CreateSession("c2", "uri2", "s2");

        // Expire the second one
        SetSessionExpiration(service, staleId, TimeSpan.FromMinutes(-5));

        var removed = service.CleanupExpiredSessions();
        removed.Should().Be(1);

        // Fresh session should still exist
        service.GetSession(freshId).Should().NotBeNull();
        service.GetSession(staleId).Should().BeNull();
    }

    [Fact]
    public void GetActiveSessionCount_ReflectsCurrentState()
    {
        var service = CreateService();

        service.GetActiveSessionCount().Should().Be(0);

        var id1 = service.CreateSession("c1", "uri1", "s1");
        service.GetActiveSessionCount().Should().Be(1);

        var id2 = service.CreateSession("c2", "uri2", "s2");
        service.GetActiveSessionCount().Should().Be(2);

        service.CompleteSession(id1);
        service.GetActiveSessionCount().Should().Be(1);

        service.CleanupExpiredSessions(); // no expired sessions
        service.GetActiveSessionCount().Should().Be(1);
    }
}
