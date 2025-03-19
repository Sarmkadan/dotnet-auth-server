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
    public static string JoinScopes(this IEnumerable<string> scopes)
    {
        return string.Join(" ", scopes.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    /// <summary>
    /// Validates if a string is a well-formed absolute URI.
    /// Stricter than Uri.TryCreate because it requires absolute URIs.
    /// </summary>
    public static bool IsValidAbsoluteUri(this string? uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            return false;

        return Uri.TryCreate(uri, UriKind.Absolute, out var result) &&
               (result.Scheme == Uri.UriSchemeHttp || result.Scheme == Uri.UriSchemeHttps);
    }

    /// <summary>
    /// Safely compares two URIs for equality, accounting for trailing slashes and normalization.
    /// </summary>
    public static bool UriEquals(this string? uri1, string? uri2)
    {
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
    public static bool IsUrlSafe(this string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return Regex.IsMatch(value, @"^[a-zA-Z0-9\-._~]+$");
    }

    /// <summary>
    /// Safely truncates a string to a maximum length.
    /// </summary>
    public static string SafeTruncate(this string value, int maxLength)
    {
        if (value == null)
            return string.Empty;

        return value.Length > maxLength ? value.Substring(0, maxLength) : value;
    }

    /// <summary>
    /// Masks sensitive parts of a string (useful for logging).
    /// Shows only first and last few characters.
    /// </summary>
    public static string MaskSensitive(this string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length <= 8)
            return "***";

        var firstChars = value.Substring(0, 3);
        var lastChars = value.Substring(value.Length - 3);
        return $"{firstChars}***{lastChars}";
    }
}
