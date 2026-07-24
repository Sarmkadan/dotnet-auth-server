using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using DotnetAuthServer.Extensions;
using DotnetAuthServer.Configuration;
using Xunit;

namespace DotnetAuthServer.Tests;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal CreatePrincipal(IEnumerable<Claim> claims)
        => new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

    [Fact]
    public void GetSubject_Returns_Sub_Claim_Value()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(Constants.Claims.Sub, "user-123")
        });

        var result = principal.GetSubject();

        Assert.Equal("user-123", result);
    }

    [Fact]
    public void GetSubject_Returns_Null_When_Claim_Missing()
    {
        var principal = CreatePrincipal(Array.Empty<Claim>());

        var result = principal.GetSubject();

        Assert.Null(result);
    }

    [Fact]
    public void GetEmail_Returns_Email_Claim_Value()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(Constants.Claims.Email, "alice@example.com")
        });

        var result = principal.GetEmail();

        Assert.Equal("alice@example.com", result);
    }

    [Fact]
    public void IsEmailVerified_Returns_True_When_Claim_Is_True()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(Constants.Claims.EmailVerified, "true")
        });

        var result = principal.IsEmailVerified();

        Assert.True(result);
    }

    [Fact]
    public void IsEmailVerified_Returns_False_When_Claim_Is_Missing_Or_False()
    {
        var missing = CreatePrincipal(Array.Empty<Claim>());
        var falseClaim = CreatePrincipal(new[]
        {
            new Claim(Constants.Claims.EmailVerified, "false")
        });

        Assert.False(missing.IsEmailVerified());
        Assert.False(falseClaim.IsEmailVerified());
    }

    [Fact]
    public void GetRoles_Returns_All_Role_Claims()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(Constants.Claims.Roles, "admin"),
            new Claim(Constants.Claims.Roles, "user")
        });

        var roles = principal.GetRoles().ToList();

        Assert.Equal(2, roles.Count);
        Assert.Contains("admin", roles);
        Assert.Contains("user", roles);
    }

    [Fact]
    public void HasRole_Is_Case_Insensitive()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(Constants.Claims.Roles, "Admin")
        });

        Assert.True(principal.HasRole("admin"));
        Assert.False(principal.HasRole("manager"));
    }

    [Fact]
    public void GetTokenSubject_Extracts_Sub_From_Identity()
    {
        var identity = new ClaimsIdentity(new[]
        {
            new Claim(Constants.Claims.Sub, "token-subject")
        }, "TestAuth");

        var result = identity.GetTokenSubject();

        Assert.Equal("token-subject", result);
    }

    [Fact]
    public void GetAudience_Returns_Aud_Claim_Value()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(Constants.Claims.Aud, "client-xyz")
        });

        var result = principal.GetAudience();

        Assert.Equal("client-xyz", result);
    }

    [Fact]
    public void GetScopes_Parses_Space_Separated_Scopes()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(Constants.Claims.Scope, "openid profile email")
        });

        var scopes = principal.GetScopes().ToList();

        Assert.Equal(3, scopes.Count);
        Assert.Contains("openid", scopes);
        Assert.Contains("profile", scopes);
        Assert.Contains("email", scopes);
    }

    [Fact]
    public void GetScopes_Returns_Empty_When_No_Scope_Claim()
    {
        var principal = CreatePrincipal(Array.Empty<Claim>());

        var scopes = principal.GetScopes();

        Assert.Empty(scopes);
    }

    [Fact]
    public void HasScope_Returns_True_If_Scope_Present()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(Constants.Claims.Scope, "read write")
        });

        Assert.True(principal.HasScope("read"));
        Assert.False(principal.HasScope("delete"));
    }

    [Fact]
    public void GetIssuedAt_Returns_Null_When_Invalid_Value()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(Constants.Claims.Iat, "not-a-number")
        });

        var result = principal.GetIssuedAt();

        Assert.Null(result);
    }

    [Fact]
    public void GetIssuedAt_Returns_Value_When_Valid()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(Constants.Claims.Iat, "1620000000")
        });

        var result = principal.GetIssuedAt();

        Assert.Equal(1620000000L, result);
    }

    [Fact]
    public void GetExpiration_Returns_Value_When_Valid()
    {
        var principal = CreatePrincipal(new[]
        {
            new Claim(Constants.Claims.Exp, "1620003600")
        });

        var result = principal.GetExpiration();

        Assert.Equal(1620003600L, result);
    }

    [Fact]
    public void GetExpiration_Returns_Null_When_Missing()
    {
        var principal = CreatePrincipal(Array.Empty<Claim>());

        var result = principal.GetExpiration();

        Assert.Null(result);
    }

    [Fact]
    public void Methods_Throw_ArgumentNullException_When_Principal_Is_Null()
    {
        ClaimsPrincipal nullPrincipal = null!;

        Assert.Throws<ArgumentNullException>(() => nullPrincipal!.GetSubject());
        Assert.Throws<ArgumentNullException>(() => nullPrincipal!.GetEmail());
        Assert.Throws<ArgumentNullException>(() => nullPrincipal!.IsEmailVerified());
        Assert.Throws<ArgumentNullException>(() => nullPrincipal!.GetRoles());
        Assert.Throws<ArgumentNullException>(() => nullPrincipal!.HasRole("any"));
        Assert.Throws<ArgumentNullException>(() => nullPrincipal!.GetAudience());
        Assert.Throws<ArgumentNullException>(() => nullPrincipal!.GetScopes());
        Assert.Throws<ArgumentNullException>(() => nullPrincipal!.HasScope("any"));
        Assert.Throws<ArgumentNullException>(() => nullPrincipal!.GetIssuedAt());
        Assert.Throws<ArgumentNullException>(() => nullPrincipal!.GetExpiration());
    }

    [Fact]
    public void HasRole_Throws_ArgumentNullException_When_Role_Is_Null()
    {
        var principal = CreatePrincipal(Array.Empty<Claim>());

        Assert.Throws<ArgumentNullException>(() => principal.HasRole(null!));
    }

    [Fact]
    public void HasScope_Throws_ArgumentNullException_When_Scope_Is_Null()
    {
        var principal = CreatePrincipal(Array.Empty<Claim>());

        Assert.Throws<ArgumentNullException>(() => principal.HasScope(null!));
    }

    [Fact]
    public void GetTokenSubject_Throws_ArgumentNullException_When_Identity_Is_Null()
    {
        ClaimsIdentity nullIdentity = null!;

        Assert.Throws<ArgumentNullException>(() => nullIdentity!.GetTokenSubject());
    }
}
