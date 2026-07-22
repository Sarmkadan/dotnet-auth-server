#nullable enable

namespace DotnetAuthServer.Tests;

using System;
using DotnetAuthServer.Events;
using FluentAssertions;
using Xunit;

public sealed class UserAuthenticatedEventTests
{
    [Fact]
    public void DefaultValues_ShouldBeInitializedCorrectly()
    {
        // Arrange & Act
        var @event = new UserAuthenticatedEvent();

        // Assert
        @event.EventId.Should().NotBeNullOrWhiteSpace()
            .And.HaveLength(32); // Guid without hyphens

        @event.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(1));

        @event.RequestId.Should().BeNull();

        @event.UserId.Should().BeEmpty();
        @event.Username.Should().BeEmpty();
        @event.ClientId.Should().BeEmpty();

        @event.ClientIpAddress.Should().BeNull();

        @event.AuthenticationMethod.Should().Be("password");

        @event.Email.Should().BeNull();
    }

    [Fact]
    public void SettingProperties_ShouldPersistValues()
    {
        // Arrange
        var @event = new UserAuthenticatedEvent
        {
            EventId = "custom-id-123",
            OccurredAt = new DateTime(2023, 01, 01, 12, 0, 0, DateTimeKind.Utc),
            RequestId = "req-456",
            UserId = "user-1",
            Username = "jdoe",
            ClientId = "client-abc",
            ClientIpAddress = "192.168.0.1",
            AuthenticationMethod = "saml",
            Email = "jdoe@example.com"
        };

        // Assert
        @event.EventId.Should().Be("custom-id-123");
        @event.OccurredAt.Should().Be(new DateTime(2023, 01, 01, 12, 0, 0, DateTimeKind.Utc));
        @event.RequestId.Should().Be("req-456");
        @event.UserId.Should().Be("user-1");
        @event.Username.Should().Be("jdoe");
        @event.ClientId.Should().Be("client-abc");
        @event.ClientIpAddress.Should().Be("192.168.0.1");
        @event.AuthenticationMethod.Should().Be("saml");
        @event.Email.Should().Be("jdoe@example.com");
    }

    [Fact]
    public void EventId_ShouldBeUniqueAcrossInstances()
    {
        // Arrange
        var first = new UserAuthenticatedEvent();
        var second = new UserAuthenticatedEvent();

        // Act & Assert
        first.EventId.Should().NotBe(second.EventId);
    }

    [Fact]
    public void OccurredAt_ShouldBeCloseToNow_OnCreation()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var @event = new UserAuthenticatedEvent();

        var after = DateTime.UtcNow;

        // Assert
        @event.OccurredAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void AuthenticationMethod_DefaultIsPassword_AndCanBeOverridden()
    {
        // Default
        var @event = new UserAuthenticatedEvent();
        @event.AuthenticationMethod.Should().Be("password");

        // Override
        @event.AuthenticationMethod = "oauth";
        @event.AuthenticationMethod.Should().Be("oauth");
    }

    [Fact]
    public void NullableProperties_CanBeSetToNullAndBack()
    {
        // Arrange
        var @event = new UserAuthenticatedEvent
        {
            RequestId = "req-123",
            ClientIpAddress = "10.0.0.1",
            Email = "user@example.com"
        };

        // Act
        @event.RequestId = null;
        @event.ClientIpAddress = null;
        @event.Email = null;

        // Assert
        @event.RequestId.Should().BeNull();
        @event.ClientIpAddress.Should().BeNull();
        @event.Email.Should().BeNull();
    }
}
