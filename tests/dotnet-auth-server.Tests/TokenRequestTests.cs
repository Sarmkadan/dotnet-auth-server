#nullable enable
using Xunit;
using DotnetAuthServer.Domain.Models;

namespace DotnetAuthServer.Tests;

public class TokenRequestTests
{
    [Fact]
    public void IsValid_ReturnsTrue_WhenGrantTypeAndClientIdArePresent()
    {
        // Arrange
        var request = new TokenRequest
        {
            GrantType = "password",
            ClientId = "client-123"
        };

        // Act
        var result = request.IsValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenGrantTypeIsMissing()
    {
        // Arrange
        var request = new TokenRequest
        {
            ClientId = "client-123"
        };

        // Act
        var result = request.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenClientIdIsMissing()
    {
        // Arrange
        var request = new TokenRequest
        {
            GrantType = "password"
        };

        // Act
        var result = request.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidForGrantType_ReturnsTrue_ForAuthorizationCode_WithValidParams()
    {
        // Arrange
        var request = new TokenRequest
        {
            GrantType = "authorization_code",
            ClientId = "client-123",
            Code = "auth-code",
            RedirectUri = "https://example.com/callback"
        };

        // Act
        var result = request.IsValidForGrantType("authorization_code");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidForGrantType_ReturnsFalse_ForAuthorizationCode_WhenCodeIsMissing()
    {
        // Arrange
        var request = new TokenRequest
        {
            GrantType = "authorization_code",
            ClientId = "client-123",
            RedirectUri = "https://example.com/callback"
        };

        // Act
        var result = request.IsValidForGrantType("authorization_code");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidForGrantType_ReturnsTrue_ForPasswordGrant_WithValidParams()
    {
        // Arrange
        var request = new TokenRequest
        {
            GrantType = "password",
            ClientId = "client-123",
            Username = "user",
            Password = "pass"
        };

        // Act
        var result = request.IsValidForGrantType("password");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidForGrantType_ReturnsFalse_ForUnknownGrantType()
    {
        // Arrange
        var request = new TokenRequest
        {
            GrantType = "unknown_grant",
            ClientId = "client-123"
        };

        // Act
        var result = request.IsValidForGrantType("unknown_grant");

        // Assert
        Assert.False(result);
    }
}
