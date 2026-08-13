using Moq;
using FluentAssertions;
using DotnetAuthServer.Services;
using DotnetAuthServer.Middleware;
using Microsoft.Extensions.Logging;
using Xunit;
using System;
using System.Collections.Generic;
using System.Threading;

/// <summary>
/// Tests for the AuditLoggingService class.
/// </summary>
public sealed class AuditLoggingServiceTests
{
    private readonly Mock<ILogger<AuditLoggingService>> _loggerMock;
    private readonly AuditLoggingService _service;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLoggingServiceTests"/> class.
    /// </summary>
    public AuditLoggingServiceTests()
    {
        _loggerMock = new Mock<ILogger<AuditLoggingService>>();
        _service = new AuditLoggingService(_loggerMock.Object);
        // Clear potential state from previous tests if any
        _service.Clear();
        LogicalContext.RequestId = "test-request-id";
    }

    /// <summary>
    /// Verifies that logging a token issuance adds an entry to the log.
    /// </summary>
    [Fact]
    public void LogTokenIssuance_AddsEntryToLog()
    {
        _loggerMock.Object.LogInformation("Entering {Method}", nameof(LogTokenIssuance_AddsEntryToLog));

        // Act
        _service.LogTokenIssuance("user1", "client1", "authorization_code", "openid profile");

        // Assert
        var entries = _service.GetRecentEntries().ToList();
        entries.Should().HaveCount(1);
        entries[0].EventType.Should().Be("TOKEN_ISSUED");
        entries[0].UserId.Should().Be("user1");
        entries[0].ClientId.Should().Be("client1");
        entries[0].Details["grant_type"].Should().Be("authorization_code");

        _loggerMock.Object.LogInformation("Exiting {Method}", nameof(LogTokenIssuance_AddsEntryToLog));
    }

    /// <summary>
    /// Verifies that logging an authentication adds an entry to the log.
    /// </summary>
    [Fact]
    public void LogAuthentication_AddsEntryToLog()
    {
        _loggerMock.Object.LogInformation("Entering {Method}", nameof(LogAuthentication_AddsEntryToLog));

        // Act
        _service.LogAuthentication("user1", "testuser", success: true);

        // Assert
        var entries = _service.GetRecentEntries().ToList();
        entries.Should().HaveCount(1);
        entries[0].EventType.Should().Be("AUTH_SUCCESS");

        _loggerMock.Object.LogInformation("Exiting {Method}", nameof(LogAuthentication_AddsEntryToLog));
    }

    /// <summary>
    /// Verifies that getting recent entries limits the results.
    /// </summary>
    [Fact]
    public void GetRecentEntries_LimitsResults()
    {
        _loggerMock.Object.LogInformation("Entering {Method}", nameof(GetRecentEntries_LimitsResults));

        // Arrange
        for (int i = 0; i < 150; i++)
        {
            _service.LogAuthentication($"user{i}", "testuser");
        }

        // Act
        var entries = _service.GetRecentEntries(10).ToList();

        // Assert
        entries.Should().HaveCount(10);

        _loggerMock.Object.LogInformation("Exiting {Method}", nameof(GetRecentEntries_LimitsResults));
    }

    /// <summary>
    /// Verifies that clearing the log removes all entries.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllEntries()
    {
        _loggerMock.Object.LogInformation("Entering {Method}", nameof(Clear_RemovesAllEntries));

        // Arrange
        _service.LogAuthentication("user1", "testuser");
        _service.GetRecentEntries().Should().HaveCount(1);

        // Act
        _service.Clear();

        // Assert
        _service.GetRecentEntries().Should().BeEmpty();

        _loggerMock.Object.LogInformation("Exiting {Method}", nameof(Clear_RemovesAllEntries));
    }

    /// <summary>
    /// Verifies that CSV export includes all expected columns.
    /// </summary>
    [Fact]
    public void ExportToCsv_ContainsExpectedColumns()
    {
        _loggerMock.Object.LogInformation("Entering {Method}", nameof(ExportToCsv_ContainsExpectedColumns));

        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow.AddHours(1);
        _service.LogTokenIssuance("user1", "client1", "authorization_code", "openid profile");
        _service.LogAuthentication("user2", "testuser");

        // Act
        var csv = _service.ExportToCsv(startTime, endTime);
        var lines = csv.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // Assert
        lines.Should().HaveCount(3); // Header + 2 entries
        lines[0].Should().Be("Timestamp,EventType,UserId,ClientId,RequestId,Severity,Details");

        _loggerMock.Object.LogInformation("Exiting {Method}", nameof(ExportToCsv_ContainsExpectedColumns));
    }

    /// <summary>
    /// Verifies that CSV export properly escapes values containing commas and quotes.
    /// </summary>
    [Fact]
    public void ExportToCsv_EscapesSpecialCharacters()
    {
        _loggerMock.Object.LogInformation("Entering {Method}", nameof(ExportToCsv_EscapesSpecialCharacters));

        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow.AddHours(1);

        // Log entry with special characters that need escaping
        _service.LogAdministrativeAction(
            "update settings",
            targetClientId: "client,with,commas",
            targetUserId: "user\"with\"quotes",
            changes: new Dictionary<string, string> { { "key", "value,with,commas" } }
        );

        // Act
        var csv = _service.ExportToCsv(startTime, endTime);
        var lines = csv.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // Assert
        lines.Should().HaveCount(2); // Header + 1 entry

        // Verify the entry line contains properly escaped values
        var entryLine = lines[1];
        entryLine.Should().Contain("ADMIN_ACTION");
        entryLine.Should().Contain("\"user\"\"with\"\"quotes\"");
        entryLine.Should().Contain("\"client,with,commas\"");

        _loggerMock.Object.LogInformation("Exiting {Method}", nameof(ExportToCsv_EscapesSpecialCharacters));
    }

    /// <summary>
    /// Verifies that CSV export includes details as JSON.
    /// </summary>
    [Fact]
    public void ExportToCsv_IncludesDetailsAsJson()
    {
        _loggerMock.Object.LogInformation("Entering {Method}", nameof(ExportToCsv_IncludesDetailsAsJson));

        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow.AddHours(1);
        _service.LogTokenIssuance("user1", "client1", "authorization_code", "openid profile");

        // Act
        var csv = _service.ExportToCsv(startTime, endTime);
        var lines = csv.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // Assert - check that the line contains the expected event type and user
        lines[1].Should().Contain("TOKEN_ISSUED");
        lines[1].Should().Contain("user1");
        lines[1].Should().Contain("client1");

        _loggerMock.Object.LogInformation("Exiting {Method}", nameof(ExportToCsv_IncludesDetailsAsJson));
    }

    /// <summary>
    /// Verifies that CSV export respects date range filtering.
    /// </summary>
    [Fact]
    public void ExportToCsv_RespectsDateRangeFilter()
    {
        _loggerMock.Object.LogInformation("Entering {Method}", nameof(ExportToCsv_RespectsDateRangeFilter));

        // Arrange
        var startTime = DateTime.UtcNow.AddMinutes(-30);
        var endTime = DateTime.UtcNow.AddMinutes(-10);

        // Log entries at different times
        _service.LogAuthentication("user1", "testuser");
        Thread.Sleep(10); // Ensure different timestamps
        _service.LogAuthentication("user2", "testuser");
        Thread.Sleep(10);
        _service.LogAuthentication("user3", "testuser");

        // Act - export only entries within range
        var csv = _service.ExportToCsv(startTime, endTime);
        var lines = csv.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // Assert - should have header only since all entries are outside the range
        lines.Should().HaveCount(1); // Only header

        _loggerMock.Object.LogInformation("Exiting {Method}", nameof(ExportToCsv_RespectsDateRangeFilter));
    }

    /// <summary>
    /// Verifies that CSV export respects maxCount parameter.
    /// </summary>
    [Fact]
    public void ExportToCsv_RespectsMaxCount()
    {
        _loggerMock.Object.LogInformation("Entering {Method}", nameof(ExportToCsv_RespectsMaxCount));

        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow.AddHours(1);

        // Create many entries
        for (int i = 0; i < 50; i++)
        {
            _service.LogAuthentication($"user{i}", "testuser");
        }

        // Act - export with maxCount = 10
        var csv = _service.ExportToCsv(startTime, endTime, 10);
        var lines = csv.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // Assert
        lines.Should().HaveCount(11); // Header + 10 entries

        _loggerMock.Object.LogInformation("Exiting {Method}", nameof(ExportToCsv_RespectsMaxCount));
    }

    /// <summary>
    /// Verifies that CSV export orders entries by timestamp (newest first).
    /// </summary>
    [Fact]
    public void ExportToCsv_OrdersByTimestampDescending()
    {
        _loggerMock.Object.LogInformation("Entering {Method}", nameof(ExportToCsv_OrdersByTimestampDescending));

        // Arrange
        var startTime = DateTime.UtcNow.AddHours(-1);
        var endTime = DateTime.UtcNow.AddHours(1);

        // Create entries in order
        _service.LogAuthentication("user1", "testuser");
        Thread.Sleep(10);
        _service.LogAuthentication("user2", "testuser");
        Thread.Sleep(10);
        _service.LogAuthentication("user3", "testuser");

        // Act
        var csv = _service.ExportToCsv(startTime, endTime);
        var lines = csv.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // Assert - newest entries should appear first
        var firstEntry = lines[1];
        var secondEntry = lines[2];
        var thirdEntry = lines[3];

        firstEntry.Should().Contain("user3");
        secondEntry.Should().Contain("user2");
        thirdEntry.Should().Contain("user1");

        _loggerMock.Object.LogInformation("Exiting {Method}", nameof(ExportToCsv_OrdersByTimestampDescending));
    }

    /// <summary>
    /// Verifies that static ExportToCsv method works with arbitrary collections.
    /// </summary>
    [Fact]
    public void ExportToCsv_StaticMethodWorksWithAnyCollection()
    {
        _loggerMock.Object.LogInformation("Entering {Method}", nameof(ExportToCsv_StaticMethodWorksWithAnyCollection));

        // Arrange
        var entries = new List<AuditLogEntry>
        {
            new AuditLogEntry
            {
                EventType = "TEST_EVENT",
                UserId = "user1",
                ClientId = "client1",
                Timestamp = DateTime.UtcNow.AddMinutes(-5),
                RequestId = "req1",
                Severity = AuditSeverity.Information,
                Details = new Dictionary<string, string> { { "key", "value" } }
            }
        };

        // Act
        var csv = AuditLoggingService.ExportToCsv(entries);
        var lines = csv.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        // Assert
        lines.Should().HaveCount(2); // Header + 1 entry
        lines[0].Should().Be("Timestamp,EventType,UserId,ClientId,RequestId,Severity,Details");
        lines[1].Should().Contain("TEST_EVENT");
        lines[1].Should().Contain("user1");

        _loggerMock.Object.LogInformation("Exiting {Method}", nameof(ExportToCsv_StaticMethodWorksWithAnyCollection));
    }
}
