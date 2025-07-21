#nullable enable
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

/// <summary>
/// Tests for the <see cref="ScopeAndExtensionTests"/> class.
/// </summary>
public sealed class ScopeAndExtensionTests
{
    /// <summary>
    /// Builds a <see cref="ScopeValidationService"/> instance with mocked dependencies.
    /// </summary>
    /// <returns>A <see cref="ScopeValidationService"/> instance.</returns>
    private static ScopeValidationService BuildScopeValidationService()
    {
        // IScopeRepository is an interface, so Moq resolves it cleanly
        var repoMock = new Mock<IScopeRepository>();
        var scopeService = new ScopeService(repoMock.Object);
        var cacheMock = new Mock<ICacheService>();
        var loggerMock = new Mock<ILogger<ScopeValidationService>>();

        return new ScopeValidationService(scopeService, cacheMock.Object, loggerMock.Object);
    }

    /// <summary>
    /// Verifies that <see cref="ScopeValidationService.ContainsRequiredScopes"/> returns false when the OIDC scope is absent and OIDC is required.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ScopeValidationService.ContainsRequiredScopes"/> returns true when the OIDC scope is present and OIDC is required.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ScopeValidationService.MergeScopes"/> merges overlapping lists and deduplicates scopes, sorting them alphabetically.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="ScopeValidationService.FilterScopes"/> returns only the intersection of granted and requested scopes.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ParseScopes"/> returns distinct non-empty scopes when given a string with duplicates and extra whitespace.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="StringExtensions.ParseScopes"/> returns an empty collection when given a null input.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="StringExtensions.IsValidAbsoluteUri"/> returns true for an HTTPS URI.
    /// </summary>
    [Fact]
    public void IsValidAbsoluteUri_WithHttpsUri_ReturnsTrue()
    {
        // Arrange
        const string uri = "https://auth.example.com/callback";

        // Act & Assert
        uri.IsValidAbsoluteUri().Should().BeTrue();
    }

    /// <summary>
    /// Verifies that <see cref="StringExtensions.IsValidAbsoluteUri"/> returns false for a relative URI.
    /// </summary>
    [Fact]
    public void IsValidAbsoluteUri_WithRelativeUri_ReturnsFalse()
    {
        // Arrange — redirect URIs must always be absolute in OAuth2
        const string relativeUri = "/callback";

        // Act & Assert
        relativeUri.IsValidAbsoluteUri().Should().BeFalse(
            "relative URIs are not permitted as redirect targets per RFC 6749");
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsExpired"/> returns true when the expiration falls within a 5-second clock skew buffer.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.IsExpired"/> returns false when the expiration is far in the future.
    /// </summary>
    [Fact]
    public void IsExpired_WhenExpirationIsFarInFuture_ReturnsFalse()
    {
        // Arrange
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        // Act & Assert
        expiresAt.IsExpired().Should().BeFalse();
    }

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.ToUnixTimestamp"/> returns zero for the Unix epoch date.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="DateTimeExtensions.AddLifetime"/> clamps negative lifetimes to the base time.
    /// </summary>
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
