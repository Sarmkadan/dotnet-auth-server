#nullable enable
using System;
using Xunit;
using DotnetAuthServer.Domain.Entities;

namespace DotnetAuthServer.Tests;

public class RefreshTokenTests
{
    private RefreshToken CreateValidToken()
    {
        return new RefreshToken
        {
            TokenId = Guid.NewGuid().ToString(),
            TokenHash = "hash123",
            ClientId = "client-1",
            UserId = "user-1",
            GrantedScopes = "openid profile",
            Version = 1,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            IsRevoked = false,
            UsageCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    [Fact]
    public void IsValid_ReturnsTrue_WhenNotRevokedAndNotExpired()
    {
        var token = CreateValidToken();

        Assert.True(token.IsValid());
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenRevoked()
    {
        var token = CreateValidToken();
        token.Revoke("test");

        Assert.False(token.IsValid());
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenExpired()
    {
        var token = CreateValidToken();
        token.ExpiresAt = DateTime.UtcNow.AddSeconds(-1); // already expired

        Assert.False(token.IsValid());
    }

    [Fact]
    public void IsExpired_ReturnsTrue_WhenExpirationTimePassed()
    {
        var token = CreateValidToken();
        token.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);

        Assert.True(token.IsExpired());
    }

    [Fact]
    public void RecordUsage_IncrementsUsageAndUpdatesTimestamps()
    {
        var token = CreateValidToken();
        var beforeUpdated = token.UpdatedAt;

        token.RecordUsage();

        Assert.Equal(1, token.UsageCount);
        Assert.NotNull(token.LastUsedAt);
        Assert.True(token.UpdatedAt > beforeUpdated);
    }

    [Fact]
    public void RecordUsage_Throws_WhenTokenIsRevoked()
    {
        var token = CreateValidToken();
        token.Revoke();

        var ex = Assert.Throws<InvalidOperationException>(() => token.RecordUsage());
        Assert.Equal("Cannot use a revoked refresh token", ex.Message);
    }

    [Fact]
    public void RecordUsage_Throws_WhenTokenIsExpired()
    {
        var token = CreateValidToken();
        token.ExpiresAt = DateTime.UtcNow.AddSeconds(-1);

        var ex = Assert.Throws<InvalidOperationException>(() => token.RecordUsage());
        Assert.Equal("Refresh token has expired", ex.Message);
    }

    [Fact]
    public void Revoke_SetsRevocationProperties()
    {
        var token = CreateValidToken();

        token.Revoke("compromised");

        Assert.True(token.IsRevoked);
        Assert.NotNull(token.RevokedAt);
        Assert.Equal("compromised", token.RevocationReason);
        Assert.True(token.UpdatedAt >= token.RevokedAt);
    }

    [Fact]
    public void Rotate_UpdatesPreviousHashAndVersion()
    {
        var token = CreateValidToken();
        var originalHash = token.TokenHash;
        var originalVersion = token.Version;
        var beforeUpdated = token.UpdatedAt;

        token.Rotate();

        Assert.Equal(originalHash, token.PreviousTokenHash);
        Assert.Equal(originalVersion + 1, token.Version);
        Assert.True(token.UpdatedAt > beforeUpdated);
    }

    [Fact]
    public void SuspiciousUsagePattern_ReturnsFalse_WhenNeverUsed()
    {
        var token = CreateValidToken();

        var result = token.SuspiciousUsagePattern(TimeSpan.FromMinutes(5));

        Assert.False(result);
    }

    [Fact]
    public void SuspiciousUsagePattern_ReturnsFalse_WhenOutsideTimeWindow()
    {
        var token = CreateValidToken();
        token.RecordUsage(); // first use
        token.RecordUsage(); // second use, now UsageCount = 2
        // Simulate last use being far in the past
        token.LastUsedAt = DateTime.UtcNow.AddHours(-1);

        var result = token.SuspiciousUsagePattern(TimeSpan.FromMinutes(5));

        Assert.False(result);
    }

    [Fact]
    public void SuspiciousUsagePattern_ReturnsTrue_WhenWithinTimeWindowAndMultipleUses()
    {
        var token = CreateValidToken();
        token.RecordUsage(); // first use
        token.RecordUsage(); // second use, UsageCount = 2
        // Ensure LastUsedAt is recent (within the window)
        token.LastUsedAt = DateTime.UtcNow.AddSeconds(-30);

        var result = token.SuspiciousUsagePattern(TimeSpan.FromMinutes(1));

        Assert.True(result);
    }
}
