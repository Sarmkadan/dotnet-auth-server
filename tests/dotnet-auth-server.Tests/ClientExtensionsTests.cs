#nullable enable
using System;
using System.Collections.Generic;
using DotnetAuthServer.Domain.Entities;
using Xunit;

namespace DotnetAuthServer.Tests;

public class ClientExtensionsTests
{
    [Fact]
    public void IsPublicClient_HappyPath_ReturnsTrue()
    {
        // Arrange
        var client = new Client { IsConfidential = false };

        // Act
        var result = client.IsPublicClient();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsPublicClient_EdgeCase_PublicClient_ReturnsTrue()
    {
        // Arrange
        var client = new Client { IsConfidential = true };

        // Act
        var result = client.IsPublicClient();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RequiresPkce_HappyPath_ReturnsFalse()
    {
        // Arrange
        var client = new Client { IsConfidential = false, RequirePkce = false };

        // Act
        var result = client.RequiresPkce();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RequiresPkce_EdgeCase_ConfidentialClient_ReturnsTrue()
    {
        // Arrange
        var client = new Client { IsConfidential = true, RequirePkce = true };

        // Act
        var result = client.RequiresPkce();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void GetTokenLifetimeMinutes_HappyPath_ReturnsCorrectValue()
    {
        // Arrange
        var client = new Client { AccessTokenLifetime = 3600, RefreshTokenLifetime = 86400 };

        // Act
        var result = client.GetTokenLifetimeMinutes(TokenType.Access);

        // Assert
        Assert.Equal(60, result);
    }

    [Fact]
    public void GetTokenLifetimeMinutes_EdgeCase_NullClient_ThrowsArgumentNullException()
    {
        // Arrange
        Client? client = null;

        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => client!.GetTokenLifetimeMinutes(TokenType.Access));
    }

    [Fact]
    public void HasCorsOrigins_HappyPath_ReturnsTrue()
    {
        // Arrange
        var client = new Client { AllowedCorsOrigins = new[] { "https://example.com" } };

        // Act
        var result = client.HasCorsOrigins();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasCorsOrigins_EdgeCase_EmptyCorsOrigins_ReturnsFalse()
    {
        // Arrange
        var client = new Client { AllowedCorsOrigins = Array.Empty<string>() };

        // Act
        var result = client.HasCorsOrigins();

        // Assert
        Assert.False(result);
    }
}
