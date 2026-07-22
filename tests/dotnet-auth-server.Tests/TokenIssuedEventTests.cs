#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Tests;

using DotnetAuthServer.Events;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for TokenIssuedEvent domain event.
/// </summary>
public sealed class TokenIssuedEventTests
{
    /// <summary>
    /// Verifies that a new TokenIssuedEvent has a valid EventId (non-empty GUID).
    /// </summary>
    [Fact]
    public void Constructor_NewInstance_GeneratesValidEventId()
    {
        // Arrange & Act
        var tokenEvent = new TokenIssuedEvent();

        // Assert
        tokenEvent.EventId.Should().NotBeNullOrEmpty();
        tokenEvent.EventId.Should().MatchRegex("^[a-f0-9]{32}$", "EventId should be a 32-character hex string");
    }

    /// <summary>
    /// Verifies that OccurredAt is set to current UTC time when event is created.
    /// </summary>
    [Fact]
    public void Constructor_NewInstance_SetsOccurredAtToUtcNow()
    {
        // Arrange
        var beforeCreation = DateTime.UtcNow;

        // Act
        var tokenEvent = new TokenIssuedEvent();
        var afterCreation = DateTime.UtcNow;

        // Assert
        tokenEvent.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        tokenEvent.OccurredAt.Should().BeOnOrAfter(beforeCreation);
        tokenEvent.OccurredAt.Should().BeOnOrBefore(afterCreation);
    }

    /// <summary>
    /// Verifies that EventType is always "token_issued".
    /// </summary>
    [Fact]
    public void EventType_Always_ReturnsTokenIssued()
    {
        // Arrange & Act
        var tokenEvent = new TokenIssuedEvent();

        // Assert
        tokenEvent.EventType.Should().Be("token_issued");
    }

    /// <summary>
    /// Happy path: Verifies all properties are correctly set for a typical token issuance.
    /// </summary>
    [Fact]
    public void Constructor_TypicalTokenIssuance_SetsAllPropertiesCorrectly()
    {
        // Arrange
        var expectedRequestId = "req-12345";
        var expectedUserId = "user-abc-123";
        var expectedClientId = "client-xyz-789";
        var expectedGrantType = "authorization_code";
        var expectedScopes = new List<string> { "openid", "profile", "email" };
        var expectedExpiresInSeconds = 3600;
        var expectedClientIpAddress = "192.168.1.100";

        // Act
        var tokenEvent = new TokenIssuedEvent
        {
            RequestId = expectedRequestId,
            UserId = expectedUserId,
            ClientId = expectedClientId,
            GrantType = expectedGrantType,
            Scopes = expectedScopes,
            ExpiresInSeconds = expectedExpiresInSeconds,
            ClientIpAddress = expectedClientIpAddress
        };

        // Assert
        tokenEvent.RequestId.Should().Be(expectedRequestId);
        tokenEvent.UserId.Should().Be(expectedUserId);
        tokenEvent.ClientId.Should().Be(expectedClientId);
        tokenEvent.GrantType.Should().Be(expectedGrantType);
        tokenEvent.Scopes.Should().BeEquivalentTo(expectedScopes);
        tokenEvent.ExpiresInSeconds.Should().Be(expectedExpiresInSeconds);
        tokenEvent.ClientIpAddress.Should().Be(expectedClientIpAddress);
    }

    /// <summary>
    /// Edge case: Empty scopes collection should not be null.
    /// </summary>
    [Fact]
    public void Scopes_EmptyCollection_ShouldNotBeNull()
    {
        // Arrange & Act
        var tokenEvent = new TokenIssuedEvent
        {
            Scopes = Enumerable.Empty<string>()
        };

        // Assert
        tokenEvent.Scopes.Should().NotBeNull();
        tokenEvent.Scopes.Should().BeEmpty();
    }

    /// <summary>
    /// Edge case: Empty UserId should be allowed (for client credentials flow).
    /// </summary>
    [Fact]
    public void UserId_EmptyString_ShouldBeAllowed()
    {
        // Arrange & Act
        var tokenEvent = new TokenIssuedEvent
        {
            UserId = string.Empty
        };

        // Assert
        tokenEvent.UserId.Should().BeEmpty();
    }

    /// <summary>
    /// Edge case: Empty ClientId should be allowed (for anonymous clients).
    /// </summary>
    [Fact]
    public void ClientId_EmptyString_ShouldBeAllowed()
    {
        // Arrange & Act
        var tokenEvent = new TokenIssuedEvent
        {
            ClientId = string.Empty
        };

        // Assert
        tokenEvent.ClientId.Should().BeEmpty();
    }

    /// <summary>
    /// Edge case: Empty GrantType should be allowed.
    /// </summary>
    [Fact]
    public void GrantType_EmptyString_ShouldBeAllowed()
    {
        // Arrange & Act
        var tokenEvent = new TokenIssuedEvent
        {
            GrantType = string.Empty
        };

        // Assert
        tokenEvent.GrantType.Should().BeEmpty();
    }

    /// <summary>
    /// Edge case: Zero ExpiresInSeconds should be allowed (for non-expiring tokens).
    /// </summary>
    [Fact]
    public void ExpiresInSeconds_Zero_ShouldBeAllowed()
    {
        // Arrange & Act
        var tokenEvent = new TokenIssuedEvent
        {
            ExpiresInSeconds = 0
        };

        // Assert
        tokenEvent.ExpiresInSeconds.Should().Be(0);
    }

    /// <summary>
    /// Edge case: Negative ExpiresInSeconds should be allowed (though unusual).
    /// </summary>
    [Fact]
    public void ExpiresInSeconds_Negative_ShouldBeAllowed()
    {
        // Arrange & Act
        var tokenEvent = new TokenIssuedEvent
        {
            ExpiresInSeconds = -1
        };

        // Assert
        tokenEvent.ExpiresInSeconds.Should().Be(-1);
    }

    /// <summary>
    /// Happy path: Null RequestId should be allowed.
    /// </summary>
    [Fact]
    public void RequestId_Null_ShouldBeAllowed()
    {
        // Arrange & Act
        var tokenEvent = new TokenIssuedEvent
        {
            RequestId = null
        };

        // Assert
        tokenEvent.RequestId.Should().BeNull();
    }

    /// <summary>
    /// Happy path: Null ClientIpAddress should be allowed.
    /// </summary>
    [Fact]
    public void ClientIpAddress_Null_ShouldBeAllowed()
    {
        // Arrange & Act
        var tokenEvent = new TokenIssuedEvent
        {
            ClientIpAddress = null
        };

        // Assert
        tokenEvent.ClientIpAddress.Should().BeNull();
    }

    /// <summary>
    /// Verifies that Scopes can be modified after event creation.
    /// </summary>
    [Fact]
    public void Scopes_CanBeModified_AfterCreation()
    {
        // Arrange
        var tokenEvent = new TokenIssuedEvent();

        // Act
        var originalScopes = tokenEvent.Scopes.ToList();
        tokenEvent.Scopes = new List<string> { "read", "write" };

        // Assert
        tokenEvent.Scopes.Should().BeEquivalentTo(new[] { "read", "write" });
        originalScopes.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that all properties can be modified after event creation.
    /// </summary>
    [Fact]
    public void Properties_CanBeModified_AfterCreation()
    {
        // Arrange
        var tokenEvent = new TokenIssuedEvent();

        // Act
        tokenEvent.EventId = "custom-event-id";
        tokenEvent.OccurredAt = DateTime.UtcNow.AddHours(-1);
        tokenEvent.RequestId = "custom-req-999";
        tokenEvent.UserId = "custom-user";
        tokenEvent.ClientId = "custom-client";
        tokenEvent.GrantType = "custom_grant";
        tokenEvent.Scopes = new[] { "custom", "scope" };
        tokenEvent.ExpiresInSeconds = 7200;
        tokenEvent.ClientIpAddress = "10.0.0.1";

        // Assert
        tokenEvent.EventId.Should().Be("custom-event-id");
        tokenEvent.OccurredAt.Should().BeCloseTo(DateTime.UtcNow.AddHours(-1), TimeSpan.FromSeconds(1));
        tokenEvent.RequestId.Should().Be("custom-req-999");
        tokenEvent.UserId.Should().Be("custom-user");
        tokenEvent.ClientId.Should().Be("custom-client");
        tokenEvent.GrantType.Should().Be("custom_grant");
        tokenEvent.Scopes.Should().BeEquivalentTo(new[] { "custom", "scope" });
        tokenEvent.ExpiresInSeconds.Should().Be(7200);
        tokenEvent.ClientIpAddress.Should().Be("10.0.0.1");
    }

    /// <summary>
    /// Verifies that EventId is auto-generated as GUID when not set.
    /// </summary>
    [Fact]
    public void EventId_AutoGenerated_WhenNotSet()
    {
        // Arrange
        var tokenEvent = new TokenIssuedEvent();

        // Act - don't set EventId, let it auto-generate
        var firstEventId = tokenEvent.EventId;

        // Assert
        firstEventId.Should().NotBeNullOrEmpty();
        firstEventId.Should().MatchRegex("^[a-f0-9]{32}$");
    }

    /// <summary>
    /// Verifies that multiple events have different EventIds.
    /// </summary>
    [Fact]
    public void EventId_MultipleEvents_HaveDifferentIds()
    {
        // Arrange & Act
        var event1 = new TokenIssuedEvent();
        var event2 = new TokenIssuedEvent();

        // Assert
        event1.EventId.Should().NotBe(event2.EventId);
    }

    /// <summary>
    /// Verifies that OccurredAt is set to current UTC time when explicitly set.
    /// </summary>
    [Fact]
    public void OccurredAt_SetToSpecificTime_StaysAsSet()
    {
        // Arrange
        var specificTime = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        // Act
        var tokenEvent = new TokenIssuedEvent
        {
            OccurredAt = specificTime
        };

        // Assert
        tokenEvent.OccurredAt.Should().Be(specificTime);
    }

    /// <summary>
    /// Happy path: Full token issuance with all properties set.
    /// </summary>
    [Fact]
    public void FullTokenIssuance_AllPropertiesSet_CorrectlyStored()
    {
        // Arrange
        var fixedDate = new DateTime(2024, 6, 15, 10, 30, 45, DateTimeKind.Utc);
        var fixedEventId = "a1b2c3d4e5f67890123456789012345";

        // Act
        var tokenEvent = new TokenIssuedEvent
        {
            EventId = fixedEventId,
            OccurredAt = fixedDate,
            RequestId = "req-abc-123",
            UserId = "user-xyz-789",
            ClientId = "client-def-456",
            GrantType = "refresh_token",
            Scopes = new[] { "openid", "profile", "email", "offline_access" },
            ExpiresInSeconds = 7200,
            ClientIpAddress = "203.0.113.42"
        };

        // Assert
        tokenEvent.EventId.Should().Be(fixedEventId);
        tokenEvent.OccurredAt.Should().Be(fixedDate);
        tokenEvent.RequestId.Should().Be("req-abc-123");
        tokenEvent.UserId.Should().Be("user-xyz-789");
        tokenEvent.ClientId.Should().Be("client-def-456");
        tokenEvent.GrantType.Should().Be("refresh_token");
        tokenEvent.Scopes.Should().BeEquivalentTo(new[] { "openid", "profile", "email", "offline_access" });
        tokenEvent.ExpiresInSeconds.Should().Be(7200);
        tokenEvent.ClientIpAddress.Should().Be("203.0.113.42");
    }
}
