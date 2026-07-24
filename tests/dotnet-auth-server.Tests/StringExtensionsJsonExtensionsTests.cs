#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;
using DotnetAuthServer.Extensions;
using Xunit;

namespace DotnetAuthServer.Tests;

public class StringExtensionsJsonExtensionsTests
{
    [Fact]
    public void ToJson_Collection_HappyPath()
    {
        // Arrange
        IEnumerable<string> values = new[] { "alpha", "beta", "gamma" };

        // Act
        var json = values.ToJson();

        // Assert
        var deserialized = JsonSerializer.Deserialize<IEnumerable<string>>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(values, deserialized);
    }

    [Fact]
    public void ToJson_Collection_Indented_ProducesReadableJson()
    {
        // Arrange
        IEnumerable<string> values = new[] { "one", "two" };

        // Act
        var json = values.ToJson(indented: true);

        // Assert
        // Indented JSON contains line breaks and spaces
        Assert.Contains("\n", json);
        Assert.Contains("  ", json);
    }

    [Fact]
    public void ToJson_Collection_Null_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<string>? values = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => values!.ToJson());
    }

    [Fact]
    public void FromJsonToScopes_ValidJson_ReturnsCollection()
    {
        // Arrange
        var original = new[] { "openid", "profile", "email" };
        var json = JsonSerializer.Serialize(original);

        // Act
        var result = json.FromJsonToScopes();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(original, result);
    }

    [Fact]
    public void FromJsonToScopes_NullOrWhiteSpace_ReturnsNull()
    {
        Assert.Null(((string?)null).FromJsonToScopes());
        Assert.Null(string.Empty.FromJsonToScopes());
        Assert.Null("   ".FromJsonToScopes());
    }

    [Fact]
    public void FromJsonToScopes_InvalidJson_ReturnsNull()
    {
        // Arrange
        var invalidJson = "{ not a json array }";

        // Act
        var result = invalidJson.FromJsonToScopes();

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJsonToScopes_ValidJson_ReturnsTrueAndScopes()
    {
        // Arrange
        var scopes = new[] { "read", "write" };
        var json = JsonSerializer.Serialize(scopes);

        // Act
        var success = json.TryFromJsonToScopes(out var result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal(scopes, result);
    }

    [Fact]
    public void TryFromJsonToScopes_InvalidOrEmptyJson_ReturnsFalse()
    {
        // Null / whitespace
        Assert.False(((string?)null).TryFromJsonToScopes(out var nullResult1));
        Assert.Null(nullResult1);

        Assert.False(string.Empty.TryFromJsonToScopes(out var nullResult2));
        Assert.Null(nullResult2);

        // Invalid JSON
        var badJson = "not a json";
        Assert.False(badJson.TryFromJsonToScopes(out var nullResult3));
        Assert.Null(nullResult3);
    }

    [Fact]
    public void ToJson_String_MasksAndSerializes()
    {
        // Arrange
        var secret = "SuperSecretPassword123!";

        // Act
        var json = secret.ToJson();

        // Assert
        using var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("masked", out var maskedProp));
        var maskedValue = maskedProp.GetString();
        Assert.NotNull(maskedValue);
        // The masked value should not be the original clear text
        Assert.NotEqual(secret, maskedValue);
        // It should contain at least one masking character (implementation uses '*')
        Assert.Contains("*", maskedValue);
    }

    [Fact]
    public void ToJson_String_WithIndentation_ContainsLineBreaks()
    {
        // Arrange
        var value = "test";

        // Act
        var json = value.ToJson(indented: true);

        // Assert
        Assert.Contains("\n", json);
    }

    [Fact]
    public void ToJson_String_Null_ThrowsArgumentNullException()
    {
        string? value = null;
        Assert.Throws<ArgumentNullException>(() => value!.ToJson());
    }

    [Fact]
    public void ToJson_String_WithMaxLength_TruncatesCorrectly()
    {
        // Arrange
        var original = "abcdefghijklmnopqrstuvwxyz";
        var maxLength = 10;

        // Act
        var json = original.ToJson(maxLength);

        // Assert
        using var doc = JsonDocument.Parse(json);
        var truncated = doc.RootElement.GetProperty("truncated").GetString();
        Assert.NotNull(truncated);
        Assert.Equal(original.Substring(0, maxLength), truncated);
    }

    [Fact]
    public void ToJson_String_WithNegativeMaxLength_ThrowsArgumentOutOfRangeException()
    {
        var value = "any";
        Assert.Throws<ArgumentOutOfRangeException>(() => value.ToJson(-1));
    }

    [Fact]
    public void ToJson_String_WithMaxLength_AndIndentation_ProducesIndentedJson()
    {
        // Arrange
        var value = "1234567890";

        // Act
        var json = value.ToJson(5, indented: true);

        // Assert
        Assert.Contains("\n", json);
    }
}
