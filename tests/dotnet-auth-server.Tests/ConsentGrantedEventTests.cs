#nullable enable

namespace DotnetAuthServer.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using DotnetAuthServer.Events;
using FluentAssertions;
using Xunit;

public sealed class ConsentGrantedEventTests
{
    [Fact]
    public void DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Arrange & Act
        var @event = new ConsentGrantedEvent();

        // Assert
        @event.EventId.Should().NotBeNullOrWhiteSpace()
            .And.HaveLength(32); // Guid without hyphens

        @event.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(1));

        @event.RequestId.Should().BeNull();

        @event.UserId.Should().BeEmpty();
        @event.ClientId.Should().BeEmpty();

        @event.GrantedScopes.Should().NotBeNull()
            .And.BeEmpty();

        @event.IsPermanent.Should().BeFalse();

        @event.ClientIpAddress.Should().BeNull();
    }

    [Fact]
    public void SettingProperties_ShouldPersistValues()
    {
        // Arrange
        var scopes = new List<string> { "openid", "profile", "email" };
        var @event = new ConsentGrantedEvent
        {
            EventId = "custom-event-id-123",
            OccurredAt = new DateTime(2023, 02, 15, 10, 30, 0, DateTimeKind.Utc),
            RequestId = "req-789",
            UserId = "user-42",
            ClientId = "client-abc",
            GrantedScopes = scopes,
            IsPermanent = true,
            ClientIpAddress = "203.0.113.5"
        };

        // Assert
        @event.EventId.Should().Be("custom-event-id-123");
        @event.OccurredAt.Should().Be(new DateTime(2023, 02, 15, 10, 30, 0, DateTimeKind.Utc));
        @event.RequestId.Should().Be("req-789");
        @event.UserId.Should().Be("user-42");
        @event.ClientId.Should().Be("client-abc");
        @event.GrantedScopes.Should().BeEquivalentTo(scopes);
        @event.IsPermanent.Should().BeTrue();
        @event.ClientIpAddress.Should().Be("203.0.113.5");
    }

    [Fact]
    public void EventId_ShouldBeUniqueAcrossInstances()
    {
        // Arrange
        var first = new ConsentGrantedEvent();
        var second = new ConsentGrantedEvent();

        // Act & Assert
        first.EventId.Should().NotBe(second.EventId);
    }

    [Fact]
    public void GrantedScopes_CanBeEmptyOrNonEmpty()
    {
        // Empty scopes
        var emptyEvent = new ConsentGrantedEvent { GrantedScopes = Enumerable.Empty<string>() };
        emptyEvent.GrantedScopes.Should().BeEmpty();

        // Non‑empty scopes
        var scopes = new[] { "read", "write" };
        var nonEmptyEvent = new ConsentGrantedEvent { GrantedScopes = scopes };
        nonEmptyEvent.GrantedScopes.Should().BeEquivalentTo(scopes);
    }

    [Fact]
    public void NullableProperties_CanBeSetToNull()
    {
        // Arrange
        var @event = new ConsentGrantedEvent
        {
            RequestId = "req-123",
            ClientIpAddress = "10.0.0.1"
        };

        // Act
        @event.RequestId = null;
        @event.ClientIpAddress = null;

        // Assert
        @event.RequestId.Should().BeNull();
        @event.ClientIpAddress.Should().BeNull();
    }
}
