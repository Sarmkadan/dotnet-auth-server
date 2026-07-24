using System.Collections.Generic;
using DotnetAuthServer.Domain.Models;
using Xunit;

namespace DotnetAuthServer.Tests;

public sealed class ConsentRequestTests
{
    [Fact]
    public void GetScopesString_MultipleScopes_ReturnsSpaceSeparatedString()
    {
        var request = new ConsentRequest
        {
            GrantedScopes = new List<string> { "openid", "profile", "email" }
        };

        var result = request.GetScopesString();

        Assert.Equal("openid profile email", result);
    }

    [Fact]
    public void GetScopesString_EmptyScopes_ReturnsEmptyString()
    {
        var request = new ConsentRequest
        {
            GrantedScopes = new List<string>()
        };

        var result = request.GetScopesString();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void IsValid_HappyPath_ApprovedFalse_ReturnsTrue()
    {
        var request = new ConsentRequest
        {
            UserId = "user123",
            ClientId = "clientABC",
            Approved = false,
            GrantedScopes = new List<string>() // empty is fine when not approved
        };

        Assert.True(request.IsValid());
    }

    [Fact]
    public void IsValid_HappyPath_ApprovedTrueWithScopes_ReturnsTrue()
    {
        var request = new ConsentRequest
        {
            UserId = "user123",
            ClientId = "clientABC",
            Approved = true,
            GrantedScopes = new List<string> { "openid" }
        };

        Assert.True(request.IsValid());
    }

    [Fact]
    public void IsValid_ApprovedTrueWithoutScopes_ReturnsFalse()
    {
        var request = new ConsentRequest
        {
            UserId = "user123",
            ClientId = "clientABC",
            Approved = true,
            GrantedScopes = new List<string>() // no scopes granted
        };

        Assert.False(request.IsValid());
    }

    [Fact]
    public void IsValid_NullUserId_ReturnsFalse()
    {
        var request = new ConsentRequest
        {
            UserId = null,
            ClientId = "clientABC",
            Approved = false,
            GrantedScopes = new List<string>()
        };

        Assert.False(request.IsValid());
    }

    [Fact]
    public void IsValid_NullClientId_ReturnsFalse()
    {
        var request = new ConsentRequest
        {
            UserId = "user123",
            ClientId = null,
            Approved = false,
            GrantedScopes = new List<string>()
        };

        Assert.False(request.IsValid());
    }
}
