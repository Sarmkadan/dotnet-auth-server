#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Extensions;

using System.Text.RegularExpressions;

/// <summary>
/// Extension methods for string operations commonly used in OAuth2/OIDC flows.
/// These methods handle scope parsing, URL validation, and encoding operations
/// essential for secure token handling.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Parses a space-delimited scope string into individual scope names.
    /// Removes duplicates and empty values to ensure consistent scope handling.
    /// </summary>
    /// <param name="scopes">The space-delimited scope string to parse. May be <see langword="null"/>,
    /// which represents an absent scope parameter and yields an empty result.</param>
    /// <returns>An enumerable of distinct, non-empty scope names.</returns>
    public static IEnumerable<string> ParseScopes(this string? scopes)
    {
        if (string.IsNullOrWhiteSpace(scopes))
            return Enumerable.Empty<string>();

        return scopes.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Distinct()
            .Where(s => s.Length > 0);
    }

    /// <summary>
    /// Joins scope names into a space-delimited string.
    /// Useful for storing scopes and comparing scope lists.
    /// </summary>
    /// <param name="scopes">The collection of scope names to join.</param>
    /// <returns>A space-delimited string of scope names.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="scopes"/> is <see langword="null"/>.</exception>
    public static string JoinScopes(this IEnumerable<string> scopes)
    {
        ArgumentNullException.ThrowIfNull(scopes);

        return string.Join(" ", scopes.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    /// <summary>
    /// Validates if a string is a well-formed absolute URI.
    /// Stricter than Uri.TryCreate because it requires absolute URIs.
    /// </summary>
    /// <param name="uri">The URI string to validate.</param>
    /// <returns><see langword="true"/> if the string is a valid absolute HTTP/HTTPS URI; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="uri"/> is <see langword="null"/>.</exception>
    public static bool IsValidAbsoluteUri(this string? uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        if (string.IsNullOrWhiteSpace(uri))
            return false;

        return Uri.TryCreate(uri, UriKind.Absolute, out var result) &&
            (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Safely compares two URIs for equality, accounting for trailing slashes and normalization.
    /// </summary>
    /// <param name="uri1">The first URI string to compare.</param>
    /// <param name="uri2">The second URI string to compare.</param>
    /// <returns><see langword="true"/> if the URIs are equal; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if either <paramref name="uri1"/> or <paramref name="uri2"/> is <see langword="null"/>.</exception>
    public static bool UriEquals(this string? uri1, string? uri2)
    {
        ArgumentNullException.ThrowIfNull(uri1);
        ArgumentNullException.ThrowIfNull(uri2);

        if (string.IsNullOrWhiteSpace(uri1) || string.IsNullOrWhiteSpace(uri2))
            return string.Equals(uri1, uri2, StringComparison.OrdinalIgnoreCase);

        if (!Uri.TryCreate(uri1, UriKind.Absolute, out var result1) ||
            !Uri.TryCreate(uri2, UriKind.Absolute, out var result2))
            return false;

        return result1.Equals(result2);
    }

    /// <summary>
    /// Checks if a string contains only URL-safe characters.
    /// Used to validate client IDs, usernames, and other identifiers.
    /// </summary>
    /// <param name="value">The string to validate.</param>
    /// <returns><see langword="true"/> if the string contains only URL-safe characters; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool IsUrlSafe(this string? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Regex.IsMatch(value, @"^[a-zA-Z0-9\-._~]+$");
    }

    /// <summary>
    /// Safely truncates a string to a maximum length.
    /// </summary>
    /// <param name="value">The string to truncate.</param>
    /// <param name="maxLength">The maximum length to keep.</param>
    /// <returns>The truncated string, or the original string if it's shorter than <paramref name="maxLength"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxLength"/> is negative.</exception>
    public static string SafeTruncate(this string value, int maxLength)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        return value.Length > maxLength ? value[..maxLength] : value;
    }

    /// <summary>
    /// Masks sensitive parts of a string (useful for logging).
    /// Shows only first and last few characters.
    /// </summary>
    /// <param name="value">The string to mask.</param>
    /// <returns>A masked version of the string showing only first and last 3 characters.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string MaskSensitive(this string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (value.Length <= 8)
            return "***";

        var firstChars = value[..3];
        var lastChars = value[^3..];
        return $"{firstChars}***{lastChars}";
    }
}