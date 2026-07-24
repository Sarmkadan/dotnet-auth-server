using System;
using DotnetAuthServer.Domain.Models;
using Xunit;

namespace DotnetAuthServer.Tests;

public class TokenResponseJsonExtensionsTests
{
    [Fact]
    public void ToJson_WithValidTokenResponse_ReturnsJsonString()
    {
        // Arrange
        var tokenResponse = new TokenResponse
        {
            AccessToken = "test-access-token",
            TokenType = "Bearer",
            ExpiresIn = 3600,
            RefreshToken = "test-refresh-token",
            Scope = "openid profile email",
            IdToken = "test-id-token"
        };

        // Act
        var result = tokenResponse.ToJson();

        // Assert
        Assert.NotNull(result);
        Assert.Contains("test-access-token", result);
        Assert.Contains("Bearer", result);
        Assert.Contains("3600", result);
    }

    [Fact]
    public void ToJson_WithIndentedTrue_ReturnsFormattedJson()
    {
        // Arrange
        var tokenResponse = new TokenResponse
        {
            AccessToken = "test-token",
            ExpiresIn = 1800
        };

        // Act
        var result = tokenResponse.ToJson(indented: true);

        // Assert
        Assert.NotNull(result);
        Assert.Contains("{", result);
        Assert.Contains("}", result);
        Assert.Contains("\n", result);
    }

    [Fact]
    public void ToJson_NullTokenResponse_ThrowsArgumentNullException()
    {
        // Arrange
        TokenResponse? nullTokenResponse = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullTokenResponse!.ToJson());
    }

    [Fact]
    public void FromJson_WithValidJson_ReturnsTokenResponse()
    {
        // Arrange
        var json = "{\"access_token\":\"test-access\",\"token_type\":\"Bearer\",\"expires_in\":3600}";

        // Act
        var result = TokenResponseJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test-access", result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(3600, result.ExpiresIn);
    }

    [Fact]
    public void FromJson_WithEmptyOrWhitespace_ReturnsNull()
    {
        // Arrange
        var emptyJson = string.Empty;
        var whitespaceJson = "   \n\t  ";

        // Act
        var emptyResult = TokenResponseJsonExtensions.FromJson(emptyJson);
        var whitespaceResult = TokenResponseJsonExtensions.FromJson(whitespaceJson);

        // Assert
        Assert.Null(emptyResult);
        Assert.Null(whitespaceResult);
    }

    [Fact]
    public void TryFromJson_WithValidJson_ReturnsTrueAndSetsValue()
    {
        // Arrange
        var json = "{\"access_token\":\"test-access\",\"token_type\":\"Bearer\",\"expires_in\":3600}";

        // Act
        var success = TokenResponseJsonExtensions.TryFromJson(json, out var result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.Equal("test-access", result.AccessToken);
    }

    [Fact]
    public void TryFromJson_WithInvalidJson_ReturnsFalseAndSetsNull()
    {
        // Arrange
        var invalidJson = "{invalid json";

        // Act
        var success = TokenResponseJsonExtensions.TryFromJson(invalidJson, out var result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void Roundtrip_SerializeThenDeserialize_ReturnsEquivalentObject()
    {
        // Arrange
        var original = new TokenResponse
        {
            AccessToken = "roundtrip-token",
            TokenType = "Bearer",
            ExpiresIn = 7200,
            RefreshToken = "roundtrip-refresh",
            Scope = "profile email"
        };

        // Act
        var json = original.ToJson();
        var deserialized = TokenResponseJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(original.AccessToken, deserialized.AccessToken);
        Assert.Equal(original.TokenType, deserialized.TokenType);
        Assert.Equal(original.ExpiresIn, deserialized.ExpiresIn);
        Assert.Equal(original.RefreshToken, deserialized.RefreshToken);
        Assert.Equal(original.Scope, deserialized.Scope);
    }
}