using System;
using DotnetAuthServer.Handlers;
using Xunit;

namespace dotnet_auth_server.Tests;

public class TokenIntrospectionHandlerJsonExtensionsTests
{
    [Fact]
    public void ToJson_WithValidObject_ReturnsJson()
    {
        // Arrange
        var response = new IntrospectionResponse();

        // Act
        var json = response.ToJson();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(json));
        // The JSON should represent an object (starts with '{' and ends with '}')
        Assert.StartsWith("{", json);
        Assert.EndsWith("}", json);
    }

    [Fact]
    public void ToJson_WithIndentation_ContainsNewLine()
    {
        // Arrange
        var response = new IntrospectionResponse();

        // Act
        var json = response.ToJson(indented: true);

        // Assert
        Assert.Contains("\n", json);
    }

    [Fact]
    public void ToJson_NullValue_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TokenIntrospectionHandlerJsonExtensions.ToJson(null!));
    }

    [Fact]
    public void FromJson_ValidJson_ReturnsObject()
    {
        // Arrange
        var response = new IntrospectionResponse();
        var json = response.ToJson();

        // Act
        var deserialized = TokenIntrospectionHandlerJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.IsType<IntrospectionResponse>(deserialized);
    }

    [Fact]
    public void FromJson_NullJson_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TokenIntrospectionHandlerJsonExtensions.FromJson(null!));
    }

    [Fact]
    public void FromJson_EmptyJson_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => TokenIntrospectionHandlerJsonExtensions.FromJson(string.Empty));
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndObject()
    {
        // Arrange
        var response = new IntrospectionResponse();
        var json = response.ToJson();

        // Act
        var result = TokenIntrospectionHandlerJsonExtensions.TryFromJson(json, out var deserialized);

        // Assert
        Assert.True(result);
        Assert.NotNull(deserialized);
        Assert.IsType<IntrospectionResponse>(deserialized);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalse()
    {
        // Arrange
        var invalidJson = "{ this is not valid json }";

        // Act
        var result = TokenIntrospectionHandlerJsonExtensions.TryFromJson(invalidJson, out var deserialized);

        // Assert
        Assert.False(result);
        Assert.Null(deserialized);
    }

    [Fact]
    public void TryFromJson_NullJson_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => TokenIntrospectionHandlerJsonExtensions.TryFromJson(null!, out _));
    }

    [Fact]
    public void TryFromJson_EmptyJson_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => TokenIntrospectionHandlerJsonExtensions.TryFromJson(string.Empty, out _));
    }
}
