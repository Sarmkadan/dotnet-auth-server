#nullable enable
using System;
using DotnetAuthServer.Domain.Entities;
using Xunit;

namespace DotnetAuthServer.Tests;

public class UserSessionJsonExtensionsTests
{
    [Fact]
    public void ToJson_WithValidSession_ReturnsNonEmptyJson()
    {
        // Arrange
        var session = new UserSession();

        // Act
        var json = session.ToJson();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(json));
        // round‑trip check
        var roundTrip = UserSessionJsonExtensions.FromJson(json);
        Assert.NotNull(roundTrip);
    }

    [Fact]
    public void ToJson_WithIndentation_ProducesMultilineJson()
    {
        // Arrange
        var session = new UserSession();

        // Act
        var json = session.ToJson(indented: true);

        // Assert
        // When WriteIndented is true the serializer inserts line‑breaks.
        Assert.Contains(Environment.NewLine, json);
    }

    [Fact]
    public void ToJson_NullSession_ThrowsArgumentNullException()
    {
        // Arrange
        UserSession? session = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => session!.ToJson());
    }

    [Fact]
    public void FromJson_NullOrWhiteSpace_ReturnsNull()
    {
        Assert.Null(UserSessionJsonExtensions.FromJson(null));
        Assert.Null(UserSessionJsonExtensions.FromJson(string.Empty));
        Assert.Null(UserSessionJsonExtensions.FromJson("   "));
    }

    [Fact]
    public void FromJson_ValidJson_ReturnsDeserializedObject()
    {
        // Arrange
        var original = new UserSession();
        var json = original.ToJson();

        // Act
        var deserialized = UserSessionJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(deserialized);
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndValue()
    {
        // Arrange
        var original = new UserSession();
        var json = original.ToJson();

        // Act
        var succeeded = UserSessionJsonExtensions.TryFromJson(json, out var value);

        // Assert
        Assert.True(succeeded);
        Assert.NotNull(value);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
    {
        // Arrange
        var malformedJson = "{ this is not valid json }";

        // Act
        var succeeded = UserSessionJsonExtensions.TryFromJson(malformedJson, out var value);

        // Assert
        Assert.False(succeeded);
        Assert.Null(value);
    }

    [Fact]
    public void TryFromJson_EmptyString_ReturnsFalse()
    {
        // Act
        var succeeded = UserSessionJsonExtensions.TryFromJson(string.Empty, out var value);

        // Assert
        Assert.False(succeeded);
        Assert.Null(value);
    }
}
