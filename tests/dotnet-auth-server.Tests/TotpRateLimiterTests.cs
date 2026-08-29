using System.Text.RegularExpressions;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Exceptions;
using DotnetAuthServer.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public sealed class TotpRateLimiterTests
{
    private const int AttemptThreshold = 3;
    private const int WindowSeconds = 5;

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ThrowIfBlocked_NullOrWhitespaceUserId_DoesNotThrow(string? userId)
    {
        using var limiter = CreateLimiter();

        var exception = Record.Exception(() => limiter.ThrowIfBlocked(userId!));

        Assert.Null(exception);
    }

    [Fact]
    public void ThrowIfBlocked_BelowAttemptThreshold_DoesNotThrow()
    {
        using var limiter = CreateLimiter();
        RecordFailures(limiter, "user-1", AttemptThreshold - 1);

        var exception = Record.Exception(() => limiter.ThrowIfBlocked("user-1"));

        Assert.Null(exception);
    }

    [Fact]
    public void ThrowIfBlocked_AtAttemptThreshold_ThrowsTooManyRequestsWithPositiveRetryAfter()
    {
        using var limiter = CreateLimiter();
        RecordFailures(limiter, "user-1", AttemptThreshold);

        var exception = Assert.Throws<AuthServerException>(() => limiter.ThrowIfBlocked("user-1"));

        Assert.Equal("too_many_requests", exception.ErrorCode);
        Assert.Equal(429, exception.StatusCode);

        var retryAfterMatch = Regex.Match(exception.Message, @"in (\d+) seconds");
        Assert.True(retryAfterMatch.Success, $"Expected a retry-after value in: {exception.Message}");
        Assert.True(int.Parse(retryAfterMatch.Groups[1].Value) > 0);
    }

    [Fact]
    public void RecordSuccess_DoesNotCountTowardBlocking()
    {
        using var limiter = CreateLimiter();
        limiter.RecordSuccess("user-1");
        RecordFailures(limiter, "user-1", AttemptThreshold - 1);

        var exception = Record.Exception(() => limiter.ThrowIfBlocked("user-1"));

        Assert.Null(exception);
    }

    [Fact]
    public void Failures_ForDifferentUsers_AreTrackedIndependently()
    {
        using var limiter = CreateLimiter();
        RecordFailures(limiter, "user-1", AttemptThreshold);

        var otherUserException = Record.Exception(() => limiter.ThrowIfBlocked("user-2"));

        Assert.Null(otherUserException);
        Assert.Throws<AuthServerException>(() => limiter.ThrowIfBlocked("user-1"));
    }

    [Fact]
    public void UserIds_AreComparedCaseInsensitively()
    {
        using var limiter = CreateLimiter();
        RecordFailures(limiter, "CaseSensitiveUser", AttemptThreshold);

        var exception = Assert.Throws<AuthServerException>(
            () => limiter.ThrowIfBlocked("casesensitiveuser"));

        Assert.Equal("too_many_requests", exception.ErrorCode);
    }

    [Fact]
    public void Dispose_CanBeCalledSafely()
    {
        var limiter = CreateLimiter();

        var exception = Record.Exception(() =>
        {
            limiter.Dispose();
            limiter.Dispose();
        });

        Assert.Null(exception);
    }

    private static TotpRateLimiter CreateLimiter()
    {
        var options = new AuthServerOptions
        {
            TotpAttemptsPerWindow = AttemptThreshold,
            TotpRateLimitWindowSeconds = WindowSeconds
        };

        return new TotpRateLimiter(options, NullLogger<TotpRateLimiter>.Instance);
    }

    private static void RecordFailures(TotpRateLimiter limiter, string userId, int count)
    {
        for (var attempt = 0; attempt < count; attempt++)
        {
            limiter.RecordFailure(userId);
        }
    }
}
