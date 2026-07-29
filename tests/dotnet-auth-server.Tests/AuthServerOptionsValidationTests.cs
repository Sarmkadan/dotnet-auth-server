using System;
using System.Collections.Generic;
using DotnetAuthServer.Configuration;
using Xunit;

namespace DotnetAuthServer.Tests;

public class AuthServerOptionsValidationTests
{
    [Fact]
    public void Validate_HappyPath_ReturnsEmptyList()
    {
        // Arrange
        var options = new AuthServerOptions
        {
            IssuerUrl = "https://example.com",
            JwtSigningKey = "secretkey",
            JwtAlgorithm = "HS256",
            AccessTokenLifetimeSeconds = 3600,
            RefreshTokenLifetimeSeconds = 3600,
            AuthorizationCodeLifetimeSeconds = 3600,
            MaxRefreshTokenGenerations = 10,
            ClockSkewToleranceSeconds = 0,
            DatabaseConnectionString = "connectionstring",
            FailedLoginAttemptThreshold = 5,
            AccountLockoutDurationMinutes = 30,
            SupportedScopes = new List<string> { "scope1", "scope2" },
            SupportedGrantTypes = new List<string> { "authorization_code", "refresh_token" }
        };

        // Act
        var errors = AuthServerOptionsValidation.Validate(options);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void IsValid_HappyPath_ReturnsTrue()
    {
        // Arrange
        var options = new AuthServerOptions
        {
            IssuerUrl = "https://example.com",
            JwtSigningKey = "secretkey",
            JwtAlgorithm = "HS256",
            AccessTokenLifetimeSeconds = 3600,
            RefreshTokenLifetimeSeconds = 3600,
            AuthorizationCodeLifetimeSeconds = 3600,
            MaxRefreshTokenGenerations = 10,
            ClockSkewToleranceSeconds = 0,
            DatabaseConnectionString = "connectionstring",
            FailedLoginAttemptThreshold = 5,
            AccountLockoutDurationMinutes = 30,
            SupportedScopes = new List<string> { "scope1", "scope2" },
            SupportedGrantTypes = new List<string> { "authorization_code", "refresh_token" }
        };

        // Act
        var isValid = AuthServerOptionsValidation.IsValid(options);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void EnsureValid_HappyPath_DoesNotThrow()
    {
        // Arrange
        var options = new AuthServerOptions
        {
            IssuerUrl = "https://example.com",
            JwtSigningKey = "secretkey",
            JwtAlgorithm = "HS256",
            AccessTokenLifetimeSeconds = 3600,
            RefreshTokenLifetimeSeconds = 3600,
            AuthorizationCodeLifetimeSeconds = 3600,
            MaxRefreshTokenGenerations = 10,
            ClockSkewToleranceSeconds = 0,
            DatabaseConnectionString = "connectionstring",
            FailedLoginAttemptThreshold = 5,
            AccountLockoutDurationMinutes = 30,
            SupportedScopes = new List<string> { "scope1", "scope2" },
            SupportedGrantTypes = new List<string> { "authorization_code", "refresh_token" }
        };

        // Act and Assert
        AuthServerOptionsValidation.EnsureValid(options);
    }

    [Fact]
    public void Validate_NullInput_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => AuthServerOptionsValidation.Validate(null));
    }

    [Fact]
    public void IsValid_NullInput_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => AuthServerOptionsValidation.IsValid(null));
    }

    [Fact]
    public void EnsureValid_NullInput_ThrowsArgumentNullException()
    {
        // Act and Assert
        Assert.Throws<ArgumentNullException>(() => AuthServerOptionsValidation.EnsureValid(null));
    }

    [Fact]
    public void EnsureValid_InvalidInput_ThrowsArgumentException()
    {
        // Arrange
        var options = new AuthServerOptions
        {
            IssuerUrl = string.Empty,
            JwtSigningKey = "secretkey",
            JwtAlgorithm = "HS256",
            AccessTokenLifetimeSeconds = 3600,
            RefreshTokenLifetimeSeconds = 3600,
            AuthorizationCodeLifetimeSeconds = 3600,
            MaxRefreshTokenGenerations = 10,
            ClockSkewToleranceSeconds = 0,
            DatabaseConnectionString = "connectionstring",
            FailedLoginAttemptThreshold = 5,
            AccountLockoutDurationMinutes = 30,
            SupportedScopes = new List<string> { "scope1", "scope2" },
            SupportedGrantTypes = new List<string> { "authorization_code", "refresh_token" }
        };

        // Act and Assert
        Assert.Throws<ArgumentException>(() => AuthServerOptionsValidation.EnsureValid(options));
    }
}
