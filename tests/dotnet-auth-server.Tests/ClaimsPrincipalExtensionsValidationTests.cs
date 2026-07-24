#nullable enable
using System;
using System.Collections.Generic;
using System.Security.Claims;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Extensions;
using Xunit;

namespace DotnetAuthServer.Tests;

public class ClaimsPrincipalExtensionsValidationTests
{
    [Fact]
    public void Validate_WithNullPrincipal_ThrowsArgumentNullException()
    {
        // Arrange
        ClaimsPrincipal? nullPrincipal = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullPrincipal!.Validate());
    }

    [Fact]
    public void Validate_WithValidPrincipal_ReturnsEmptyList()
    {
        // Arrange
        var principal = CreateValidPrincipal();

        // Act
        var result = principal.Validate();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Validate_WithMissingSubjectClaim_AddsProblem()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = principal.Validate();

        // Assert
        Assert.Single(result);
        Assert.Contains("Subject claim is missing or empty", result);
    }

    [Fact]
    public void Validate_WithEmptySubjectClaim_AddsProblem()
    {
        // Arrange - Only subject claim with whitespace
        var identity = new ClaimsIdentity(new[] { new Claim(Constants.Claims.Sub, "   ") });
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.Validate();

        // Assert - Should only have the subject problem
        Assert.Single(result);
        Assert.Contains("Subject claim is missing or empty", result);
    }

    [Fact]
    public void Validate_WithValidSubjectClaim_NoSubjectProblem()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] { new Claim("sub", "user123") });
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.Validate();

        // Assert
        Assert.DoesNotContain("Subject claim is missing or empty", result);
    }

    [Fact]
    public void Validate_WithEmptyRolesCollection_AddsProblem()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] { new Claim("sub", "user123") });
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.Validate();

        // Assert
        Assert.Contains("No roles claims found", result);
    }

    [Fact]
    public void Validate_WithEmptyRoleString_AddsProblem()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] {
            new Claim("sub", "user123"),
            new Claim("role", "")
        });
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.Validate();

        // Assert
        Assert.Contains("Role claim contains empty string", result);
    }

    [Fact]
    public void Validate_WithValidRoles_NoRoleProblems()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] {
            new Claim("sub", "user123"),
            new Claim("role", "admin"),
            new Claim("role", "user")
        });
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.Validate();

        // Assert
        Assert.DoesNotContain("No roles claims found", result);
        Assert.DoesNotContain("Role claim contains empty string", result);
    }

    [Fact]
    public void Validate_WithEmptyScopesCollection_AddsProblem()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] { new Claim("sub", "user123") });
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.Validate();

        // Assert
        Assert.Contains("No scope claims found", result);
    }

    [Fact]
    public void Validate_WithEmptyScopeString_AddsProblem()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] {
            new Claim("sub", "user123"),
            new Claim("scope", "")
        });
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.Validate();

        // Assert
        Assert.Contains("Scope claim contains empty string", result);
    }

    [Fact]
    public void Validate_WithValidScopes_NoScopeProblems()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] {
            new Claim("sub", "user123"),
            new Claim("scope", "openid"),
            new Claim("scope", "profile")
        });
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.Validate();

        // Assert
        Assert.DoesNotContain("No scope claims found", result);
        Assert.DoesNotContain("Scope claim contains empty string", result);
    }

    [Fact]
    public void Validate_WithMissingExpiration_AddsProblem()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] { new Claim("sub", "user123") });
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.Validate();

        // Assert
        Assert.Contains("Expiration timestamp is missing", result);
    }

    [Fact]
    public void Validate_WithInvalidExpiration_AddsProblem()
    {
        // Arrange
        var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var identity = new ClaimsIdentity(new[] {
            new Claim("sub", "user123"),
            new Claim("exp", "0") // Invalid expiration
        });
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.Validate();

        // Assert
        Assert.Contains("Expiration timestamp is invalid", result);
    }

    [Fact]
    public void Validate_WithExpirationInPast_AddsProblem()
    {
        // Arrange
        var pastTimestamp = DateTimeOffset.UtcNow.AddDays(-2).ToUnixTimeSeconds();
        var identity = new ClaimsIdentity(new[] {
            new Claim("sub", "user123"),
            new Claim("exp", pastTimestamp.ToString())
        });
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.Validate();

        // Assert
        Assert.Contains("Expiration timestamp is in the past", result);
    }

    [Fact]
    public void Validate_WithExpirationBeforeIssuedAt_AddsProblem()
    {
        // Arrange
        var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expiration = DateTimeOffset.UtcNow.AddDays(-1).ToUnixTimeSeconds(); // Yesterday
        var identity = new ClaimsIdentity(new[] {
            new Claim("sub", "user123"),
            new Claim("iat", issuedAt.ToString()),
            new Claim("exp", expiration.ToString())
        });
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.Validate();

        // Assert
        Assert.Contains("Expiration timestamp is before IssuedAt timestamp", result);
    }

    [Fact]
    public void Validate_WithEmptyEmail_NoEmailProblem()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] {
            new Claim("sub", "user123"),
            new Claim("email", "")
        });
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.Validate();

        // Assert - Empty email should not cause a problem (null check only)
        Assert.DoesNotContain("Email claim is empty", result);
    }

    [Fact]
    public void Validate_WithInvalidEmailVerifiedFormat_AddsProblem()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] {
            new Claim("sub", "user123"),
            new Claim("email_verified", "not-a-boolean")
        });
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.Validate();

        // Assert
        Assert.Contains("EmailVerified claim has invalid boolean format", result);
    }

    [Fact]
    public void IsValid_WithNullPrincipal_ThrowsArgumentNullException()
    {
        // Arrange
        ClaimsPrincipal? nullPrincipal = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullPrincipal!.IsValid());
    }

    [Fact]
    public void IsValid_WithValidPrincipal_ReturnsTrue()
    {
        // Arrange
        var principal = CreateValidPrincipal();

        // Act
        var result = principal.IsValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithInvalidPrincipal_ReturnsFalse()
    {
        // Arrange
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        // Act
        var result = principal.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void EnsureValid_WithNullPrincipal_ThrowsArgumentNullException()
    {
        // Arrange
        ClaimsPrincipal? nullPrincipal = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => nullPrincipal!.EnsureValid());
    }

    [Fact]
    public void EnsureValid_WithValidPrincipal_DoesNotThrow()
    {
        // Arrange
        var principal = CreateValidPrincipal();

        // Act
        var exception = Record.Exception(() => principal.EnsureValid());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void EnsureValid_WithInvalidPrincipal_ThrowsArgumentException()
    {
        // Arrange
        var identity = new ClaimsIdentity();
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => principal.EnsureValid());
        Assert.Contains("validation failed", exception.Message);
    }

    [Fact]
    public void EnsureValid_WithMultipleProblems_ContainsAllProblemsInMessage()
    {
        // Arrange
        var identity = new ClaimsIdentity(new[] {
            new Claim("sub", "   "), // Empty subject
            new Claim("role", "") // Empty role
        });
        var principal = new ClaimsPrincipal(identity);

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => principal.EnsureValid());
        Assert.Contains("Subject claim is missing or empty", exception.Message);
        Assert.Contains("Role claim contains empty string", exception.Message);
    }

    private static ClaimsPrincipal CreateValidPrincipal()
    {
        var issuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var expiration = DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds();

        var identity = new ClaimsIdentity(new[] {
            new Claim(Constants.Claims.Sub, "user123"),
            new Claim(Constants.Claims.Email, "user@example.com"),
            new Claim(Constants.Claims.EmailVerified, "true"),
            new Claim(Constants.Claims.Roles, "admin"),
            new Claim(Constants.Claims.Roles, "user"),
            new Claim(Constants.Claims.Scope, "openid profile"),
            new Claim(Constants.Claims.Aud, "test-audience"),
            new Claim(Constants.Claims.Iat, issuedAt.ToString()),
            new Claim(Constants.Claims.Exp, expiration.ToString()),
            new Claim(Constants.Claims.Sub, "token123")
        });

        return new ClaimsPrincipal(identity);
    }
}