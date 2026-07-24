#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using DotnetAuthServer.Domain.Models;

namespace DotnetAuthServer.Tests;

public class AuthorizationRequestTests
{
    private AuthorizationRequest CreateValidRequest()
    {
        return new AuthorizationRequest
        {
            ClientId = "client-123",
            ResponseType = "code",
            RedirectUri = "https://example.com/callback",
            Scope = "openid profile email",
            State = "xyz",
            Nonce = "nonce123",
            CodeChallenge = "challenge",
            CodeChallengeMethod = "S256",
            Display = "page",
            Prompt = "login"
        };
    }

    [Fact]
    public void IsValid_ReturnsTrue_WhenAllRequiredPropertiesAreSet()
    {
        var request = CreateValidRequest();

        Assert.True(request.IsValid());
    }

    [Theory]
    [InlineData(null, "code", "https://example.com/callback", "openid")]
    [InlineData("client-123", null, "https://example.com/callback", "openid")]
    [InlineData("client-123", "code", null, "openid")]
    [InlineData("client-123", "code", "https://example.com/callback", null)]
    public void IsValid_ReturnsFalse_WhenRequiredPropertyIsMissing(
        string? clientId,
        string? responseType,
        string? redirectUri,
        string? scope)
    {
        var request = new AuthorizationRequest
        {
            ClientId = clientId,
            ResponseType = responseType,
            RedirectUri = redirectUri,
            Scope = scope
        };

        Assert.False(request.IsValid());
    }

    [Fact]
    public void GetRequestedScopes_ReturnsEmpty_WhenScopeIsNullOrWhiteSpace()
    {
        var requestNull = new AuthorizationRequest { Scope = null };
        var requestEmpty = new AuthorizationRequest { Scope = "   " };

        Assert.Empty(requestNull.GetRequestedScopes());
        Assert.Empty(requestEmpty.GetRequestedScopes());
    }

    [Fact]
    public void GetRequestedScopes_ParsesSpaceSeparatedScopes_Correctly()
    {
        var request = new AuthorizationRequest { Scope = "openid profile email  " };

        var scopes = request.GetRequestedScopes().ToArray();

        Assert.Equal(3, scopes.Length);
        Assert.Contains("openid", scopes);
        Assert.Contains("profile", scopes);
        Assert.Contains("email", scopes);
    }

    [Fact]
    public void HasPkce_ReturnsTrue_WhenCodeChallengeIsProvided()
    {
        var request = new AuthorizationRequest { CodeChallenge = "some-challenge" };

        Assert.True(request.HasPkce());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void HasPkce_ReturnsFalse_WhenCodeChallengeIsMissingOrWhitespace(string? challenge)
    {
        var request = new AuthorizationRequest { CodeChallenge = challenge };

        Assert.False(request.HasPkce());
    }

    [Fact]
    public void IsOpenIdRequest_ReturnsTrue_WhenScopeContainsOpenId_IgnoringCase()
    {
        var request = new AuthorizationRequest { Scope = "profile OpenID email" };

        Assert.True(request.IsOpenIdRequest());
    }

    [Fact]
    public void IsOpenIdRequest_ReturnsFalse_WhenOpenIdScopeIsAbsent()
    {
        var request = new AuthorizationRequest { Scope = "profile email" };

        Assert.False(request.IsOpenIdRequest());
    }
}
