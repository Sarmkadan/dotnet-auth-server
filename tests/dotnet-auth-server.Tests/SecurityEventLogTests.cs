#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Tests;

using DotnetAuthServer.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

public sealed class SecurityEventLogTests
{
    [Fact]
    public void RefreshTokenReuseDetected_Called_LogsWarningWithEventIdAndFamilyId()
    {
        // Arrange
        var logger = new CapturingLogger();

        // Act
        SecurityEventLog.RefreshTokenReuseDetected(logger, "tok-1", "user-1", "client-1", "family-1");

        // Assert
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(4001, entry.EventId.Id);
        Assert.Contains("user-1", entry.Message);
        Assert.Contains("family-1", entry.Message);
    }

    [Fact]
    public void RevokedRefreshTokenPresented_Called_LogsWarningWithRevocationReason()
    {
        // Arrange
        var logger = new CapturingLogger();

        // Act
        SecurityEventLog.RevokedRefreshTokenPresented(logger, "tok-2", "user-2", "client-2", "family-2", "logout");

        // Assert
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(4002, entry.EventId.Id);
        Assert.Contains("family-2", entry.Message);
        Assert.Contains("logout", entry.Message);
    }

    [Fact]
    public void AccountLockedOut_Called_LogsWarningWithUserIdAndAttemptCount()
    {
        // Arrange
        var logger = new CapturingLogger();
        var lockedUntil = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        SecurityEventLog.AccountLockedOut(logger, "user-3", lockedUntil, 5);

        // Assert
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(4101, entry.EventId.Id);
        Assert.Contains("user-3", entry.Message);
        Assert.Contains("5 failed login attempts", entry.Message);
    }

    [Fact]
    public void LoginRejectedForLockedAccount_Called_LogsWarningWithEventId4102()
    {
        // Arrange
        var logger = new CapturingLogger();
        var lockedUntil = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        SecurityEventLog.LoginRejectedForLockedAccount(logger, "user-4", lockedUntil);

        // Assert
        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Warning, entry.Level);
        Assert.Equal(4102, entry.EventId.Id);
        Assert.Contains("user-4", entry.Message);
    }

    private sealed record LogEntry(LogLevel Level, EventId EventId, string Message);

    private sealed class CapturingLogger : ILogger
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, eventId, formatter(state, exception)));
        }
    }
}
