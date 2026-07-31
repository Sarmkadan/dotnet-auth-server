using System;
using System.Collections.Generic;
using DotnetAuthServer.Domain.Entities;
using Xunit;

namespace DotnetAuthServer.Tests;

public sealed class ClientModelExtensionsTests
{
    [Fact]
    public void IsConfidential_ReturnsTrue_WhenConfidential()
    {
        var client = new Client { IsConfidential = true };
        Assert.True(client.IsConfidential());
    }

    [Fact]
    public void IsConfidential_ReturnsFalse_WhenNotConfidential()
    {
        var client = new Client { IsConfidential = false };
        Assert.False(client.IsConfidential());
    }

    [Fact]
    public void AllowsGrantType_ReturnsTrue_WhenGrantExists()
    {
        var client = new Client
        {
            AllowedGrantTypes = new List<string> { "authorization_code", "client_credentials" }
        };
        Assert.True(client.AllowsGrantType("client_credentials"));
    }

    [Fact]
    public void AllowsGrantType_ReturnsFalse_WhenGrantMissing()
    {
        var client = new Client
        {
            AllowedGrantTypes = new List<string> { "authorization_code" }
        };
        Assert.False(client.AllowsGrantType("implicit"));
    }

    [Fact]
    public void HasRedirectUri_ReturnsTrue_WhenUriExists()
    {
        var client = new Client
        {
            RedirectUris = new List<string>
            {
                "https://example.com/callback",
                "http://localhost:5000/callback"
            }
        };
        var uri = new Uri("https://example.com/callback");
        Assert.True(client.HasRedirectUri(uri));
    }

    [Fact]
    public void HasRedirectUri_ReturnsFalse_WhenUriMissing()
    {
        var client = new Client
        {
            RedirectUris = new List<string> { "https://example.com/callback" }
        };
        var uri = new Uri("https://other.com/callback");
        Assert.False(client.HasRedirectUri(uri));
    }
}
