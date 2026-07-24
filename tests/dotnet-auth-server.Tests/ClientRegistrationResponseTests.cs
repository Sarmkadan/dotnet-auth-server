#nullable enable
using Xunit;
using DotnetAuthServer.Domain.Models;

namespace DotnetAuthServer.Tests;

public class ClientRegistrationResponseTests
{
    [Fact]
    public void DefaultValues_AreAsExpected()
    {
        // Arrange
        var response = new ClientRegistrationResponse();

        // Act & Assert
        Assert.Null(response.ClientId);
        Assert.Null(response.ClientSecret);
        Assert.Equal(0L, response.ClientIdIssuedAt);
        Assert.Null(response.ClientSecretExpiresAt);
        Assert.Null(response.ClientName);
        Assert.Empty(response.GrantTypes);
        Assert.Empty(response.RedirectUris);
        Assert.Empty(response.ResponseTypes);
        Assert.Null(response.Scope);
        Assert.Equal("client_secret_basic", response.TokenEndpointAuthMethod);
        Assert.Null(response.LogoUri);
        Assert.Null(response.PolicyUri);
        Assert.Null(response.TosUri);
        Assert.Empty(response.Contacts);
    }

    [Fact]
    public void SettingProperties_UpdatesValuesCorrectly()
    {
        // Arrange
        var response = new ClientRegistrationResponse
        {
            ClientId = "client-123",
            ClientSecret = "secret",
            ClientIdIssuedAt = 1625097600,
            ClientSecretExpiresAt = 1627689600,
            ClientName = "Test Client",
            GrantTypes = new List<string> { "authorization_code", "refresh_token" },
            RedirectUris = new List<string> { "https://example.com/callback" },
            ResponseTypes = new List<string> { "code" },
            Scope = "openid profile",
            TokenEndpointAuthMethod = "client_secret_post",
            LogoUri = "https://example.com/logo.png",
            PolicyUri = "https://example.com/policy",
            TosUri = "https://example.com/tos",
            Contacts = new List<string> { "admin@example.com" }
        };

        // Act & Assert
        Assert.Equal("client-123", response.ClientId);
        Assert.Equal("secret", response.ClientSecret);
        Assert.Equal(1625097600L, response.ClientIdIssuedAt);
        Assert.Equal(1627689600L, response.ClientSecretExpiresAt);
        Assert.Equal("Test Client", response.ClientName);
        Assert.Equal(2, response.GrantTypes.Count);
        Assert.Contains("authorization_code", response.GrantTypes);
        Assert.Contains("refresh_token", response.GrantTypes);
        Assert.Single(response.RedirectUris);
        Assert.Contains("https://example.com/callback", response.RedirectUris);
        Assert.Single(response.ResponseTypes);
        Assert.Contains("code", response.ResponseTypes);
        Assert.Equal("openid profile", response.Scope);
        Assert.Equal("client_secret_post", response.TokenEndpointAuthMethod);
        Assert.Equal("https://example.com/logo.png", response.LogoUri);
        Assert.Equal("https://example.com/policy", response.PolicyUri);
        Assert.Equal("https://example.com/tos", response.TosUri);
        Assert.Single(response.Contacts);
        Assert.Contains("admin@example.com", response.Contacts);
    }

    [Fact]
    public void NullableStringProperties_AcceptNullValues()
    {
        // Arrange
        var response = new ClientRegistrationResponse
        {
            ClientSecret = null,
            Scope = null,
            LogoUri = null,
            PolicyUri = null,
            TosUri = null
        };

        // Act & Assert
        Assert.Null(response.ClientSecret);
        Assert.Null(response.Scope);
        Assert.Null(response.LogoUri);
        Assert.Null(response.PolicyUri);
        Assert.Null(response.TosUri);
    }

    [Fact]
    public void CollectionProperties_AcceptEmptyAndNull()
    {
        // Arrange
        var response = new ClientRegistrationResponse
        {
            GrantTypes = new List<string>(),
            RedirectUris = new List<string>(),
            ResponseTypes = new List<string>()
        };

        // Act & Assert
        Assert.Empty(response.GrantTypes);
        Assert.Empty(response.RedirectUris);
        Assert.Empty(response.ResponseTypes);

        // Setting to null via null! cast
        response.GrantTypes = null!;
        response.RedirectUris = null!;
        response.ResponseTypes = null!;

        Assert.Null(response.GrantTypes);
        Assert.Null(response.RedirectUris);
        Assert.Null(response.ResponseTypes);
    }

    [Fact]
    public void TokenEndpointAuthMethod_DefaultValue_IsClientSecretBasic()
    {
        // Arrange
        var response = new ClientRegistrationResponse();

        // Act & Assert
        Assert.Equal("client_secret_basic", response.TokenEndpointAuthMethod);
    }
}
