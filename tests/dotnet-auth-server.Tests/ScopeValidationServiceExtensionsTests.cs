#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Tests;

using DotnetAuthServer.Caching;
using DotnetAuthServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

/// <summary>
/// Tests for the <see cref="ScopeValidationServiceExtensions"/> class.
/// </summary>
public sealed class ScopeValidationServiceExtensionsTests
{
    /// <summary>
    /// Builds a <see cref="ScopeValidationService"/> instance with mocked dependencies.
    /// </summary>
    /// <returns>A <see cref="ScopeValidationService"/> instance.</returns>
    private static ScopeValidationService BuildScopeValidationService()
    {
        var scopeService = new ScopeService(new Mock<IScopeRepository>().Object);
        var cacheMock = new Mock<ICacheService>();
        var loggerMock = new Mock<ILogger<ScopeValidationService>>();

        return new ScopeValidationService(scopeService, cacheMock.Object, loggerMock.Object);
    }

    [Fact]
    public void ContainsAnyRequiredScope_OverlappingScope_ReturnsTrueIgnoringCase()
    {
        // Arrange
        var service = BuildScopeValidationService();
        var requested = new[] { "OpenID", "profile" };

        // Act
        var result = service.ContainsAnyRequiredScope(requested, "email", "openid");

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void ContainsAnyRequiredScope_NoOverlapOrNoRequiredScopes_ReturnsFalse()
    {
        // Arrange
        var service = BuildScopeValidationService();
        var requested = new[] { "openid" };

        // Act
        var noOverlap = service.ContainsAnyRequiredScope(requested, "email");
        var noRequired = service.ContainsAnyRequiredScope(requested);

        // Assert
        noOverlap.Should().BeFalse();
        noRequired.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateScopesWithResultsAsync_MixedScopes_PartitionsValidAndInvalid()
    {
        // Arrange
        var service = BuildScopeValidationService();
        var scopes = new[] { "openid", "", "  ", "read:data" };

        // Act
        var (valid, invalid) = await service.ValidateScopesWithResultsAsync(scopes);

        // Assert
        valid.Should().Equal("openid", "read:data");
        invalid.Should().Equal("", "  ");
    }

    [Fact]
    public async Task ValidateScopesWithResultsAsync_NullScopes_ThrowsArgumentNullException()
    {
        // Arrange
        var service = BuildScopeValidationService();

        // Act
        var act = () => service.ValidateScopesWithResultsAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("scopes");
    }

    [Fact]
    public void IsStandardScopesOnly_OnlyStandardScopes_ReturnsTrueIgnoringCase()
    {
        // Arrange
        var service = BuildScopeValidationService();

        // Act
        var result = service.IsStandardScopesOnly("OpenID PROFILE offline_access");

        // Assert
        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("openid custom:write")]
    public void IsStandardScopesOnly_NullBlankOrCustomScope_ReturnsFalse(string? scopeString)
    {
        // Arrange
        var service = BuildScopeValidationService();

        // Act
        var result = service.IsStandardScopesOnly(scopeString);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetAddedScopes_CurrentNotInPrevious_ReturnsDifferenceIgnoringCase()
    {
        // Arrange
        var service = BuildScopeValidationService();
        var current = new[] { "openid", "profile", "email" };
        var previous = new[] { "OPENID", "profile" };

        // Act
        var added = service.GetAddedScopes(current, previous);

        // Assert
        added.Should().BeEquivalentTo(new[] { "email" });
    }

    [Fact]
    public void GetAddedScopes_NullPreviousScopes_ThrowsArgumentNullException()
    {
        // Arrange
        var service = BuildScopeValidationService();
        var current = new[] { "openid" };

        // Act
        var act = () => service.GetAddedScopes(current, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("previousScopes");
    }
}
