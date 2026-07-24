#nullable enable
using System;
using DotnetAuthServer.Domain.Entities;
using Xunit;

namespace DotnetAuthServer.Tests;

public class UserSessionTests
{
    [Fact]
    public void NewSession_HasValidDefaults()
    {
        // Arrange
        var session = new UserSession
        {
            UserId = "user-123",
            ClientId = "client-abc",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(session.SessionId));
        Assert.Equal("user-123", session.UserId);
        Assert.Equal("client-abc", session.ClientId);
        Assert.Null(session.IpAddress);
        Assert.Null(session.UserAgent);
        Assert.Equal(string.Empty, session.GrantedScopes);
        Assert.NotNull(session.CreatedAt);
        Assert.True(session.ExpiresAt > DateTime.UtcNow);
        Assert.Null(session.LastActivityAt);
        Assert.False(session.IsRevoked);
        Assert.Null(session.RevocationReason);
    }

    [Fact]
    public void IsActive_ReturnsTrue_WhenNotRevokedAndNotExpired()
    {
        var session = new UserSession
        {
            UserId = "u",
            ClientId = "c",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };

        Assert.True(session.IsActive());
    }

    [Fact]
    public void IsActive_ReturnsFalse_WhenRevoked()
    {
        var session = new UserSession
        {
            UserId = "u",
            ClientId = "c",
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        };
        session.Revoke("test");

        Assert.False(session.IsActive());
    }

    [Fact]
    public void IsActive_ReturnsFalse_WhenExpired()
    {
        var session = new UserSession
        {
            UserId = "u",
            ClientId = "c",
            ExpiresAt = DateTime.UtcNow.AddSeconds(-1) // already past
        };

        Assert.False(session.IsActive());
    }

    [Fact]
    public void Revoke_SetsIsRevokedAndReason()
    {
        var session = new UserSession
        {
            UserId = "u",
            ClientId = "c",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        session.Revoke("compromised");

        Assert.True(session.IsRevoked);
        Assert.Equal("compromised", session.RevocationReason);
    }

    [Fact]
    public void Revoke_WithNullReason_SetsIsRevokedOnly()
    {
        var session = new UserSession
        {
            UserId = "u",
            ClientId = "c",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        session.Revoke();

        Assert.True(session.IsRevoked);
        Assert.Null(session.RevocationReason);
    }

    [Fact]
    public void Touch_UpdatesLastActivityAt()
    {
        var session = new UserSession
        {
            UserId = "u",
            ClientId = "c",
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Ensure initial value is null
        Assert.Null(session.LastActivityAt);

        // Act
        session.Touch();

        // Assert that LastActivityAt is set to a recent time (within 1 second)
        Assert.NotNull(session.LastActivityAt);
        var now = DateTime.UtcNow;
        var diff = now - session.LastActivityAt!.Value;
        Assert.InRange(diff.TotalSeconds, 0, 1);
    }

    [Fact]
    public void GrantedScopes_CanBeSetAndRead()
    {
        var session = new UserSession
        {
            UserId = "u",
            ClientId = "c",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            GrantedScopes = "openid profile email"
        };

        Assert.Equal("openid profile email", session.GrantedScopes);
    }
}
