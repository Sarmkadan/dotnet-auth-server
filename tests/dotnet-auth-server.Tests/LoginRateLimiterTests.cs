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

    #endregion

    #region RecordSuccess Tests

    /// <summary>
    /// Tests that RecordSuccess clears the attempt counter for username after successful login.
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

    #region Edge Cases and Boundary Values

    /// <summary>
    /// Tests that exactly threshold failures triggers block.
    /// </summary>
    [Fact]
    public void ThrowIfBlocked_ExactlyThresholdAttempts_Throws()
    {
        // Arrange
        var username = "testuser";
        var ipAddress = "192.168.1.1";

        // Record exactly 5 failures (threshold)
        for (int i = 0; i < 5; i++)
        {
            _limiter.RecordFailure(username, ipAddress);
        }

        // Act
        Action act = () => _limiter.ThrowIfBlocked(username, ipAddress);

        // Assert
        act.Should().Throw<AuthServerException>();
    }

    /// <summary>
    /// Tests that threshold minus one does not trigger block.
    /// </summary>
    [Fact]
    public void ThrowIfBlocked_ThresholdMinusOne_DoesNotThrow()
    {
        // Arrange
        var username = "testuser";
        var ipAddress = "192.168.1.1";

        // Record 4 failures (threshold - 1)
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
    /// Tests that whitespace username is treated as null.
    /// </summary>
    [Fact]
    public void RecordFailure_WhitespaceUsername_DoesNotRecord()
    {
        // Arrange
        var ipAddress = "192.168.1.1";

        // Act
        _limiter.RecordFailure(" ", ipAddress);

        // Assert
        Action act = () => _limiter.ThrowIfBlocked(" ", ipAddress);
        act.Should().NotThrow<AuthServerException>();
    }

    /// <summary>
    /// Tests that whitespace IP address is treated as null.
    /// </summary>
    [Fact]
    public void RecordFailure_WhitespaceIpAddress_DoesNotRecord()
    {
        // Arrange
        var username = "testuser";

        // Act
        _limiter.RecordFailure(username, " ");

        // Assert
        Action act = () => _limiter.ThrowIfBlocked(username, " ");
        act.Should().NotThrow<AuthServerException>();
    }

    #endregion
}
