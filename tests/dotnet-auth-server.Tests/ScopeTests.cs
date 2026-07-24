#nullable enable
using System;
using Xunit;
using DotnetAuthServer.Domain.Entities;

namespace DotnetAuthServer.Tests;

public class ScopeTests
{
    [Fact]
    public void IsValid_ReturnsTrue_WhenAllPropertiesAreValid()
    {
        // Arrange
        var scope = new Scope
        {
            ScopeId = "openid",
            DisplayName = "OpenID Connect",
            Description = "OpenID Connect scope",
            IsRequired = true,
            RequiresConsent = true,
            IsOpenIdScope = true,
            IsActive = true,
            IdTokenClaims = new[] { "openid" },
            AccessTokenClaims = new[] { "openid" },
            AllowedRoles = new[] { "admin" }
        };

        // Act
        var result = scope.IsValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenScopeIdIsEmpty()
    {
        // Arrange
        var scope = new Scope
        {
            ScopeId = "",
            DisplayName = "OpenID Connect",
            Description = "OpenID Connect scope",
            IsRequired = true,
            RequiresConsent = true,
            IsOpenIdScope = true,
            IsActive = true,
            IdTokenClaims = new[] { "openid" },
            AccessTokenClaims = new[] { "openid" },
            AllowedRoles = new[] { "admin" }
        };

        // Act
        var result = scope.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenDisplayNameIsEmpty()
    {
        // Arrange
        var scope = new Scope
        {
            ScopeId = "openid",
            DisplayName = "",
            Description = "OpenID Connect scope",
            IsRequired = true,
            RequiresConsent = true,
            IsOpenIdScope = true,
            IsActive = true,
            IdTokenClaims = new[] { "openid" },
            AccessTokenClaims = new[] { "openid" },
            AllowedRoles = new[] { "admin" }
        };

        // Act
        var result = scope.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenDescriptionIsEmpty()
    {
        // Arrange
        var scope = new Scope
        {
            ScopeId = "openid",
            DisplayName = "OpenID Connect",
            Description = "",
            IsRequired = true,
            RequiresConsent = true,
            IsOpenIdScope = true,
            IsActive = true,
            IdTokenClaims = new[] { "openid" },
            AccessTokenClaims = new[] { "openid" },
            AllowedRoles = new[] { "admin" }
        };

        // Act
        var result = scope.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanUserAccessScope_ReturnsTrue_WhenUserHasAllowedRole()
    {
        // Arrange
        var scope = new Scope
        {
            ScopeId = "openid",
            DisplayName = "OpenID Connect",
            Description = "OpenID Connect scope",
            IsRequired = true,
            RequiresConsent = true,
            IsOpenIdScope = true,
            IsActive = true,
            IdTokenClaims = new[] { "openid" },
            AccessTokenClaims = new[] { "openid" },
            AllowedRoles = new[] { "admin" }
        };
        var userRoles = new[] { "admin" };

        // Act
        var result = scope.CanUserAccessScope(userRoles);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanUserAccessScope_ReturnsFalse_WhenUserDoesNotHaveAllowedRole()
    {
        // Arrange
        var scope = new Scope
        {
            ScopeId = "openid",
            DisplayName = "OpenID Connect",
            Description = "OpenID Connect scope",
            IsRequired = true,
            RequiresConsent = true,
            IsOpenIdScope = true,
            IsActive = true,
            IdTokenClaims = new[] { "openid" },
            AccessTokenClaims = new[] { "openid" },
            AllowedRoles = new[] { "admin" }
        };
        var userRoles = new[] { "user" };

        // Act
        var result = scope.CanUserAccessScope(userRoles);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetAllClaims_ReturnsAllClaims()
    {
        // Arrange
        var scope = new Scope
        {
            ScopeId = "openid",
            DisplayName = "OpenID Connect",
            Description = "OpenID Connect scope",
            IsRequired = true,
            RequiresConsent = true,
            IsOpenIdScope = true,
            IsActive = true,
            IdTokenClaims = new[] { "openid", "profile" },
            AccessTokenClaims = new[] { "openid", "profile" },
            AllowedRoles = new[] { "admin" }
        };

        // Act
        var result = scope.GetAllClaims();

        // Assert
        Assert.Equal(new[] { "openid", "profile" }, result);
    }
}
