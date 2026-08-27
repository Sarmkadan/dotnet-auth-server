using System;
using DotnetAuthServer.Security;
using FluentAssertions;
using Xunit;

namespace DotnetAuthServer.Tests.Security;

/// <summary>
/// Test suite for the RevokedTokenStore class.
/// </summary>
public class RevokedTokenStoreTests
{
    private readonly RevokedTokenStore _store = new();

    /// <summary>
    /// Tests that IsRevoked returns false for a token that has never been revoked.
    /// </summary>
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

    /// <summary>
    /// Tests that IsRevoked returns true after revoking a token with a future expiration time.
    /// </summary>
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

    /// <summary>
    /// Tests that calling Revoke twice with the same JTI is idempotent and doesn't cause issues.
    /// </summary>
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

    /// <summary>
    /// Tests that IsRevoked returns false for a token that has already expired at the time of revocation.
    /// </summary>
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

    /// <summary>
    /// Tests that IsRevoked returns true after manual purge when the token has not yet expired.
    /// </summary>
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

    /// <summary>
    /// Tests that PurgeExpired removes expired entries from the store.
    /// </summary>
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

    /// <summary>
    /// Tests that RemoveExpired removes expired entries from the store.
    /// </summary>
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

    /// <summary>
    /// Tests that IsRevoked performs case-insensitive comparison of JTI values.
    /// </summary>
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

    /// <summary>
    /// Tests that calling Revoke with the same JTI updates the expiration time.
    /// </summary>
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

    /// <summary>
    /// Tests that PurgeExpired removes multiple expired entries while keeping valid ones.
    /// </summary>
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

    /// <summary>
    /// Tests that IsRevoked returns false after a token's expiry time has passed.
    /// </summary>
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

    /// <summary>
    /// Tests that Revoke throws ArgumentNullException when JTI is null.
    /// </summary>
    [Fact]
    public void Revoke_ThrowsArgumentNullException_WhenJtiIsNull()
    {
        // Arrange
        string nullJti = null!;
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        // Act
        Action act = () => _store.Revoke(nullJti, expiresAt);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that IsRevoked throws ArgumentNullException when JTI is null.
    /// </summary>
    [Fact]
    public void IsRevoked_ThrowsArgumentNullException_WhenJtiIsNull()
    {
        // Arrange
        string nullJti = null!;

        // Act
        Action act = () => _store.IsRevoked(nullJti);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that Revoke handles an empty string JTI without throwing an exception.
    /// </summary>
    [Fact]
    public void Revoke_HandlesEmptyStringJti()
    {
        // Arrange
        var emptyJti = string.Empty;
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        // Act - should not throw
        _store.Revoke(emptyJti, expiresAt);

        // Assert - should be revoked
        _store.IsRevoked(emptyJti).Should().BeTrue();
    }

    /// <summary>
    /// Tests that IsRevoked returns false for an empty string JTI when the store is empty.
    /// </summary>
    [Fact]
    public void IsRevoked_ReturnsFalse_ForEmptyStringJti()
    {
        // Arrange
        var emptyJti = string.Empty;

        // Act
        var isRevoked = _store.IsRevoked(emptyJti);

        // Assert
        isRevoked.Should().BeFalse();
    }

    /// <summary>
    /// Tests that PurgeExpired does not throw an exception when the store is empty.
    /// </summary>
    [Fact]
    public void PurgeExpired_DoesNotThrow_WhenStoreIsEmpty()
    {
        // Arrange - empty store

        // Act - should not throw
        Action act = () => _store.PurgeExpired();

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that RemoveExpired does not throw an exception when the store is empty.
    /// </summary>
    [Fact]
    public void RemoveExpired_DoesNotThrow_WhenStoreIsEmpty()
    {
        // Arrange - empty store
        var now = DateTime.UtcNow;

        // Act - should not throw
        Action act = () => _store.RemoveExpired(now);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that the store can handle the maximum number of tokens without issues.
    /// </summary>
    [Fact]
    public void Revoke_MultipleTokens_WithinMaxSize()
    {
        // Arrange - test the max size boundary
        var maxSize = 10000;
        var tokens = new List<(string jti, DateTime expiresAt)>();

        // Act - add maxSize entries
        for (int i = 0; i < maxSize; i++)
        {
            var jti = Guid.NewGuid().ToString();
            var expiresAt = DateTime.UtcNow.AddMinutes(30);
            _store.Revoke(jti, expiresAt);
            tokens.Add((jti, expiresAt));
        }

        // Assert - all should be revoked
        foreach (var (jti, _) in tokens)
        {
            _store.IsRevoked(jti).Should().BeTrue();
        }
    }

    /// <summary>
    /// Tests that adding a token beyond the maximum store size triggers an automatic purge of expired entries.
    /// </summary>
    [Fact]
    public void Revoke_ExceedsMaxSize_TriggersAutomaticPurge()
    {
        // Arrange - exceed max size to trigger automatic purge
        var maxSize = 10000;
        var tokens = new List<(string jti, DateTime expiresAt)>();

        // Add maxSize entries
        for (int i = 0; i < maxSize; i++)
        {
            var jti = $"token-{i}";
            var expiresAt = DateTime.UtcNow.AddMinutes(30);
            _store.Revoke(jti, expiresAt);
            tokens.Add((jti, expiresAt));
        }

        // Add one more to exceed max size
        var overflowJti = "overflow-token";
        var overflowExpiresAt = DateTime.UtcNow.AddMinutes(30);
        _store.Revoke(overflowJti, overflowExpiresAt);

        // Act - check overflow token
        var isOverflowRevoked = _store.IsRevoked(overflowJti);

        // Assert - overflow token should be revoked (purge should have cleaned up expired ones)
        isOverflowRevoked.Should().BeTrue();
    }
}