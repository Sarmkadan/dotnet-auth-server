#nullable enable

using System.Text.Json;
using DotnetAuthServer.Events;
using FluentAssertions;
using Xunit;


namespace DotnetAuthServer.Tests;

public class ConsentGrantedEventJsonExtensionsTests
{
    private static readonly ConsentGrantedEvent SampleEvent = new()
    {
        EventId = "test-event-id",
        OccurredAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
        RequestId = "test-request-id",
        UserId = "user-123",
        ClientId = "client-456",
        GrantedScopes = new[] { "read", "write", "profile" },
        IsPermanent = true,
        ClientIpAddress = "192.168.1.1"
    };

    private static readonly string ExpectedJson =
        "{\"eventId\":\"test-event-id\",\"occurredAt\":\"2024-01-01T12:00:00Z\",\"requestId\":\"test-request-id\",\"eventType\":\"consent_granted\",\"userId\":\"user-123\",\"clientId\":\"client-456\",\"grantedScopes\":[\"read\",\"write\",\"profile\"],\"isPermanent\":true,\"clientIpAddress\":\"192.168.1.1\"}";

    [Fact]
    public void ToJson_WithValidEvent_ReturnsValidJson()
    {
        // Act
        var json = SampleEvent.ToJson();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("test-event-id");
        json.Should().Contain("user-123");
        json.Should().Contain("client-456");
        json.Should().Contain("read");
        json.Should().Contain("write");
        json.Should().Contain("profile");
        json.Should().Contain("consent_granted");
    }

    [Fact]
    public void ToJson_WithIndentedTrue_ReturnsPrettyPrintedJson()
    {
        // Act
        var json = SampleEvent.ToJson(indented: true);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("{");
        json.Should().Contain("\"eventId\": \"test-event-id\"");
        json.Should().Contain("\n"); // Should have newlines for pretty printing
    }

    [Fact]
    public void ToJson_WithIndentedFalse_ReturnsCompactJson()
    {
        // Act
        var json = SampleEvent.ToJson(indented: false);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().NotContain("\n"); // Should not have newlines
    }

    [Fact]
    public void ToJson_WithNullEvent_ThrowsArgumentNullException()
    {
        // Arrange
        ConsentGrantedEvent? nullEvent = null;

        // Act
        Action act = () => nullEvent!.ToJson();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromJson_WithValidJson_ReturnsDeserializedEvent()
    {
        // Arrange
        var json = ExpectedJson;

        // Act
        var result = ConsentGrantedEventJsonExtensions.FromJson(json);

        // Assert
        result.Should().NotBeNull();
        result!.EventId.Should().Be("test-event-id");
        result.OccurredAt.Should().Be(new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc));
        result.RequestId.Should().Be("test-request-id");
        result.UserId.Should().Be("user-123");
        result.ClientId.Should().Be("client-456");
        result.GrantedScopes.Should().BeEquivalentTo(new[] { "read", "write", "profile" });
        result.IsPermanent.Should().BeTrue();
        result.ClientIpAddress.Should().Be("192.168.1.1");
        result.EventType.Should().Be("consent_granted");
    }

    [Fact]
    public void FromJson_WithEmptyJson_ThrowsArgumentException()
    {
        // Arrange
        var json = string.Empty;

        // Act
        Action act = () => ConsentGrantedEventJsonExtensions.FromJson(json);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromJson_WithNullJson_ThrowsArgumentException()
    {
        // Arrange
        string? nullJson = null;

        // Act
        Action act = () => ConsentGrantedEventJsonExtensions.FromJson(nullJson!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FromJson_WithInvalidJson_ReturnsNull()
    {
        // Arrange
        var invalidJson = "{ invalid json";

        // Act
        var result = ConsentGrantedEventJsonExtensions.FromJson(invalidJson);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void FromJson_WithMinimalValidJson_ReturnsEventWithDefaults()
    {
        // Arrange
        var minimalJson = "{\"eventId\":\"min-id\",\"userId\":\"user-789\",\"clientId\":\"client-abc\",\"grantedScopes\":[],\"isPermanent\":false}";

        // Act
        var result = ConsentGrantedEventJsonExtensions.FromJson(minimalJson);

        // Assert
        result.Should().NotBeNull();
        result!.EventId.Should().Be("min-id");
        result.UserId.Should().Be("user-789");
        result.ClientId.Should().Be("client-abc");
        result.GrantedScopes.Should().BeEmpty();
        result.IsPermanent.Should().BeFalse();
        result.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        result.EventType.Should().Be("consent_granted");
    }

    [Fact]
    public void TryFromJson_WithValidJson_ReturnsTrueAndDeserializedEvent()
    {
        // Arrange
        var json = ExpectedJson;

        // Act
        var success = ConsentGrantedEventJsonExtensions.TryFromJson(json, out var result);

        // Assert
        success.Should().BeTrue();
        result.Should().NotBeNull();
        result!.EventId.Should().Be("test-event-id");
    }

    [Fact]
    public void TryFromJson_WithEmptyJson_ThrowsArgumentException()
    {
        // Arrange
        var json = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ConsentGrantedEventJsonExtensions.TryFromJson(json, out _));
    }

    [Fact]
    public void TryFromJson_WithNullJson_ThrowsArgumentException()
    {
        // Arrange
        string? nullJson = null;

        // Act
        Action act = () => ConsentGrantedEventJsonExtensions.TryFromJson(nullJson!, out _);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void TryFromJson_WithInvalidJson_ReturnsFalseAndNull()
    {
        // Arrange
        var invalidJson = "{ invalid json";

        // Act
        var success = ConsentGrantedEventJsonExtensions.TryFromJson(invalidJson, out var result);

        // Assert
        success.Should().BeFalse();
        result.Should().BeNull();
    }

    [Fact]
    public void RoundTripSerialization_PreservesAllData()
    {
        // Arrange
        var originalEvent = SampleEvent;

        // Act
        var json = originalEvent.ToJson();
        var deserializedEvent = ConsentGrantedEventJsonExtensions.FromJson(json);

        // Assert
        deserializedEvent.Should().NotBeNull();
        deserializedEvent.Should().BeEquivalentTo(originalEvent, options =>
            options.IncludingAllRuntimeProperties());
    }

    [Fact]
    public void EmptyScopesSerialization_WorksCorrectly()
    {
        // Arrange
        var eventWithEmptyScopes = new ConsentGrantedEvent
        {
            EventId = "empty-scopes-id",
            UserId = "user-empty",
            ClientId = "client-empty",
            GrantedScopes = Array.Empty<string>(),
            IsPermanent = false
        };

        // Act
        var json = eventWithEmptyScopes.ToJson();
        var deserialized = ConsentGrantedEventJsonExtensions.FromJson(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.GrantedScopes.Should().BeEmpty();
        json.Should().Contain("grantedScopes");
    }

    [Fact]
    public void NullOptionalFieldsSerialization_WorksCorrectly()
    {
        // Arrange
        var eventWithNullFields = new ConsentGrantedEvent
        {
            EventId = "null-fields-id",
            UserId = "user-null",
            ClientId = "client-null",
            GrantedScopes = new[] { "scope1" },
            IsPermanent = true
            // RequestId and ClientIpAddress are null
        };

        // Act
        var json = eventWithNullFields.ToJson();
        var deserialized = ConsentGrantedEventJsonExtensions.FromJson(json);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.RequestId.Should().BeNull();
        deserialized.ClientIpAddress.Should().BeNull();
        json.Should().NotContain("requestId");
        json.Should().NotContain("clientIpAddress");
    }
}