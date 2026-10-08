using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DotnetAuthServer.Domain.Models;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace DotnetAuthServer.Tests;

public class TokenResponseTests
{
    [Fact]
    public void Constructor_Default_CreatesInstanceWithDefaultValues()
    {
        // Act
        var tokenResponse = new TokenResponse();

        // Assert
        Assert.NotNull(tokenResponse);
        Assert.Null(tokenResponse.AccessToken);
        Assert.Equal("Bearer", tokenResponse.TokenType);
        Assert.Equal(0, tokenResponse.ExpiresIn);
        Assert.Null(tokenResponse.RefreshToken);
        Assert.Null(tokenResponse.Scope);
        Assert.Null(tokenResponse.IdToken);
        Assert.NotNull(tokenResponse.CustomProperties);
        Assert.Empty(tokenResponse.CustomProperties);
    }

    [Fact]
    public void Constructor_WithAllProperties_InitializesCorrectly()
    {
        // Arrange
        var accessToken = "test-access-token";
        var tokenType = "Bearer";
        var expiresIn = 3600;
        var refreshToken = "test-refresh-token";
        var scope = "openid profile email";
        var idToken = "test-id-token";
        var customProperties = new Dictionary<string, object> { { "custom_claim", "custom_value" } };

        // Act
        var tokenResponse = new TokenResponse
        {
            AccessToken = accessToken,
            TokenType = tokenType,
            ExpiresIn = expiresIn,
            RefreshToken = refreshToken,
            Scope = scope,
            IdToken = idToken,
            CustomProperties = customProperties
        };

        // Assert
        Assert.Equal(accessToken, tokenResponse.AccessToken);
        Assert.Equal(tokenType, tokenResponse.TokenType);
        Assert.Equal(expiresIn, tokenResponse.ExpiresIn);
        Assert.Equal(refreshToken, tokenResponse.RefreshToken);
        Assert.Equal(scope, tokenResponse.Scope);
        Assert.Equal(idToken, tokenResponse.IdToken);
        Assert.Same(customProperties, tokenResponse.CustomProperties);
        Assert.Equal("custom_value", tokenResponse.CustomProperties["custom_claim"]);
    }

    [Fact]
    public void AccessToken_SetAndGet_ReturnsSameValue()
    {
        // Arrange
        var tokenResponse = new TokenResponse();
        var expected = "access-token-12345";

        // Act
        tokenResponse.AccessToken = expected;

        // Assert
        Assert.Equal(expected, tokenResponse.AccessToken);
    }

    [Fact]
    public void AccessToken_SetToNull_IsAllowed()
    {
        // Arrange
        var tokenResponse = new TokenResponse();

        // Act
        tokenResponse.AccessToken = null!;

        // Assert
        Assert.Null(tokenResponse.AccessToken);
    }

    [Fact]
    public void TokenType_SetAndGet_ReturnsSameValue()
    {
        // Arrange
        var tokenResponse = new TokenResponse();
        var expected = "JWT";

        // Act
        tokenResponse.TokenType = expected;

        // Assert
        Assert.Equal(expected, tokenResponse.TokenType);
    }

    [Fact]
    public void TokenType_DefaultValue_IsBearer()
    {
        // Arrange
        var tokenResponse = new TokenResponse();

        // Assert
        Assert.Equal("Bearer", tokenResponse.TokenType);
    }

    [Fact]
    public void ExpiresIn_SetAndGet_ReturnsSameValue()
    {
        // Arrange
        var tokenResponse = new TokenResponse();
        var expected = 7200;

        // Act
        tokenResponse.ExpiresIn = expected;

        // Assert
        Assert.Equal(expected, tokenResponse.ExpiresIn);
    }

    [Fact]
    public void ExpiresIn_SetToNegative_StoresNegativeValue()
    {
        // Arrange
        var tokenResponse = new TokenResponse();
        var expected = -1;

        // Act
        tokenResponse.ExpiresIn = expected;

        // Assert
        Assert.Equal(expected, tokenResponse.ExpiresIn);
    }

    [Fact]
    public void RefreshToken_SetAndGet_ReturnsSameValue()
    {
        // Arrange
        var tokenResponse = new TokenResponse();
        var expected = "refresh-token-67890";

        // Act
        tokenResponse.RefreshToken = expected;

        // Assert
        Assert.Equal(expected, tokenResponse.RefreshToken);
    }

    [Fact]
    public void RefreshToken_SetToNull_IsAllowed()
    {
        // Arrange
        var tokenResponse = new TokenResponse();

        // Act
        tokenResponse.RefreshToken = null;

        // Assert
        Assert.Null(tokenResponse.RefreshToken);
    }

    [Fact]
    public void Scope_SetAndGet_ReturnsSameValue()
    {
        // Arrange
        var tokenResponse = new TokenResponse();
        var expected = "read write delete";

        // Act
        tokenResponse.Scope = expected;

        // Assert
        Assert.Equal(expected, tokenResponse.Scope);
    }

    [Fact]
    public void Scope_SetToNull_IsAllowed()
    {
        // Arrange
        var tokenResponse = new TokenResponse();

        // Act
        tokenResponse.Scope = null;

        // Assert
        Assert.Null(tokenResponse.Scope);
    }

    [Fact]
    public void Scope_SetToEmptyString_IsAllowed()
    {
        // Arrange
        var tokenResponse = new TokenResponse();

        // Act
        tokenResponse.Scope = string.Empty;

        // Assert
        Assert.Equal(string.Empty, tokenResponse.Scope);
    }

    [Fact]
    public void IdToken_SetAndGet_ReturnsSameValue()
    {
        // Arrange
        var tokenResponse = new TokenResponse();
        var expected = "id-token-abcde";

        // Act
        tokenResponse.IdToken = expected;

        // Assert
        Assert.Equal(expected, tokenResponse.IdToken);
    }

    [Fact]
    public void IdToken_SetToNull_IsAllowed()
    {
        // Arrange
        var tokenResponse = new TokenResponse();

        // Act
        tokenResponse.IdToken = null;

        // Assert
        Assert.Null(tokenResponse.IdToken);
    }

    [Fact]
    public void CustomProperties_SetAndGet_ReturnsSameInstance()
    {
        // Arrange
        var tokenResponse = new TokenResponse();
        var expected = new Dictionary<string, object> { { "key1", "value1" }, { "key2", 123 } };

        // Act
        tokenResponse.CustomProperties = expected;

        // Assert
        Assert.Same(expected, tokenResponse.CustomProperties);
    }

    // -------------------------------------------------------------------------
    // Diagnostics (ToString / DebuggerDisplay)
    // -------------------------------------------------------------------------

    [Fact]
    public void ToString_WithJwtAccessToken_ShowsJtiExpiryAndScopesWithoutFullTokens()
    {
        // Arrange
        var jti = "test-jti-123";
        var accessToken = CreateSignedJwt(jti, DateTime.UtcNow.AddMinutes(10));
        var response = new TokenResponse
        {
            AccessToken = accessToken,
            ExpiresIn = 600,
            RefreshToken = "refresh-token-secret-value-abcdef",
            IdToken = "id-token-secret-value-abcdef",
            Scope = "openid profile"
        };

        // Act
        var text = response.ToString();

        // Assert
        Assert.Contains($"Jti = {jti}", text);
        Assert.Contains("ExpiresAt = ", text);
        Assert.Contains("Scope = openid profile", text);
        Assert.Contains(accessToken[..6] + "...", text);
        Assert.DoesNotContain(accessToken, text);
        Assert.DoesNotContain("refresh-token-secret-value-abcdef", text);
        Assert.DoesNotContain("id-token-secret-value-abcdef", text);
    }

    [Fact]
    public void ToString_WithMalformedAccessToken_ReportsUnknownJtiWithoutThrowing()
    {
        // Arrange
        var response = new TokenResponse { AccessToken = "not.a.jwt-at-all" };

        // Act
        var text = response.ToString();

        // Assert
        Assert.Contains("Jti = n/a", text);
        Assert.Contains("ExpiresAt = n/a", text);
        Assert.DoesNotContain("not.a.jwt-at-all", text);
    }

    private static string CreateSignedJwt(string jti, DateTime expires)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(new string('k', 32)));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            claims: [new Claim(JwtRegisteredClaimNames.Jti, jti)],
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
