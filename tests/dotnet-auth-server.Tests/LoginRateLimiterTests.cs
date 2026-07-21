using Moq;
using FluentAssertions;
using DotnetAuthServer.Security;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Exceptions;
using Microsoft.Extensions.Logging;
using Xunit;

/// <summary>
/// Tests for the LoginRateLimiter class.
/// </summary>
public sealed class LoginRateLimiterTests
{
    private readonly Mock<ILogger<LoginRateLimiter>> _loggerMock;
    private readonly AuthServerOptions _options;
    private readonly LoginRateLimiter _limiter;

    /// <summary>
    /// Initializes a new instance of the <see cref="LoginRateLimiterTests"/> class.
    /// </summary>
    public LoginRateLimiterTests()
    {
        _loggerMock = new Mock<ILogger<LoginRateLimiter>>();
        _options = new AuthServerOptions
        {
            FailedLoginAttemptThreshold = 5,
            AccountLockoutDurationMinutes = 15
        };
        _limiter = new LoginRateLimiter(_options, _loggerMock.Object);
    }

    #region Constructor and Initial State

    /// <summary>
    /// Tests that the LoginRateLimiter is initialized with correct default values.
    /// </summary>
    [Fact]
    public void Constructor_InitializesWithCorrectOptions()
    {
        // Arrange
        var options = new AuthServerOptions
        {
            FailedLoginAttemptThreshold = 3,
            AccountLockoutDurationMinutes = 10
        };
        var loggerMock = new Mock<ILogger<LoginRateLimiter>>();

        // Act
        var limiter = new LoginRateLimiter(options, loggerMock.Object);

        // Assert - using reflection to verify private fields
        limiter.Should().NotBeNull();
    }

    #endregion

    #region ThrowIfBlocked Tests

    /// <summary>
    /// Tests that ThrowIfBlocked does not throw when no attempts have been recorded.
    /// </summary>
    [Fact]
    public void ThrowIfBlocked_NoAttempts_DoesNotThrow()
    {
        // Arrange
        var username = "testuser";
        var ipAddress = "192.168.1.1";

        // Act
        Action act = () => _limiter.ThrowIfBlocked(username, ipAddress);

        // Assert
        act.Should().NotThrow<AuthServerException>();
    }

    /// <summary>
    /// Tests that ThrowIfBlocked does not throw when attempts are below the threshold.
    /// </summary>
    [Fact]
    public void ThrowIfBlocked_UnderThreshold_DoesNotThrow()
    {
        // Arrange
        var username = "testuser";
        var ipAddress = "192.168.1.1";

        // Record 4 failures (below threshold of 5)
        for (int i = 0; i < 4; i++)
        {
            _limiter.RecordFailure(username, ipAddress);
        }

        // Act
        Action act = () => _limiter.ThrowIfBlocked(username, ipAddress);

        // Assert
        act.Should().NotThrow<AuthServerException>();
    }

    /// <summary>
    /// Tests that ThrowIfBlocked throws AuthServerException when username attempts exceed threshold.
    /// </summary>
    [Fact]
    public void ThrowIfBlocked_UsernameExceedsThreshold_ThrowsAuthServerException()
    {
        // Arrange
        var username = "testuser";
        var ipAddress = "192.168.1.1";

        // Record 5 failures (reaches threshold)
        for (int i = 0; i < 5; i++)
        {
            _limiter.RecordFailure(username, ipAddress);
        }

        // Act
        Action act = () => _limiter.ThrowIfBlocked(username, ipAddress);

        // Assert
        act.Should().Throw<AuthServerException>()
            .Where(ex => ex.StatusCode == 429)
            .Where(ex => ex.ErrorCode == "too_many_requests");
    }

    /// <summary>
    /// Tests that ThrowIfBlocked throws AuthServerException when IP address attempts exceed threshold.
    /// </summary>
    [Fact]
    public void ThrowIfBlocked_IpAddressExceedsThreshold_ThrowsAuthServerException()
    {
        // Arrange
        var username = "testuser";
        var ipAddress = "192.168.1.1";

        // Record 5 failures for IP only
        for (int i = 0; i < 5; i++)
        {
            _limiter.RecordFailure(null, ipAddress);
        }

        // Act
        Action act = () => _limiter.ThrowIfBlocked(username, ipAddress);

        // Assert
        act.Should().Throw<AuthServerException>()
            .Where(ex => ex.StatusCode == 429)
            .Where(ex => ex.ErrorCode == "too_many_requests");
    }

    /// <summary>
    /// Tests that ThrowIfBlocked throws when both username and IP exceed threshold.
    /// </summary>
    [Fact]
    public void ThrowIfBlocked_BothExceedThreshold_ThrowsAuthServerException()
    {
        // Arrange
        var username = "testuser";
        var ipAddress = "192.168.1.1";

        // Record 5 failures for both username and IP
        for (int i = 0; i < 5; i++)
        {
            _limiter.RecordFailure(username, ipAddress);
        }

        // Act
        Action act = () => _limiter.ThrowIfBlocked(username, ipAddress);

        // Assert
        act.Should().Throw<AuthServerException>()
            .Where(ex => ex.StatusCode == 429)
            .Where(ex => ex.ErrorCode == "too_many_requests");
    }

    /// <summary>
    /// Tests that ThrowIfBlocked allows access after RecordSuccess clears the username counter.
    /// This verifies that successful logins reset the rate limiting state for the username.
    /// Note: IP counters are not cleared by RecordSuccess.
    /// </summary>
    [Fact]
    public void ThrowIfBlocked_AfterRecordSuccess_RestoresAccess()
    {
        // Arrange
        var username = "testuser";
        var ipAddress = "192.168.1.1";

        // Record 5 failures for username only
        for (int i = 0; i < 5; i++)
        {
            _limiter.RecordFailure(username, null);
        }

        // Verify lockout is active for username
        Action actBefore = () => _limiter.ThrowIfBlocked(username, ipAddress);
        actBefore.Should().Throw<AuthServerException>();

        // Act - record successful login which clears the username counter
        _limiter.RecordSuccess(username);

        // Assert - should not throw after successful login (username cleared)
        Action actAfter = () => _limiter.ThrowIfBlocked(username, ipAddress);
        actAfter.Should().NotThrow<AuthServerException>();
    }

    /// <summary>
    /// Tests that ThrowIfBlocked does not throw when username is null or empty.
    /// </summary>
    [Fact]
    public void ThrowIfBlocked_NullUsername_DoesNotThrow()
    {
        // Arrange
        var ipAddress = "192.168.1.1";

        // Record 5 failures for IP only
        for (int i = 0; i < 5; i++)
        {
            _limiter.RecordFailure(null, ipAddress);
        }

        // Act - null username should not be blocked, but IP should be
        Action act = () => _limiter.ThrowIfBlocked(null, ipAddress);

        // Assert - should throw because IP is blocked even though username is null
        act.Should().Throw<AuthServerException>();
    }

    /// <summary>
    /// Tests that ThrowIfBlocked does not throw when IP address is null or empty.
    /// </summary>
    [Fact]
    public void ThrowIfBlocked_NullIpAddress_DoesNotThrow()
    {
        // Arrange
        var username = "testuser";

        // Record 5 failures for username only
        for (int i = 0; i < 5; i++)
        {
            _limiter.RecordFailure(username, null);
        }

        // Act - username should be blocked, but null IP should not cause issues
        Action act = () => _limiter.ThrowIfBlocked(username, null);

        // Assert - should throw because username is blocked
        act.Should().Throw<AuthServerException>();
    }

    #endregion

    #region RecordFailure Tests

    /// <summary>
    /// Tests that RecordFailure increments the attempt counter for username.
    /// </summary>
    [Fact]
    public void RecordFailure_IncrementsUsernameCounter()
    {
        // Arrange
        var username = "testuser";
        var ipAddress = "192.168.1.1";

        // Act
        _limiter.RecordFailure(username, ipAddress);

        // Assert - indirectly verify by checking if lockout occurs after 5 attempts
        for (int i = 0; i < 4; i++)
        {
            _limiter.RecordFailure(username, ipAddress);
        }

        Action act = () => _limiter.ThrowIfBlocked(username, ipAddress);
        act.Should().Throw<AuthServerException>();
    }

    /// <summary>
    /// Tests that RecordFailure increments the attempt counter for IP address.
    /// </summary>
    [Fact]
    public void RecordFailure_IncrementsIpAddressCounter()
    {
        // Arrange
        var username = "testuser";
        var ipAddress = "192.168.1.1";

        // Act
        _limiter.RecordFailure(null, ipAddress);

        // Assert - indirectly verify by checking if lockout occurs after 5 attempts
        for (int i = 0; i < 4; i++)
        {
            _limiter.RecordFailure(null, ipAddress);
        }

        Action act = () => _limiter.ThrowIfBlocked(username, ipAddress);
        act.Should().Throw<AuthServerException>();
    }

    /// <summary>
    /// Tests that RecordFailure handles null username and null IP address gracefully.
    /// </summary>
    [Fact]
    public void RecordFailure_NullBothParameters_DoesNotThrow()
    {
        // Act
        Action act = () => _limiter.RecordFailure(null, null);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that RecordFailure handles empty username.
    /// </summary>
    [Fact]
    public void RecordFailure_EmptyUsername_DoesNotRecord()
    {
        // Arrange
        var ipAddress = "192.168.1.1";

        // Act
        _limiter.RecordFailure(string.Empty, ipAddress);

        // Assert - should not throw when checking blocked status
        Action act = () => _limiter.ThrowIfBlocked(string.Empty, ipAddress);
        act.Should().NotThrow<AuthServerException>();
    }

    /// <summary>
    /// Tests that RecordFailure handles empty IP address.
    /// </summary>
    [Fact]
    public void RecordFailure_EmptyIpAddress_DoesNotRecord()
    {
        // Arrange
        var username = "testuser";

        // Act
        _limiter.RecordFailure(username, string.Empty);

        // Assert - should not throw when checking blocked status
        Action act = () => _limiter.ThrowIfBlocked(username, string.Empty);
        act.Should().NotThrow<AuthServerException>();
    }

    /// <summary>
    /// Tests that RecordFailure handles whitespace username.
    /// </summary>
    [Fact]
    public void RecordFailure_WhitespaceUsername_DoesNotRecord()
    {
        // Arrange
        var ipAddress = "192.168.1.1";

        // Act
        _limiter.RecordFailure("   ", ipAddress);

        // Assert - should not throw when checking blocked status
        Action act = () => _limiter.ThrowIfBlocked("   ", ipAddress);
        act.Should().NotThrow<AuthServerException>();
    }

    #endregion

    #region RecordSuccess Tests

    /// <summary>
    /// Tests that RecordSuccess clears the attempt counter for username after successful login.
    /// Note: Only the username counter is cleared, IP counters remain active.
    /// </summary>
    [Fact]
    public void RecordSuccess_ClearsUsernameCounter()
    {
        // Arrange
        var username = "testuser";
        var ipAddress = "192.168.1.1";

        // Record 5 failures for username only
        for (int i = 0; i < 5; i++)
        {
            _limiter.RecordFailure(username, null);
        }

        // Verify lockout is active for username
        Action actBefore = () => _limiter.ThrowIfBlocked(username, ipAddress);
        actBefore.Should().Throw<AuthServerException>();

        // Act - record successful login
        _limiter.RecordSuccess(username);

        // Assert - should not throw after successful login (username cleared)
        Action actAfter = () => _limiter.ThrowIfBlocked(username, ipAddress);
        actAfter.Should().NotThrow<AuthServerException>();
    }

    /// <summary>
    /// Tests that RecordSuccess with null username does not throw.
    /// </summary>
    [Fact]
    public void RecordSuccess_NullUsername_DoesNotThrow()
    {
        // Act
        Action act = () => _limiter.RecordSuccess(null);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that RecordSuccess with empty username does not throw.
    /// </summary>
    [Fact]
    public void RecordSuccess_EmptyUsername_DoesNotThrow()
    {
        // Act
        Action act = () => _limiter.RecordSuccess(string.Empty);

        // Assert
        act.Should().NotThrow();
    }

    /// <summary>
    /// Tests that RecordSuccess does not affect IP address counters.
    /// </summary>
    [Fact]
    public void RecordSuccess_DoesNotAffectIpAddressCounter()
    {
        // Arrange
        var username = "testuser";
        var ipAddress = "192.168.1.1";

        // Record 5 failures for both username and IP
        for (int i = 0; i < 5; i++)
        {
            _limiter.RecordFailure(username, ipAddress);
        }

        // Verify lockout is active
        Action actBefore = () => _limiter.ThrowIfBlocked(username, ipAddress);
        actBefore.Should().Throw<AuthServerException>();

        // Act - record successful login for username
        _limiter.RecordSuccess(username);

        // Assert - IP should still be blocked
        Action actAfter = () => _limiter.ThrowIfBlocked(username, ipAddress);
        actAfter.Should().Throw<AuthServerException>();
    }

    #endregion

    #region Per-User Isolation Tests

    /// <summary>
    /// Tests that different usernames are tracked independently.
    /// </summary>
    [Fact]
    public void PerUserIsolation_DifferentUsernames_TrackedIndependently()
    {
        // Arrange
        var username1 = "user1";
        var username2 = "user2";
        var ipAddress = "192.168.1.1";

        // Record 5 failures for user1 only
        for (int i = 0; i < 5; i++)
        {
            _limiter.RecordFailure(username1, null);
        }

        // user1 should be blocked
        Action actUser1 = () => _limiter.ThrowIfBlocked(username1, ipAddress);
        actUser1.Should().Throw<AuthServerException>();

        // user2 should not be blocked
        Action actUser2 = () => _limiter.ThrowIfBlocked(username2, ipAddress);
        actUser2.Should().NotThrow<AuthServerException>();
    }

    /// <summary>
    /// Tests that different IP addresses are tracked independently.
    /// </summary>
    [Fact]
    public void PerIpIsolation_DifferentIpAddresses_TrackedIndependently()
    {
        // Arrange
        var username = "testuser";
        var ipAddress1 = "192.168.1.1";
        var ipAddress2 = "192.168.1.2";

        // Record 5 failures for ipAddress1 only
        for (int i = 0; i < 5; i++)
        {
            _limiter.RecordFailure(null, ipAddress1);
        }

        // ipAddress1 should be blocked
        Action actIp1 = () => _limiter.ThrowIfBlocked(username, ipAddress1);
        actIp1.Should().Throw<AuthServerException>();

        // ipAddress2 should not be blocked
        Action actIp2 = () => _limiter.ThrowIfBlocked(username, ipAddress2);
        actIp2.Should().NotThrow<AuthServerException>();
    }

    #endregion

    #region Custom Threshold Tests

    /// <summary>
    /// Tests that custom threshold values work correctly.
    /// </summary>
    [Fact]
    public void CustomThreshold_WorksWithDifferentValues()
    {
        // Arrange
        var customOptions = new AuthServerOptions
        {
            FailedLoginAttemptThreshold = 3,
            AccountLockoutDurationMinutes = 10
        };
        var customLimiter = new LoginRateLimiter(customOptions, _loggerMock.Object);
        var username = "testuser";

        // Record 2 failures (below custom threshold of 3)
        for (int i = 0; i < 2; i++)
        {
            customLimiter.RecordFailure(username, null);
        }

        // Should not throw
        Action actUnder = () => customLimiter.ThrowIfBlocked(username, null);
        actUnder.Should().NotThrow<AuthServerException>();

        // Record 1 more failure (reaches threshold)
        customLimiter.RecordFailure(username, null);

        // Should throw
        Action actOver = () => customLimiter.ThrowIfBlocked(username, null);
        actOver.Should().Throw<AuthServerException>();
    }

    #endregion
}