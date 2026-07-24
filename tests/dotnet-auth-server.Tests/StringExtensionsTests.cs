#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Tests;

using DotnetAuthServer.Extensions;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for StringExtensions class covering scope parsing, URI validation,
/// URL safety checks, and string manipulation utilities.
/// </summary>
public sealed class StringExtensionsTests
{
    // -------------------------------------------------------------------------
    // ParseScopes tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that ParseScopes correctly handles a valid space-delimited scope string.
    /// </summary>
    [Fact]
    public void ParseScopes_WithValidScopes_ReturnsDistinctScopes()
    {
        // Arrange
        const string scopes = "openid profile email offline_access";

        // Act
        var result = scopes.ParseScopes();

        // Assert
        result.Should().BeEquivalentTo(new[] { "openid", "profile", "email", "offline_access" });
    }

    /// <summary>
    /// Verifies that ParseScopes handles null input gracefully.
    /// </summary>
    [Fact]
    public void ParseScopes_WithNullInput_ReturnsEmptyEnumerable()
    {
        // Arrange
        string? scopes = null;

        // Act
        var result = scopes.ParseScopes();

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ParseScopes handles empty string input.
    /// </summary>
    [Fact]
    public void ParseScopes_WithEmptyString_ReturnsEmptyEnumerable()
    {
        // Arrange
        var scopes = string.Empty;

        // Act
        var result = scopes.ParseScopes();

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ParseScopes handles whitespace-only input.
    /// </summary>
    [Fact]
    public void ParseScopes_WithWhitespaceOnly_ReturnsEmptyEnumerable()
    {
        // Arrange
        const string scopes = "   \t  \n  ";

        // Act
        var result = scopes.ParseScopes();

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that ParseScopes removes duplicate scopes.
    /// </summary>
    [Fact]
    public void ParseScopes_WithDuplicateScopes_ReturnsDistinctScopes()
    {
        // Arrange
        const string scopes = "openid profile openid email profile";

        // Act
        var result = scopes.ParseScopes();

        // Assert
        result.Should().BeEquivalentTo(new[] { "openid", "profile", "email" });
    }

    /// <summary>
    /// Verifies that ParseScopes trims whitespace around scope names.
    /// </summary>
    [Fact]
    public void ParseScopes_WithExtraWhitespace_TrimsScopes()
    {
        // Arrange
        const string scopes = "  openid   profile  email  ";

        // Act
        var result = scopes.ParseScopes();

        // Assert
        result.Should().BeEquivalentTo(new[] { "openid", "profile", "email" });
    }

    /// <summary>
    /// Verifies that ParseScopes handles single scope.
    /// </summary>
    [Fact]
    public void ParseScopes_WithSingleScope_ReturnsSingleScope()
    {
        // Arrange
        const string scopes = "openid";

        // Act
        var result = scopes.ParseScopes();

        // Assert
        result.Should().BeEquivalentTo(new[] { "openid" });
    }

    /// <summary>
    /// Verifies that ParseScopes handles empty scopes between delimiters.
    /// </summary>
    [Fact]
    public void ParseScopes_WithEmptyScopesBetweenDelimiters_RemovesEmptyScopes()
    {
        // Arrange
        const string scopes = "openid  profile";

        // Act
        var result = scopes.ParseScopes();

        // Assert
        result.Should().BeEquivalentTo(new[] { "openid", "profile" });
    }

    // -------------------------------------------------------------------------
    // JoinScopes tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that JoinScopes correctly joins a collection of scopes.
    /// </summary>
    [Fact]
    public void JoinScopes_WithValidCollection_ReturnsSpaceDelimitedString()
    {
        // Arrange
        var scopes = new[] { "openid", "profile", "email" };

        // Act
        var result = scopes.JoinScopes();

        // Assert
        result.Should().Be("openid profile email");
    }

    /// <summary>
    /// Verifies that JoinScopes handles empty collection.
    /// </summary>
    [Fact]
    public void JoinScopes_WithEmptyCollection_ReturnsEmptyString()
    {
        // Arrange
        var scopes = Array.Empty<string>();

        // Act
        var result = scopes.JoinScopes();

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that JoinScopes handles collection with null elements.
    /// </summary>
    [Fact]
    public void JoinScopes_WithNullElements_FiltersOutNulls()
    {
        // Arrange
        var scopes = new[] { "openid", null, "profile", string.Empty, "email" };

        // Act
        var result = scopes.JoinScopes();

        // Assert
        result.Should().Be("openid profile email");
    }

    /// <summary>
    /// Verifies that JoinScopes handles collection with whitespace-only elements.
    /// </summary>
    [Fact]
    public void JoinScopes_WithWhitespaceElements_FiltersOutWhitespace()
    {
        // Arrange
        var scopes = new[] { "openid", "   ", "profile", "\t", "email" };

        // Act
        var result = scopes.JoinScopes();

        // Assert
        result.Should().Be("openid profile email");
    }

    /// <summary>
    /// Verifies that JoinScopes throws ArgumentNullException for null collection.
    /// </summary>
    [Fact]
    public void JoinScopes_WithNullCollection_ThrowsArgumentNullException()
    {
        // Arrange
        IEnumerable<string>? scopes = null;

        // Act
        Action act = () => scopes!.JoinScopes();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that JoinScopes handles single scope.
    /// </summary>
    [Fact]
    public void JoinScopes_WithSingleScope_ReturnsSingleScope()
    {
        // Arrange
        var scopes = new[] { "openid" };

        // Act
        var result = scopes.JoinScopes();

        // Assert
        result.Should().Be("openid");
    }

    // -------------------------------------------------------------------------
    // IsValidAbsoluteUri tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that IsValidAbsoluteUri accepts valid HTTP URIs.
    /// </summary>
    [Fact]
    public void IsValidAbsoluteUri_WithValidHttpUri_ReturnsTrue()
    {
        // Arrange
        const string uri = "http://example.com/path";

        // Act
        var result = uri.IsValidAbsoluteUri();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsValidAbsoluteUri accepts valid HTTPS URIs.
    /// </summary>
    [Fact]
    public void IsValidAbsoluteUri_WithValidHttpsUri_ReturnsTrue()
    {
        // Arrange
        const string uri = "https://example.com/path";

        // Act
        var result = uri.IsValidAbsoluteUri();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsValidAbsoluteUri rejects null input.
    /// </summary>
    [Fact]
    public void IsValidAbsoluteUri_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        string? uri = null;

        // Act
        Action act = () => uri!.IsValidAbsoluteUri();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that IsValidAbsoluteUri rejects empty string.
    /// </summary>
    [Fact]
    public void IsValidAbsoluteUri_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        var uri = string.Empty;

        // Act
        var result = uri.IsValidAbsoluteUri();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsValidAbsoluteUri rejects whitespace-only string.
    /// </summary>
    [Fact]
    public void IsValidAbsoluteUri_WithWhitespaceOnly_ReturnsFalse()
    {
        // Arrange
        const string uri = "   ";

        // Act
        var result = uri.IsValidAbsoluteUri();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsValidAbsoluteUri rejects relative URIs.
    /// </summary>
    [Fact]
    public void IsValidAbsoluteUri_WithRelativeUri_ReturnsFalse()
    {
        // Arrange
        const string uri = "/path/to/resource";

        // Act
        var result = uri.IsValidAbsoluteUri();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsValidAbsoluteUri rejects non-HTTP/HTTPS schemes.
    /// </summary>
    [Fact]
    public void IsValidAbsoluteUri_WithNonHttpScheme_ReturnsFalse()
    {
        // Arrange
        const string uri = "ftp://example.com/path";

        // Act
        var result = uri.IsValidAbsoluteUri();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsValidAbsoluteUri rejects malformed URIs.
    /// </summary>
    [Fact]
    public void IsValidAbsoluteUri_WithMalformedUri_ReturnsFalse()
    {
        // Arrange
        const string uri = "not a valid uri";

        // Act
        var result = uri.IsValidAbsoluteUri();

        // Assert
        result.Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // UriEquals tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that UriEquals returns true for identical URIs.
    /// </summary>
    [Fact]
    public void UriEquals_WithIdenticalUris_ReturnsTrue()
    {
        // Arrange
        const string uri1 = "https://example.com/path";
        const string uri2 = "https://example.com/path";

        // Act
        var result = uri1.UriEquals(uri2);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that UriEquals returns false for URIs that differ in case.
    /// </summary>
    [Fact]
    public void UriEquals_WithCaseDifferingUris_ReturnsFalse()
    {
        // Arrange
        const string uri1 = "https://Example.Com/Path";
        const string uri2 = "https://example.com/path";

        // Act
        var result = uri1.UriEquals(uri2);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that UriEquals returns false for different URIs.
    /// </summary>
    [Fact]
    public void UriEquals_WithDifferentUris_ReturnsFalse()
    {
        // Arrange
        const string uri1 = "https://example.com/path1";
        const string uri2 = "https://example.com/path2";

        // Act
        var result = uri1.UriEquals(uri2);

        // Assert
        result.Should().BeFalse();
    }


    /// <summary>
    /// Verifies that UriEquals returns true when both URIs are empty.
    /// </summary>
    [Fact]
    public void UriEquals_WithBothEmptyUris_ReturnsTrue()
    {
        // Arrange
        var uri1 = string.Empty;
        var uri2 = string.Empty;

        // Act
        var result = uri1.UriEquals(uri2);

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that UriEquals returns false for URIs with different schemes.
    /// </summary>
    [Fact]
    public void UriEquals_WithDifferentSchemes_ReturnsFalse()
    {
        // Arrange
        const string uri1 = "http://example.com/path";
        const string uri2 = "https://example.com/path";

        // Act
        var result = uri1.UriEquals(uri2);

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that UriEquals throws ArgumentNullException when first URI is null.
    /// </summary>
    [Fact]
    public void UriEquals_WithFirstNullUri_ThrowsArgumentNullException()
    {
        // Arrange
        string? uri1 = null;
        const string uri2 = "https://example.com/path";

        // Act
        Action act = () => uri1!.UriEquals(uri2);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }


    // -------------------------------------------------------------------------
    // IsUrlSafe tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that IsUrlSafe accepts strings with only URL-safe characters.
    /// </summary>
    [Fact]
    public void IsUrlSafe_WithUrlSafeString_ReturnsTrue()
    {
        // Arrange
        const string value = "abc123-ABC_abc.~def";

        // Act
        var result = value.IsUrlSafe();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsUrlSafe rejects strings with unsafe characters.
    /// </summary>
    [Fact]
    public void IsUrlSafe_WithUnsafeCharacters_ReturnsFalse()
    {
        // Arrange
        const string value = "user@domain.com";

        // Act
        var result = value.IsUrlSafe();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsUrlSafe rejects null input.
    /// </summary>
    [Fact]
    public void IsUrlSafe_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        string? value = null;

        // Act
        Action act = () => value!.IsUrlSafe();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that IsUrlSafe rejects empty string.
    /// </summary>
    [Fact]
    public void IsUrlSafe_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        var value = string.Empty;

        // Act
        var result = value.IsUrlSafe();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsUrlSafe rejects whitespace-only string.
    /// </summary>
    [Fact]
    public void IsUrlSafe_WithWhitespaceOnly_ReturnsFalse()
    {
        // Arrange
        const string value = "   ";

        // Act
        var result = value.IsUrlSafe();

        // Assert
        result.Should().BeFalse();
    }

    /// <summary>
    /// Verifies that IsUrlSafe accepts single character.
    /// </summary>
    [Fact]
    public void IsUrlSafe_WithSingleCharacter_ReturnsTrue()
    {
        // Arrange
        const string value = "a";

        // Act
        var result = value.IsUrlSafe();

        // Assert
        result.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsUrlSafe accepts only alphanumeric characters.
    /// </summary>
    [Fact]
    public void IsUrlSafe_WithAlphanumericOnly_ReturnsTrue()
    {
        // Arrange
        const string value = "abc123XYZ789";

        // Act
        var result = value.IsUrlSafe();

        // Assert
        result.Should().BeTrue();
    }

    // -------------------------------------------------------------------------
    // SafeTruncate tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that SafeTruncate returns the original string when length is less than max.
    /// </summary>
    [Fact]
    public void SafeTruncate_WithStringShorterThanMax_ReturnsOriginalString()
    {
        // Arrange
        const string value = "short";
        const int maxLength = 10;

        // Act
        var result = value.SafeTruncate(maxLength);

        // Assert
        result.Should().Be("short");
    }

    /// <summary>
    /// Verifies that SafeTruncate truncates string to max length when needed.
    /// </summary>
    [Fact]
    public void SafeTruncate_WithStringLongerThanMax_TruncatesToMaxLength()
    {
        // Arrange
        const string value = "this is a longer string";
        const int maxLength = 10;

        // Act
        var result = value.SafeTruncate(maxLength);

        // Assert
        result.Should().Be("this is a ");
        result.Length.Should().Be(maxLength);
    }

    /// <summary>
    /// Verifies that SafeTruncate returns empty string when max length is 0.
    /// </summary>
    [Fact]
    public void SafeTruncate_WithMaxLengthZero_ReturnsEmptyString()
    {
        // Arrange
        const string value = "test";
        const int maxLength = 0;

        // Act
        var result = value.SafeTruncate(maxLength);

        // Assert
        result.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that SafeTruncate throws ArgumentNullException for null input.
    /// </summary>
    [Fact]
    public void SafeTruncate_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        string? value = null;
        const int maxLength = 10;

        // Act
        Action act = () => value!.SafeTruncate(maxLength);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that SafeTruncate throws ArgumentOutOfRangeException for negative max length.
    /// </summary>
    [Fact]
    public void SafeTruncate_WithNegativeMaxLength_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        const string value = "test";
        const int maxLength = -1;

        // Act
        Action act = () => value.SafeTruncate(maxLength);

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Verifies that SafeTruncate handles exact length match.
    /// </summary>
    [Fact]
    public void SafeTruncate_WithExactLengthMatch_ReturnsOriginalString()
    {
        // Arrange
        const string value = "exact";
        const int maxLength = 5;

        // Act
        var result = value.SafeTruncate(maxLength);

        // Assert
        result.Should().Be("exact");
        result.Length.Should().Be(maxLength);
    }

    /// <summary>
    /// Verifies that SafeTruncate handles unicode characters correctly.
    /// </summary>
    [Fact]
    public void SafeTruncate_WithUnicodeCharacters_TruncatesCorrectly()
    {
        // Arrange
        const string value = "Hello 世界";
        const int maxLength = 5;

        // Act
        var result = value.SafeTruncate(maxLength);

        // Assert
        result.Should().Be("Hello");
        result.Length.Should().Be(maxLength);
    }

    // -------------------------------------------------------------------------
    // MaskSensitive tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// Verifies that MaskSensitive masks strings longer than 8 characters.
    /// </summary>
    [Fact]
    public void MaskSensitive_WithLongString_MasksMiddleCharacters()
    {
        // Arrange
        const string value = "sensitiveData123";

        // Act
        var result = value.MaskSensitive();

        // Assert
        result.Should().Be("sen***123");
    }

    /// <summary>
    /// Verifies that MaskSensitive returns asterisks for strings 8 characters or shorter.
    /// </summary>
    [Fact]
    public void MaskSensitive_WithShortString_ReturnsAsterisks()
    {
        // Arrange
        const string value = "secret";

        // Act
        var result = value.MaskSensitive();

        // Assert
        result.Should().Be("***");
    }

    /// <summary>
    /// Verifies that MaskSensitive returns asterisks for empty string.
    /// </summary>
    [Fact]
    public void MaskSensitive_WithEmptyString_ReturnsAsterisks()
    {
        // Arrange
        var value = string.Empty;

        // Act
        var result = value.MaskSensitive();

        // Assert
        result.Should().Be("***");
    }

    /// <summary>
    /// Verifies that MaskSensitive throws ArgumentNullException for null input.
    /// </summary>
    [Fact]
    public void MaskSensitive_WithNullInput_ThrowsArgumentNullException()
    {
        // Arrange
        string? value = null;

        // Act
        Action act = () => value!.MaskSensitive();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Verifies that MaskSensitive handles exactly 9 characters.
    /// </summary>
    [Fact]
    public void MaskSensitive_With9Characters_ReturnsMaskedString()
    {
        // Arrange
        const string value = "123456789";

        // Act
        var result = value.MaskSensitive();

        // Assert
        result.Should().Be("123***789");
    }

    /// <summary>
    /// Verifies that MaskSensitive handles exactly 8 characters.
    /// </summary>
    [Fact]
    public void MaskSensitive_With8Characters_ReturnsAsterisks()
    {
        // Arrange
        const string value = "12345678";

        // Act
        var result = value.MaskSensitive();

        // Assert
        result.Should().Be("***");
    }
}