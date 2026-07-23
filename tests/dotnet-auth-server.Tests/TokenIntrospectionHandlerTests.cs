#nullable enable

namespace DotnetAuthServer.Tests;

using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Security;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

/// <summary>
/// Tests for <see cref="TokenIntrospectionHandler"/> which implements OAuth2 token introspection (RFC 7662).
/// </summary>
public sealed class TokenIntrospectionHandlerTests
{
    private readonly Mock<ILogger<TokenIntrospectionHandler>> _loggerMock = new();
    private readonly AuthServerOptions _options;
    private readonly RevokedTokenStore _revokedTokenStore;
    private readonly TokenIntrospectionHandler _handler;

    public TokenIntrospectionHandlerTests()
    {
        _options = new AuthServerOptions
        {
            IssuerUrl = "https://auth.example.com",
            JwtSigningKey = "test-key-32-chars-long-123456789012",
            AccessTokenLifetimeSeconds = 3600,
            JwtAlgorithm = "HS256"
        };

        _revokedTokenStore = new RevokedTokenStore();
        _handler = new TokenIntrospectionHandler(_options, _revokedTokenStore, _loggerMock.Object);
    }

    [Fact]
    public void IntrospectToken_NullToken_ReturnsInactiveResponse()
    {
        // Act
        var result = _handler.IntrospectToken(null);

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeFalse();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void IntrospectToken_EmptyToken_ReturnsInactiveResponse()
    {
        // Act
        var result = _handler.IntrospectToken("");

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeFalse();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void IntrospectToken_WhitespaceToken_ReturnsInactiveResponse()
    {
        // Act
        var result = _handler.IntrospectToken("   ");

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeFalse();
        _loggerMock.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public void IntrospectToken_ValidJwt_ReturnsActiveResponseWithClaims()
    {
        // Arrange - create a valid JWT token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_options.JwtSigningKey);
        var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.IssuerUrl,
            claims: new[]
            {
                new Claim("sub", "user123"),
                new Claim("aud", "client-app"),
                new Claim("scope", "openid profile email"),
                new Claim("jti", "token-123"),
                new Claim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        var jwt = tokenHandler.WriteToken(token);

        // Act
        var result = _handler.IntrospectToken(jwt);

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeTrue();
        result.Username.Should().Be("user123");
        result.ClientId.Should().Be("client-app");
        result.Scope.Should().Be("openid profile email");
        result.TokenType.Should().Be("Bearer");
        result.Sub.Should().Be("user123");
        result.Exp.Should().BeGreaterThan(0);
        result.Iat.Should().BeGreaterThan(0);
    }

    [Fact]
    public void IntrospectToken_ValidJwt_MissingOptionalClaims_ReturnsResponseWithNullOptionalFields()
    {
        // Arrange - create a JWT without optional claims
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_options.JwtSigningKey);
        var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.IssuerUrl,
            claims: new[]
            {
                new Claim("sub", "user456"),
                new Claim("jti", "token-456"),
                new Claim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        var jwt = tokenHandler.WriteToken(token);

        // Act
        var result = _handler.IntrospectToken(jwt);

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeTrue();
        result.Username.Should().Be("user456");
        result.ClientId.Should().BeNull();
        result.Scope.Should().BeNull();
        result.TokenType.Should().Be("Bearer");
        result.Sub.Should().Be("user456");
        result.Exp.Should().BeGreaterThan(0);
        result.Iat.Should().BeGreaterThan(0);
    }

    [Fact]
    public void IntrospectToken_ExpiredToken_ReturnsInactiveResponse()
    {
        // Arrange - create an expired JWT token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_options.JwtSigningKey);
        var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.IssuerUrl,
            claims: new[]
            {
                new Claim("sub", "user789"),
                new Claim("jti", "token-789"),
                new Claim("exp", DateTimeOffset.UtcNow.AddMinutes(-30).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("iat", DateTimeOffset.UtcNow.AddMinutes(-60).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            },
            expires: DateTime.UtcNow.AddMinutes(-30),
            signingCredentials: creds
        );

        var jwt = tokenHandler.WriteToken(token);

        // Act
        var result = _handler.IntrospectToken(jwt);

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeFalse();
    }

    [Fact]
    public void IntrospectToken_InvalidSignature_ReturnsInactiveResponse()
    {
        // Arrange - create a JWT with wrong signature
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("wrong-key-32-chars-long-12345678901");
        var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.IssuerUrl,
            claims: new[]
            {
                new Claim("sub", "user999"),
                new Claim("jti", "token-999"),
                new Claim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        var jwt = tokenHandler.WriteToken(token);

        // Act
        var result = _handler.IntrospectToken(jwt);

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeFalse();
    }

    [Fact]
    public void IntrospectToken_WrongIssuer_ReturnsInactiveResponse()
    {
        // Arrange - create a JWT with wrong issuer
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_options.JwtSigningKey);
        var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "https://wrong-issuer.com",
            claims: new[]
            {
                new Claim("sub", "user111"),
                new Claim("jti", "token-111"),
                new Claim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        var jwt = tokenHandler.WriteToken(token);

        // Act
        var result = _handler.IntrospectToken(jwt);

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeFalse();
    }

    [Fact]
    public void IntrospectToken_RevokedToken_ReturnsInactiveResponse()
    {
        // Arrange - create a valid JWT token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_options.JwtSigningKey);
        var creds = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.IssuerUrl,
            claims: new[]
            {
                new Claim("sub", "user222"),
                new Claim("aud", "client-app"),
                new Claim("scope", "openid profile"),
                new Claim("jti", "revoked-token-222"),
                new Claim("exp", DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
                new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            },
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        var jwt = tokenHandler.WriteToken(token);

        // Revoke the token
        _revokedTokenStore.Revoke("revoked-token-222", DateTime.UtcNow.AddHours(1));

        // Act
        var result = _handler.IntrospectToken(jwt);

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeFalse();
    }

    [Fact]
    public void IntrospectToken_MalformedToken_ReturnsInactiveResponse()
    {
        // Arrange - malformed token string
        var malformedToken = "this.is.not.a.valid.jwt.token.string";

        // Act
        var result = _handler.IntrospectToken(malformedToken);

        // Assert
        result.Should().NotBeNull();
        result.Active.Should().BeFalse();
    }

    [Fact]
    public void IntrospectionResponse_Properties_InitializedCorrectly()
    {
        // Arrange
        var response = new IntrospectionResponse();

        // Act - just verify it's a valid object
        // Assert - no exceptions thrown during construction
        response.Should().NotBeNull();
        response.Active.Should().BeFalse(); // default value
        response.Scope.Should().BeNull();
        response.ClientId.Should().BeNull();
        response.Username.Should().BeNull();
        response.TokenType.Should().BeNull();
        response.Exp.Should().BeNull();
        response.Iat.Should().BeNull();
        response.Sub.Should().BeNull();
    }

    [Fact]
    public void IntrospectionResponse_WithValues_PropertiesSetCorrectly()
    {
        // Arrange
        var response = new IntrospectionResponse
        {
            Active = true,
            Scope = "openid profile",
            ClientId = "test-client",
            Username = "test-user",
            TokenType = "Bearer",
            Exp = 1234567890,
            Iat = 1234567800,
            Sub = "test-subject"
        };

        // Act & Assert
        response.Active.Should().BeTrue();
        response.Scope.Should().Be("openid profile");
        response.ClientId.Should().Be("test-client");
        response.Username.Should().Be("test-user");
        response.TokenType.Should().Be("Bearer");
        response.Exp.Should().Be(1234567890);
        response.Iat.Should().Be(1234567800);
        response.Sub.Should().Be("test-subject");
    }
}
