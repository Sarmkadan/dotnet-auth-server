#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using DotnetAuthServer.Domain.Entities;
using Xunit;

namespace DotnetAuthServer.Tests;

public class ScopeValidationTests
{
    [Fact]
    public void Validate_ReturnsEmptyList_WhenScopeIsValid()
    {
        // Arrange
        var scope = new Scope
        {
            ScopeId = "scope-id",
            DisplayName = "display-name",
            Description = "description",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IdTokenClaims = new List<string> { "claim1", "claim2" },
            AccessTokenClaims = new List<string> { "claim3", "claim4" },
            AllowedRoles = new List<string> { "role1", "role2" }
        };

        // Act
        var problems = ScopeValidation.Validate(scope);

        // Assert
        Assert.Empty(problems);
    }

    [Fact]
    public void Validate_ReturnsListWithErrors_WhenScopeIsInvalid()
    {
        // Arrange
        var scope = new Scope
        {
            ScopeId = "",
            DisplayName = "",
            Description = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IdTokenClaims = new List<string>(),
            AccessTokenClaims = new List<string>(),
            AllowedRoles = new List<string>()
        };

        // Act
        var problems = ScopeValidation.Validate(scope);

        // Assert
        Assert.Single(problems);
    }

    [Fact]
    public void IsValid_ReturnsTrue_WhenScopeIsValid()
    {
        // Arrange
        var scope = new Scope
        {
            ScopeId = "scope-id",
            DisplayName = "display-name",
            Description = "description",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IdTokenClaims = new List<string> { "claim1", "claim2" },
            AccessTokenClaims = new List<string> { "claim3", "claim4" },
            AllowedRoles = new List<string> { "role1", "role2" }
        };

        // Act
        var isValid = ScopeValidation.IsValid(scope);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void IsValid_ReturnsFalse_WhenScopeIsInvalid()
    {
        // Arrange
        var scope = new Scope
        {
            ScopeId = "",
            DisplayName = "",
            Description = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IdTokenClaims = new List<string>(),
            AccessTokenClaims = new List<string>(),
            AllowedRoles = new List<string>()
        };

        // Act
        var isValid = ScopeValidation.IsValid(scope);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void EnsureValid_ThrowsArgumentException_WhenScopeIsInvalid()
    {
        // Arrange
        var scope = new Scope
        {
            ScopeId = "",
            DisplayName = "",
            Description = "",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IdTokenClaims = new List<string>(),
            AccessTokenClaims = new List<string>(),
            AllowedRoles = new List<string>()
        };

        // Act and Assert
        Assert.Throws<ArgumentException>(() => ScopeValidation.EnsureValid(scope));
    }
}
