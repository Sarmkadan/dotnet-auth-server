#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetAuthServer.Domain.Entities;
using Xunit;

namespace DotnetAuthServer.Tests;

public sealed class UserTests
{
    [Fact]
    public void ToString_LockedUser_ShowsLockStateWithoutPasswordHashOrEmail()
    {
        // Arrange
        var user = new User
        {
            UserId = "user-42",
            Username = "alice",
            Email = "alice@example.com",
            PasswordHash = "pbkdf2-hash-must-not-leak",
            MfaEnabled = true
        };
        user.LockAccount(TimeSpan.FromMinutes(15));

        // Act
        var text = user.ToString();

        // Assert
        Assert.Contains("UserId = user-42", text);
        Assert.Contains("Username = alice", text);
        Assert.Contains("LockedOut = True", text);
        Assert.Contains("MfaEnabled = True", text);
        Assert.DoesNotContain("pbkdf2-hash-must-not-leak", text);
        Assert.DoesNotContain("alice@example.com", text);
    }

    [Fact]
    public void ToString_ExpiredLock_ReportsNotLockedWithoutClearingLockState()
    {
        // Arrange
        var user = new User
        {
            UserId = "user-7",
            Username = "bob",
            PasswordHash = "hash",
            LockedUntil = DateTime.UtcNow.AddMinutes(-1)
        };

        // Act
        var text = user.ToString();

        // Assert
        Assert.Contains("LockedOut = False", text);
        Assert.NotNull(user.LockedUntil);
    }
}
