// tests/dotnet-auth-server.Tests/OpaClientJsonExtensionsTests.cs
using System;
using System.Net.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using DotnetAuthServer.Integration;
using DotnetAuthServer.Configuration;

namespace DotnetAuthServer.Tests;

public sealed class OpaClientJsonExtensionsTests
{
    private static OpaClient CreateOpaClient()
    {
        // OpaClient requires an HttpClient, OpaOptions and an ILogger<OpaClient>.
        // All of these can be created with their default constructors or mocked.
        var httpClient = new HttpClient(); // No special handler needed for serialization tests.
        var options = new OpaOptions();    // Assuming a parameter‑less constructor exists.
        var logger = Mock.Of<ILogger<OpaClient>>();

        return new OpaClient(httpClient, options, logger);
    }

    [Fact]
    public void ToJson_NullArgument_ThrowsArgumentNullException()
    {
        // Arrange
        OpaClient? client = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => client!.ToJson());
    }

    [Fact]
    public void ToJson_ValidInstance_ReturnsNonEmptyJson()
    {
        // Arrange
        var client = CreateOpaClient();

        // Act
        var json = client.ToJson();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(json));
        // The JSON should represent an object (at least "{}").
        Assert.StartsWith("{", json.TrimStart());
    }

    [Fact]
    public void ToJson_WithIndentation_ProducesIndentedJson()
    {
        // Arrange
        var client = CreateOpaClient();

        // Act
        var json = client.ToJson(indented: true);

        // Assert
        // Indented JSON contains line‑breaks.
        Assert.Contains(Environment.NewLine, json);
    }

    [Fact]
    public void FromJson_NullArgument_ThrowsArgumentNullException()
    {
        // Arrange
        string? json = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => OpaClientJsonExtensions.FromJson(json!));
    }

    [Fact]
    public void FromJson_EmptyOrWhiteSpace_ReturnsNull()
    {
        // Arrange
        var empty = "";
        var whitespace = "   \t\r\n";

        // Act
        var resultEmpty = OpaClientJsonExtensions.FromJson(empty);
        var resultWhite = OpaClientJsonExtensions.FromJson(whitespace);

        // Assert
        Assert.Null(resultEmpty);
        Assert.Null(resultWhite);
    }

    [Fact]
    public void FromJson_ValidJson_ReturnsInstance()
    {
        // Arrange
        var client = CreateOpaClient();
        var json = client.ToJson();

        // Act
        var deserialized = OpaClientJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.IsType<OpaClient>(deserialized);
    }

    [Fact]
    public void TryFromJson_NullArgument_ThrowsArgumentNullException()
    {
        // Arrange
        string? json = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => OpaClientJsonExtensions.TryFromJson(json!, out _));
    }

    [Fact]
    public void TryFromJson_EmptyOrWhiteSpace_ReturnsFalse()
    {
        // Arrange
        var empty = "";
        var whitespace = "   ";

        // Act
        var resultEmpty = OpaClientJsonExtensions.TryFromJson(empty, out var valueEmpty);
        var resultWhite = OpaClientJsonExtensions.TryFromJson(whitespace, out var valueWhite);

        // Assert
        Assert.False(resultEmpty);
        Assert.False(resultWhite);
        Assert.Null(valueEmpty);
        Assert.Null(valueWhite);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalse()
    {
        // Arrange
        var invalidJson = "this is not json";

        // Act
        var success = OpaClientJsonExtensions.TryFromJson(invalidJson, out var value);

        // Assert
        Assert.False(success);
        Assert.Null(value);
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndInstance()
    {
        // Arrange
        var client = CreateOpaClient();
        var json = client.ToJson();

        // Act
        var success = OpaClientJsonExtensions.TryFromJson(json, out var value);

        // Assert
        Assert.True(success);
        Assert.NotNull(value);
        Assert.IsType<OpaClient>(value);
    }
}
