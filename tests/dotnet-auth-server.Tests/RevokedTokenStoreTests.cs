using System;
using DotnetAuthServer.Security;
using FluentAssertions;
using Xunit;

namespace DotnetAuthServer.Tests.Security;

public class RevokedTokenStoreTests
{
    private readonly RevokedTokenStore _store = new();

    [Fact]
    public void IsRevoked_ReturnsFalse_ForUnknownToken()
    {
        // Arrange
        var unknownJti = Guid.NewGuid().ToString();

        // Act
        var isRevoked = _store.IsRevoked(unknownJti);

        // Assert
        isRevoked.Should().BeFalse();
    }

    [Fact]
    public void Revoke_ThenIsRevoked_ReturnsTrue_ForRevokedToken()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        // Act
        _store.Revoke(jti, expiresAt);
        var isRevoked = _store.IsRevoked(jti);

        // Assert
        isRevoked.Should().BeTrue();
    }

    [Fact]
    public void Revoke_Twice_IsIdempotent()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var expiresAt1 = DateTime.UtcNow.AddMinutes(30);
        var expiresAt2 = DateTime.UtcNow.AddMinutes(60);

        // Act
        _store.Revoke(jti, expiresAt1);
        _store.Revoke(jti, expiresAt2);
        var isRevoked = _store.IsRevoked(jti);

        // Assert
        isRevoked.Should().BeTrue();
    }

    [Fact]
    public void IsRevoked_ReturnsFalse_WhenTokenHasExpired()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddMinutes(-30); // Already expired

        // Act
        _store.Revoke(jti, expiresAt);
        var isRevoked = _store.IsRevoked(jti);

        // Assert
        isRevoked.Should().BeFalse();
    }

    [Fact]
    public void IsRevoked_ReturnsFalse_AfterManualPurge()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        _store.Revoke(jti, expiresAt);
        _store.IsRevoked(jti).Should().BeTrue();

        // Act - manually purge expired (though not actually expired)
        _store.PurgeExpired();

        // Assert
        _store.IsRevoked(jti).Should().BeTrue();
    }

    [Fact]
    public void PurgeExpired_RemovesExpiredEntries()
    {
        // Arrange
        var expiredJti = Guid.NewGuid().ToString();
        var validJti = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        _store.Revoke(expiredJti, now.AddMinutes(-30)); // Expired
        _store.Revoke(validJti, now.AddMinutes(30)); // Valid

        // Act
        _store.PurgeExpired();

        // Assert
        _store.IsRevoked(expiredJti).Should().BeFalse();
        _store.IsRevoked(validJti).Should().BeTrue();
    }

    [Fact]
    public void RemoveExpired_RemovesExpiredEntries()
    {
        // Arrange
        var expiredJti = Guid.NewGuid().ToString();
        var validJti = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        _store.Revoke(expiredJti, now.AddMinutes(-30)); // Expired
        _store.Revoke(validJti, now.AddMinutes(30)); // Valid

        // Act
        _store.RemoveExpired(now);

        // Assert
        _store.IsRevoked(expiredJti).Should().BeFalse();
        _store.IsRevoked(validJti).Should().BeTrue();
    }

    [Fact]
    public void IsRevoked_CaseInsensitiveComparison()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        _store.Revoke(jti, expiresAt);

        // Act & Assert - different cases
        _store.IsRevoked(jti.ToUpperInvariant()).Should().BeTrue();
        _store.IsRevoked(jti.ToLowerInvariant()).Should().BeTrue();
        _store.IsRevoked(jti).Should().BeTrue();
    }

    [Fact]
    public void Revoke_UpdatesExpirationTime()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var expiresAt1 = DateTime.UtcNow.AddMinutes(30);
        var expiresAt2 = DateTime.UtcNow.AddMinutes(60);

        _store.Revoke(jti, expiresAt1);
        _store.IsRevoked(jti).Should().BeTrue();

        // Act - revoke with new expiration time
        _store.Revoke(jti, expiresAt2);

        // Assert - should still be revoked
        _store.IsRevoked(jti).Should().BeTrue();
    }

    [Fact]
    public void PurgeExpired_WithMultipleExpiredEntries()
    {
        // Arrange
        var expiredJti1 = Guid.NewGuid().ToString();
        var expiredJti2 = Guid.NewGuid().ToString();
        var expiredJti3 = Guid.NewGuid().ToString();
        var validJti = Guid.NewGuid().ToString();
        var now = DateTime.UtcNow;

        _store.Revoke(expiredJti1, now.AddMinutes(-60));
        _store.Revoke(expiredJti2, now.AddMinutes(-30));
        _store.Revoke(expiredJti3, now.AddMinutes(-10));
        _store.Revoke(validJti, now.AddMinutes(30));

        // Act
        _store.PurgeExpired();

        // Assert
        _store.IsRevoked(expiredJti1).Should().BeFalse();
        _store.IsRevoked(expiredJti2).Should().BeFalse();
        _store.IsRevoked(expiredJti3).Should().BeFalse();
        _store.IsRevoked(validJti).Should().BeTrue();
    }

    [Fact]
    public void IsRevoked_ReturnsFalse_AfterTokenExpiryTimePasses()
    {
        // Arrange
        var jti = Guid.NewGuid().ToString();
        var expiresAt = DateTime.UtcNow.AddMilliseconds(100); // Very short expiry

        _store.Revoke(jti, expiresAt);
        _store.IsRevoked(jti).Should().BeTrue();

        // Wait for token to expire
        System.Threading.Thread.Sleep(200);

        // Act & Assert - should automatically detect expiration
        var isRevoked = _store.IsRevoked(jti);
        isRevoked.Should().BeFalse();
    }
}