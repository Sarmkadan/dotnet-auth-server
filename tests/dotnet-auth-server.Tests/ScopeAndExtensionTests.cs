// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Tests;

using DotnetAuthServer.Caching;
using DotnetAuthServer.Extensions;
using DotnetAuthServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

public class ScopeAndExtensionTests
{
    // -------------------------------------------------------------------------
    // ScopeValidationService — pure methods tested via mocked dependencies
    // -------------------------------------------------------------------------

    private static ScopeValidationService BuildScopeValidationService()
    {
        // IScopeRepository is an interface, so Moq resolves it cleanly
        var repoMock = new Mock<IScopeRepository>();
        var scopeService = new ScopeService(repoMock.Object);
        var cacheMock = new Mock<ICacheService>();
        var loggerMock = new Mock<ILogger<ScopeValidationService>>();

        return new ScopeValidationService(scopeService, cacheMock.Object, loggerMock.Object);
    }

    [Fact]
    public void ContainsRequiredScopes_WhenOpenIdScopeAbsentAndOidcRequired_ReturnsFalse()
    {
        // Arrange
        var service = BuildScopeValidationService();
        var scopesWithoutOpenId = new[] { "profile", "email" };

        // Act
        var result = service.ContainsRequiredScopes(scopesWithoutOpenId, isOidc: true);

        // Assert
        result.Should().BeFalse(
            "OIDC requests must include 'openid' scope; without it the ID token cannot be issued");
    }

    [Fact]
    public void ContainsRequiredScopes_WhenOpenIdScopePresentAndOidcRequired_ReturnsTrue()
    {
        // Arrange
        var service = BuildScopeValidationService();
        var scopes = new[] { "openid", "profile" };

        // Act
        var result = service.ContainsRequiredScopes(scopes, isOidc: true);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void MergeScopes_WithOverlappingLists_DeduplicatesAndSortsAlphabetically()
    {
        // Arrange
        var service = BuildScopeValidationService();
        var list1 = new[] { "profile", "openid" };
        var list2 = new[] { "email", "openid" };   // "openid" appears in both

        // Act
        var merged = service.MergeScopes(list1, list2);

        // Assert
        merged.Should().Be("email openid profile",
            "scopes must be sorted alphabetically and deduplicated so token comparisons are stable");
    }

    [Fact]
    public void FilterScopes_ReturnsOnlyIntersectionOfGrantedAndRequestedScopes()
    {
        // Arrange
        var service = BuildScopeValidationService();
        var grantedScopes = new[] { "openid", "profile", "email" };
        var requestedScopes = new[] { "profile", "offline_access" };  // offline_access not granted

        // Act
        var filtered = service.FilterScopes(grantedScopes, requestedScopes).ToList();

        // Assert
        filtered.Should().ContainSingle()
            .Which.Should().Be("profile",
                "only scopes that are both granted and requested should be issued");
    }

    // -------------------------------------------------------------------------
    // StringExtensions
    // -------------------------------------------------------------------------

    [Fact]
    public void ParseScopes_WithDuplicatesAndExtraWhitespace_ReturnsDistinctNonEmptyScopes()
    {
        // Arrange
        const string scopeString = "openid  profile openid email  profile";

        // Act
        var scopes = scopeString.ParseScopes().ToList();

        // Assert
        scopes.Should().BeEquivalentTo(["openid", "profile", "email"],
            "duplicates and extra spaces must be removed regardless of order");
        scopes.Should().HaveCount(3);
    }

    [Fact]
    public void ParseScopes_WithNullInput_ReturnsEmptyCollection()
    {
        // Arrange
        string? nullScope = null;

        // Act
        var scopes = nullScope.ParseScopes();

        // Assert
        scopes.Should().BeEmpty("a null scope string represents an absent scope parameter");
    }

    [Fact]
    public void IsValidAbsoluteUri_WithHttpsUri_ReturnsTrue()
    {
        // Arrange
        const string uri = "https://auth.example.com/callback";

        // Act & Assert
        uri.IsValidAbsoluteUri().Should().BeTrue();
    }

    [Fact]
    public void IsValidAbsoluteUri_WithRelativeUri_ReturnsFalse()
    {
        // Arrange — redirect URIs must always be absolute in OAuth2
        const string relativeUri = "/callback";

        // Act & Assert
        relativeUri.IsValidAbsoluteUri().Should().BeFalse(
            "relative URIs are not permitted as redirect targets per RFC 6749");
    }

    // -------------------------------------------------------------------------
    // DateTimeExtensions
    // -------------------------------------------------------------------------

    [Fact]
    public void IsExpired_WhenExpirationFallsWithinFiveSecondClockSkewBuffer_ReturnsTrue()
    {
        // Arrange — expires 3 seconds from now, inside the 5-second tolerance window
        var expiresAt = DateTime.UtcNow.AddSeconds(3);

        // Act
        var expired = expiresAt.IsExpired();

        // Assert
        expired.Should().BeTrue(
            "tokens expiring within 5 seconds are treated as expired to prevent race conditions at validation time");
    }

    [Fact]
    public void IsExpired_WhenExpirationIsFarInFuture_ReturnsFalse()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        // Act & Assert
        expiresAt.IsExpired().Should().BeFalse();
    }

    [Fact]
    public void ToUnixTimestamp_WithKnownEpochDate_ReturnsZero()
    {
        // Arrange — Unix epoch itself must produce 0
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act
        var timestamp = epoch.ToUnixTimestamp();

        // Assert
        timestamp.Should().Be(0L, "Unix epoch is the zero point for all timestamp calculations");
    }

    [Fact]
    public void AddLifetime_WithNegativeSeconds_ClampsToBaseTime()
    {
        // Arrange
        var baseTime = new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc);

        // Act — negative lifetime should not wind time backwards
        var result = baseTime.AddLifetime(-100);

        // Assert
        result.Should().Be(baseTime,
            "Math.Max(0, lifetimeSeconds) must guard against negative lifetimes producing past expiry times");
    }
}
