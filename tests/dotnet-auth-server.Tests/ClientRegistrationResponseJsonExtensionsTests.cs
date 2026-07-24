#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetAuthServer.Domain.Models;
using Xunit;

namespace DotnetAuthServer.Tests;

public class ClientRegistrationResponseJsonExtensionsTests
{
    [Fact]
    public void ToJson_ReturnsJsonString_WhenClientRegistrationResponseIsValid()
    {
        // Arrange
        var clientRegistrationResponse = new ClientRegistrationResponse
        {
            ClientId = "client-id",
            ClientSecret = "client-secret",
            RedirectUris = new[] { "https://example.com/callback" },
            GrantTypes = new[] { "authorization_code" },
            ResponseTypes = new[] { "code" },
            Scope = "openid profile email"
        };

        // Act
        var json = clientRegistrationResponse.ToJson();

        // Assert
        Assert.NotEmpty(json);
    }

    [Fact]
    public void ToJson_ThrowsArgumentNullException_WhenClientRegistrationResponseIsNull()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => (new ClientRegistrationResponse()).ToJson());
    }

    [Fact]
    public void FromJson_ReturnsClientRegistrationResponse_WhenJsonIsValid()
    {
        // Arrange
        var json = "{\"ClientId\":\"client-id\",\"ClientSecret\":\"client-secret\",\"RedirectUris\":[\"https://example.com/callback\"],\"GrantTypes\":[\"authorization_code\"],\"ResponseTypes\":[\"code\"],\"Scope\":\"openid profile email\"}";

        // Act
        var clientRegistrationResponse = ClientRegistrationResponseJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(clientRegistrationResponse);
    }

    [Fact]
    public void FromJson_ReturnsNull_WhenJsonIsInvalid()
    {
        // Act
        var clientRegistrationResponse = ClientRegistrationResponseJsonExtensions.FromJson(null);

        // Assert
        Assert.Null(clientRegistrationResponse);
    }

    [Fact]
    public void TryFromJson_ReturnsTrue_WhenJsonIsValid()
    {
        // Arrange
        var json = "{\"ClientId\":\"client-id\",\"ClientSecret\":\"client-secret\",\"RedirectUris\":[\"https://example.com/callback\"],\"GrantTypes\":[\"authorization_code\"],\"ResponseTypes\":[\"code\"],\"Scope\":\"openid profile email\"}";

        // Act
        var success = ClientRegistrationResponseJsonExtensions.TryFromJson(json, out var clientRegistrationResponse);

        // Assert
        Assert.True(success);
        Assert.NotNull(clientRegistrationResponse);
    }

    [Fact]
    public void TryFromJson_ReturnsFalse_WhenJsonIsInvalid()
    {
        // Act
        var success = ClientRegistrationResponseJsonExtensions.TryFromJson(null, out var clientRegistrationResponse);

        // Assert
        Assert.False(success);
        Assert.Null(clientRegistrationResponse);
    }
}
