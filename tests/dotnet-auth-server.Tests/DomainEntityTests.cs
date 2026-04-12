// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Tests;

using DotnetAuthServer.Domain.Entities;
using FluentAssertions;
using Xunit;

public class DomainEntityTests
{
    // -------------------------------------------------------------------------
    // User
    // -------------------------------------------------------------------------

    [Fact]
    public void User_RecordFailedLogin_WhenThresholdReached_LocksAccountAndResetsCounter()
    {
        // Arrange
        var user = new User
        {
            UserId = "u1",
            Username = "alice",
            Email = "alice@example.com",
            PasswordHash = "hash"
        };

        // Act — reach the default threshold of 5 attempts
        for (var i = 0; i < 5; i++)
            user.RecordFailedLogin();

        // Assert
        user.IsLocked().Should().BeTrue("the account must be locked after hitting the attempt threshold");
        user.LockedUntil.Should().NotBeNull();
        user.FailedLoginAttempts.Should().Be(0,
            "LockAccount resets the counter so the lockout duration is not extended unfairly");
    }

    [Fact]
    public void User_RecordFailedLogin_BelowThreshold_DoesNotLockAccount()
    {
        // Arrange
        var user = new User
        {
            UserId = "u2",
            Username = "bob",
            Email = "bob@example.com",
            PasswordHash = "hash"
        };

        // Act — 4 attempts, one below the default threshold of 5
        for (var i = 0; i < 4; i++)
            user.RecordFailedLogin();

        // Assert
        user.IsLocked().Should().BeFalse();
        user.FailedLoginAttempts.Should().Be(4);
    }

    [Fact]
    public void User_RecordSuccessfulLogin_ResetsFailedAttemptsAndSetsLastLoginAt()
    {
        // Arrange
        var user = new User
        {
            UserId = "u3",
            Username = "charlie",
            Email = "charlie@example.com",
            PasswordHash = "hash",
            FailedLoginAttempts = 3
        };

        // Act
        user.RecordSuccessfulLogin();

        // Assert
        user.FailedLoginAttempts.Should().Be(0);
        user.LastLoginAt.Should().NotBeNull();
        user.LastLoginAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        user.LockedUntil.Should().BeNull("a successful login clears any existing lockout");
    }

    [Fact]
    public void User_IsLocked_WhenLockHasExpired_ReturnsFalseAndClearsLockedUntil()
    {
        // Arrange — simulate a past lockout that already expired
        var user = new User
        {
            UserId = "u4",
            Username = "diana",
            Email = "diana@example.com",
            PasswordHash = "hash",
            LockedUntil = DateTime.UtcNow.AddSeconds(-1)
        };

        // Act
        var locked = user.IsLocked();

        // Assert
        locked.Should().BeFalse("the lockout window has already passed");
        user.LockedUntil.Should().BeNull("IsLocked auto-clears the stale lock");
    }

    // -------------------------------------------------------------------------
    // Client
    // -------------------------------------------------------------------------

    [Fact]
    public void Client_IsValid_WhenConfidentialClientHasNoSecretHash_ReturnsFalse()
    {
        // Arrange
        var client = new Client
        {
            ClientId = "api-client",
            ClientName = "API Client",
            IsConfidential = true,
            ClientSecretHash = null,          // missing required secret
            RedirectUris = ["https://app.example.com/callback"],
            AllowedGrantTypes = ["authorization_code"]
        };

        // Act
        var valid = client.IsValid();

        // Assert
        valid.Should().BeFalse(
            "a confidential client must have a hashed secret to authenticate");
    }

    [Fact]
    public void Client_IsRedirectUriValid_MatchesCaseInsensitively()
    {
        // Arrange
        var client = new Client
        {
            ClientId = "spa",
            ClientName = "SPA",
            RedirectUris = ["https://App.Example.Com/Callback"],
            AllowedGrantTypes = ["authorization_code"]
        };

        // Act — lowercase variant of the registered URI
        var result = client.IsRedirectUriValid("https://app.example.com/callback");

        // Assert
        result.Should().BeTrue("redirect URI comparison must be case-insensitive per OAuth2 spec");
    }

    [Fact]
    public void Client_IsGrantTypeAllowed_WhenGrantTypeRegistered_ReturnsTrue()
    {
        // Arrange
        var client = new Client
        {
            ClientId = "m2m",
            ClientName = "M2M Client",
            IsConfidential = true,
            ClientSecretHash = "hash",
            RedirectUris = ["https://example.com"],
            AllowedGrantTypes = ["client_credentials", "refresh_token"]
        };

        // Act
        var allowed = client.IsGrantTypeAllowed("CLIENT_CREDENTIALS");

        // Assert
        allowed.Should().BeTrue("grant type comparison must be case-insensitive");
    }

    // -------------------------------------------------------------------------
    // RefreshToken
    // -------------------------------------------------------------------------

    [Fact]
    public void RefreshToken_Rotate_IncrementsVersionAndPreservesPreviousHash()
    {
        // Arrange
        var token = new RefreshToken
        {
            TokenId = Guid.NewGuid().ToString(),
            TokenHash = "original_hash",
            ClientId = "client1",
            UserId = "user1",
            GrantedScopes = "openid profile",
            ExpiresAt = DateTime.UtcNow.AddDays(30)
        };

        var originalHash = token.TokenHash;
        var originalVersion = token.Version;

        // Act
        token.Rotate();

        // Assert
        token.Version.Should().Be(originalVersion + 1,
            "each rotation must advance the version counter for replay detection");
        token.PreviousTokenHash.Should().Be(originalHash,
            "the previous hash is stored to detect stolen token re-use");
    }

    [Fact]
    public void RefreshToken_RecordUsage_WhenTokenIsRevoked_ThrowsInvalidOperationException()
    {
        // Arrange
        var token = new RefreshToken
        {
            TokenId = Guid.NewGuid().ToString(),
            TokenHash = "hash",
            ClientId = "client1",
            UserId = "user1",
            GrantedScopes = "openid",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsRevoked = true
        };

        // Act
        Action act = () => token.RecordUsage();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*revoked*", "using a revoked token must always fail");
    }

    [Fact]
    public void RefreshToken_IsValid_WhenRevokedAndNotExpired_ReturnsFalse()
    {
        // Arrange
        var token = new RefreshToken
        {
            TokenId = Guid.NewGuid().ToString(),
            TokenHash = "hash",
            ClientId = "client1",
            UserId = "user1",
            GrantedScopes = "openid",
            ExpiresAt = DateTime.UtcNow.AddDays(30),
            IsRevoked = true
        };

        // Act & Assert
        token.IsValid().Should().BeFalse(
            "revocation must invalidate a token regardless of its expiry time");
    }
}
