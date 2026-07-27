using System;
using System.Runtime.Serialization;
using DotnetAuthServer.Services;
using Xunit;

namespace DotnetAuthServer.Tests;

public class ClientValidationServiceJsonExtensionsTests
{
    private static ClientValidationService CreateNonNullInstance()
    {
        // Create an instance without invoking any constructor.
        // This works even if the type has no public parameterless constructor.
        return (ClientValidationService)FormatterServices.GetUninitializedObject(typeof(ClientValidationService));
    }

    [Fact]
    public void ToJson_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        ClientValidationService? nullService = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullService!.ToJson());
    }

    [Fact]
    public void ToJson_NonNullValue_ThrowsNotSupportedException()
    {
        // Arrange
        var service = CreateNonNullInstance();

        // Act & Assert
        var ex = Assert.Throws<NotSupportedException>(() => service.ToJson());
        Assert.Contains("cannot be serialized", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void FromJson_NullOrEmpty_ThrowsArgumentException(string? json)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ClientValidationServiceJsonExtensions.FromJson(json!));
    }

    [Fact]
    public void FromJson_NonEmpty_ThrowsNotSupportedException()
    {
        // Arrange
        var json = "{}";

        // Act & Assert
        var ex = Assert.Throws<NotSupportedException>(() => ClientValidationServiceJsonExtensions.FromJson(json));
        Assert.Contains("cannot be deserialized", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void TryFromJson_NullOrEmpty_ThrowsArgumentException(string? json)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => ClientValidationServiceJsonExtensions.TryFromJson(json!, out _));
    }

    [Fact]
    public void TryFromJson_NonEmpty_ReturnsFalseAndNullValue()
    {
        // Arrange
        var json = "{\"some\":\"value\"}";

        // Act
        var result = ClientValidationServiceJsonExtensions.TryFromJson(json, out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }
}
