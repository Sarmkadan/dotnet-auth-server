// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotnetAuthServer.Extensions;
using FluentAssertions;

namespace DotnetAuthServer.Tests;

public class StringExtensionsTests
{
    // -------------------------------------------------------------------------
    // ParseScopes
    // -------------------------------------------------------------------------

    [Fact]
    public void ParseScopes_NullInput_ReturnsEmpty()
    {
        string? scopes = null;
        scopes.ParseScopes().Should().BeEmpty();
    }

    [Fact]
    public void ParseScopes_EmptyInput_ReturnsEmpty()
    {
        "".ParseScopes().Should().BeEmpty();
    }

    [Fact]
    public void ParseScopes_WhitespaceInput_ReturnsEmpty()
    {
        "   ".ParseScopes().Should().BeEmpty();
    }

    [Fact]
    public void ParseScopes_SingleScope_ReturnsSingleItem()
    {
        "openid".ParseScopes().Should().BeEquivalentTo(new[] { "openid" });
    }

    [Fact]
    public void ParseScopes_MultipleScopes_SplitsCorrectly()
    {
        "openid profile email".ParseScopes().Should().BeEquivalentTo(new[] { "openid", "profile", "email" });
    }

    [Fact]
    public void ParseScopes_DuplicateScopes_RemovesDuplicates()
    {
        "openid openid profile".ParseScopes().Should().BeEquivalentTo(new[] { "openid", "profile" });
    }

    [Fact]
    public void ParseScopes_ExtraSpaces_HandlesGracefully()
    {
        "openid  profile   email".ParseScopes().Should().HaveCount(3);
    }

    // -------------------------------------------------------------------------
    // JoinScopes
    // -------------------------------------------------------------------------

    [Fact]
    public void JoinScopes_EmptyCollection_ReturnsEmpty()
    {
        Enumerable.Empty<string>().JoinScopes().Should().BeEmpty();
    }

    [Fact]
    public void JoinScopes_MultipleScopes_JoinsWithSpace()
    {
        new[] { "openid", "profile" }.JoinScopes().Should().Be("openid profile");
    }

    [Fact]
    public void JoinScopes_FiltersNullAndEmpty()
    {
        new[] { "openid", "", null!, "profile" }.JoinScopes().Should().Be("openid profile");
    }

    // -------------------------------------------------------------------------
    // IsValidAbsoluteUri
    // -------------------------------------------------------------------------

    [Fact]
    public void IsValidAbsoluteUri_HttpsUrl_ReturnsTrue()
    {
        "https://example.com/callback".IsValidAbsoluteUri().Should().BeTrue();
    }

    [Fact]
    public void IsValidAbsoluteUri_HttpUrl_ReturnsTrue()
    {
        "http://localhost:5000".IsValidAbsoluteUri().Should().BeTrue();
    }

    [Fact]
    public void IsValidAbsoluteUri_FtpUrl_ReturnsFalse()
    {
        "ftp://files.example.com".IsValidAbsoluteUri().Should().BeFalse();
    }

    [Fact]
    public void IsValidAbsoluteUri_RelativePath_ReturnsFalse()
    {
        "/callback".IsValidAbsoluteUri().Should().BeFalse();
    }

    [Fact]
    public void IsValidAbsoluteUri_NullInput_ReturnsFalse()
    {
        string? uri = null;
        uri.IsValidAbsoluteUri().Should().BeFalse();
    }

    [Fact]
    public void IsValidAbsoluteUri_EmptyInput_ReturnsFalse()
    {
        "".IsValidAbsoluteUri().Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // UriEquals
    // -------------------------------------------------------------------------

    [Fact]
    public void UriEquals_SameUri_ReturnsTrue()
    {
        "https://example.com/path".UriEquals("https://example.com/path").Should().BeTrue();
    }

    [Fact]
    public void UriEquals_BothNull_ReturnsTrue()
    {
        string? uri1 = null;
        uri1.UriEquals(null).Should().BeTrue();
    }

    [Fact]
    public void UriEquals_OneNull_ReturnsFalse()
    {
        "https://example.com".UriEquals(null).Should().BeFalse();
    }

    [Fact]
    public void UriEquals_DifferentUris_ReturnsFalse()
    {
        "https://a.com".UriEquals("https://b.com").Should().BeFalse();
    }

    // -------------------------------------------------------------------------
    // IsUrlSafe
    // -------------------------------------------------------------------------

    [Fact]
    public void IsUrlSafe_AlphanumericWithDash_ReturnsTrue()
    {
        "my-client-id".IsUrlSafe().Should().BeTrue();
    }

    [Fact]
    public void IsUrlSafe_WithTilde_ReturnsTrue()
    {
        "value~1".IsUrlSafe().Should().BeTrue();
    }

    [Fact]
    public void IsUrlSafe_WithSpaces_ReturnsFalse()
    {
        "has spaces".IsUrlSafe().Should().BeFalse();
    }

    [Fact]
    public void IsUrlSafe_NullInput_ReturnsFalse()
    {
        string? value = null;
        value.IsUrlSafe().Should().BeFalse();
    }

    [Fact]
    public void IsUrlSafe_EmptyInput_ReturnsFalse()
    {
        "".IsUrlSafe().Should().BeFalse();
    }

    [Fact]
    public void IsUrlSafe_SpecialChars_ReturnsFalse()
    {
        "client<script>".IsUrlSafe().Should().BeFalse();
    }
}
