#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Diagnostics;

/// <summary>
/// Produces log- and debugger-safe renderings of secret values. Only a short prefix and the
/// total length are emitted; the remainder of the value is never written out.
/// </summary>
internal static class Redaction
{
    /// <summary>
    /// Number of leading characters shown for a redacted value.
    /// </summary>
    public const int VisiblePrefixLength = 6;

    /// <summary>
    /// Returns a truncated prefix of a secret followed by its length, e.g. <c>eyJhbG...(len=312)</c>.
    /// Values no longer than the visible prefix are fully masked.
    /// </summary>
    /// <param name="value">The secret value to render. May be <c>null</c>.</param>
    /// <returns>A redacted representation that is safe to log or display.</returns>
    public static string Secret(string? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (value.Length <= VisiblePrefixLength)
        {
            return "***";
        }

        return $"{value[..VisiblePrefixLength]}...(len={value.Length})";
    }
}
