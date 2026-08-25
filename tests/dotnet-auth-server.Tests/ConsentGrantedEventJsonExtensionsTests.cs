#nullable enable

using System.Text.Json;
using DotnetAuthServer.Events;
using FluentAssertions;
using Xunit;


namespace DotnetAuthServer.Tests;

/// <summary>
/// Unit tests for the ConsentGrantedEvent JSON serialization extension methods, covering compact
/// and indented output, handling of null, empty, and malformed input, default value population
/// during deserialization, and round-trip preservation of event data.
/// </summary>
public class ConsentGrantedEventJsonExtensionsTests
{
    /// <summary>
    /// A fully populated consent granted event used as the standard input for the serialization tests.
    /// </summary>
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

    /// <summary>
    /// The expected compact JSON representation of the sample event, used as input for the deserialization tests.
    /// </summary>
    private static readonly string ExpectedJson =
        "{\"eventId\":\"test-event-id\",\"occurredAt\":\"2024-01-01T12:00:00Z\",\"requestId\":\"test-request-id\",\"eventType\":\"consent_granted\",\"userId\":\"user-123\",\"clientId\":\"client-456\",\"grantedScopes\":[\"read\",\"write\",\"profile\"],\"isPermanent\":true,\"clientIpAddress\":\"192.168.1.1\"}";

    /// <summary>
    /// Verifies that serializing the fully populated sample event produces non-empty JSON containing
    /// the event id, user id, client id, each granted scope, and the "consent_granted" event type.
    /// </summary>
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

    /// <summary>
    /// Verifies that requesting indented output produces pretty-printed JSON with spaced property
    /// names such as "eventId": "test-event-id" and embedded newlines between elements.
    /// </summary>
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

    /// <summary>
    /// Verifies that requesting non-indented output produces compact JSON containing no newline characters.
    /// </summary>
    [Fact]
    public void ToJson_WithIndentedFalse_ReturnsCompactJson()
    {
        // Act
        var json = SampleEvent.ToJson(indented: false);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().NotContain("\n"); // Should not have newlines
    }

    /// <summary>
    /// Verifies that invoking ToJson through a null event reference throws an ArgumentNullException.
    /// </summary>
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

    /// <summary>
    /// Verifies that deserializing the expected JSON yields an event whose event id, occurrence time,
    /// request id, user id, client id, granted scopes, permanence flag, client IP address, and
    /// "consent_granted" event type all match the original sample values.
    /// </summary>
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

    /// <summary>
    /// Verifies that passing an empty string to FromJson throws an ArgumentException.
    /// </summary>
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

    /// <summary>
    /// Verifies that passing a null string to FromJson throws an ArgumentException.
    /// </summary>
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

    /// <summary>
    /// Verifies that malformed JSON causes FromJson to return null rather than throwing an exception.
    /// </summary>
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

    /// <summary>
    /// Verifies that deserializing minimal JSON keeps the supplied identifiers and empty scope list,
    /// while populating missing optional fields with their defaults: an occurrence time close to the
    /// current UTC time and the "consent_granted" event type.
    /// </summary>
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

    /// <summary>
    /// Verifies that TryFromJson reports success and outputs an event carrying the expected event id
    /// when given valid JSON.
    /// </summary>
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

    /// <summary>
    /// Verifies that TryFromJson throws an ArgumentException when given an empty string.
    /// </summary>
    [Fact]
    public void TryFromJson_WithEmptyJson_ThrowsArgumentException()
    {
        // Arrange
        var json = "";

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            ConsentGrantedEventJsonExtensions.TryFromJson(json, out _));
    }

    /// <summary>
    /// Verifies that TryFromJson throws an ArgumentException when given a null string.
    /// </summary>
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

    /// <summary>
    /// Verifies that TryFromJson reports failure and a null result when given malformed JSON.
    /// </summary>
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

    /// <summary>
    /// Verifies that serializing the sample event and deserializing the resulting JSON reproduces an
    /// event equivalent to the original across all runtime properties.
    /// </summary>
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

    /// <summary>
    /// Verifies that an event with an empty granted-scopes array serializes with a "grantedScopes"
    /// element and deserializes back to an empty collection.
    /// </summary>
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

    /// <summary>
    /// Verifies that null RequestId and ClientIpAddress values are omitted from the serialized JSON
    /// and remain null after deserialization.
    /// </summary>
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
