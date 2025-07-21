using Moq;
using FluentAssertions;
using DotnetAuthServer.Services;
using DotnetAuthServer.Middleware;
using Microsoft.Extensions.Logging;
using Xunit;

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
        // Act
        _service.LogTokenIssuance("user1", "client1", "authorization_code", "openid profile");

        // Assert
        var entries = _service.GetRecentEntries().ToList();
        entries.Should().HaveCount(1);
        entries[0].EventType.Should().Be("TOKEN_ISSUED");
        entries[0].UserId.Should().Be("user1");
        entries[0].ClientId.Should().Be("client1");
        entries[0].Details["grant_type"].Should().Be("authorization_code");
    }

    /// <summary>
    /// Verifies that logging an authentication adds an entry to the log.
    /// </summary>
    [Fact]
    public void LogAuthentication_AddsEntryToLog()
    {
        // Act
        _service.LogAuthentication("user1", "testuser", success: true);

        // Assert
        var entries = _service.GetRecentEntries().ToList();
        entries.Should().HaveCount(1);
        entries[0].EventType.Should().Be("AUTH_SUCCESS");
    }

    /// <summary>
    /// Verifies that getting recent entries limits the results.
    /// </summary>
    [Fact]
    public void GetRecentEntries_LimitsResults()
    {
        // Arrange
        for (int i = 0; i < 150; i++)
        {
            _service.LogAuthentication($"user{i}", "testuser");
        }

        // Act
        var entries = _service.GetRecentEntries(10).ToList();

        // Assert
        entries.Should().HaveCount(10);
    }

    /// <summary>
    /// Verifies that clearing the log removes all entries.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllEntries()
    {
        // Arrange
        _service.LogAuthentication("user1", "testuser");
        _service.GetRecentEntries().Should().HaveCount(1);

        // Act
        _service.Clear();

        // Assert
        _service.GetRecentEntries().Should().BeEmpty();
    }
}
