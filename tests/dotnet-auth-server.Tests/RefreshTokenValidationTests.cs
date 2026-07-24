#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DotnetAuthServer.Domain.Entities;
using Xunit;

namespace DotnetAuthServer.Tests;

public class RefreshTokenValidationTests
{
    [Fact]
    public void Validate_ReturnsEmptyList_WhenRefreshTokenIsValid()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            TokenId = "token-id",
            TokenHash = "token-hash",
            ClientId = "client-id",
            UserId = "user-id",
            GrantedScopes = "scopes",
            Version = 1,
            UsageCount = 0,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RevokedAt = null,
            LastUsedAt = null
        };

        // Act
        var errors = RefreshTokenValidation.Validate(refreshToken);

        // Assert
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_ReturnsListWithErrors_WhenRefreshTokenIsInvalid()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            TokenId = "",
            TokenHash = "",
            ClientId = "",
            UserId = "",
            GrantedScopes = "",
            Version = 0,
            UsageCount = -1,
            ExpiresAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RevokedAt = null,
            LastUsedAt = null
        };

        // Act
        var errors = RefreshTokenValidation.Validate(refreshToken);

        // Assert
        Assert.Single(errors);
    }

    [Fact]
    public void IsValid_ReturnsTrue_WhenRefreshTokenIsValid()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            TokenId = "token-id",
            TokenHash = "token-hash",
            ClientId = "client-id",
            UserId = "user-id",
            GrantedScopes = "scopes",
            Version = 1,
            UsageCount = 0,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RevokedAt = null,
            LastUsedAt = null
        };

        // Act
        var isValid = RefreshTokenValidation.IsValid(refreshToken);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenRefreshTokenIsInvalid()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            TokenId = "",
            TokenHash = "",
            ClientId = "",
            UserId = "",
            GrantedScopes = "",
            Version = 0,
            UsageCount = -1,
            ExpiresAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RevokedAt = null,
            LastUsedAt = null
        };

        // Act
        var isValid = RefreshTokenValidation.IsValid(refreshToken);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void EnsureValid_ThrowsArgumentException_WhenRefreshTokenIsInvalid()
    {
        // Arrange
        var refreshToken = new RefreshToken
        {
            TokenId = "",
            TokenHash = "",
            ClientId = "",
            UserId = "",
            GrantedScopes = "",
            Version = 0,
            UsageCount = -1,
            ExpiresAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RevokedAt = null,
            LastUsedAt = null
        };

        // Act and Assert
        Assert.Throws<ArgumentException>(() => RefreshTokenValidation.EnsureValid(refreshToken));
    }
}
