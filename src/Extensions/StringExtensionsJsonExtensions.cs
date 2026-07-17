#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Extensions;

using System.Text.Json;

/// <summary>
/// Provides System.Text.Json serialization extensions for string operations from <see cref="StringExtensions"/>.
/// These methods enable JSON serialization of string extension method results.
/// </summary>
public static class StringExtensionsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a collection of strings to JSON.
    /// </summary>
    /// <param name="values">The collection of strings to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representing the collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="values"/> is <see langword="null"/>.</exception>
    public static string ToJson(this IEnumerable<string> values, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(values);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(values, options);
    }

    /// <summary>
    /// Deserializes a JSON array of scope strings.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>An enumerable of scope strings from the JSON array.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is <see langword="null"/>.</exception>
    public static IEnumerable<string>? FromJsonToScopes(this string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<IEnumerable<string>>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize JSON content into a collection of scope strings.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="scopes">Receives the deserialized scope collection if successful.</param>
    /// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    public static bool TryFromJsonToScopes(this string? json, out IEnumerable<string>? scopes)
    {
        scopes = null;
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            scopes = JsonSerializer.Deserialize<IEnumerable<string>>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Serializes a masked string representation to JSON.
    /// </summary>
    /// <param name="value">The string to mask and serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string containing the masked value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this string value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var masked = value.MaskSensitive();
        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(new { masked }, options);
    }

    /// <summary>
    /// Serializes a truncated string to JSON.
    /// </summary>
    /// <param name="value">The string to truncate and serialize.</param>
    /// <param name="maxLength">The maximum length to keep.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string containing the truncated value.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxLength"/> is negative.</exception>
    public static string ToJson(this string value, int maxLength, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentOutOfRangeException.ThrowIfNegative(maxLength);

        var truncated = value.SafeTruncate(maxLength);
        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        return JsonSerializer.Serialize(new { truncated }, options);
    }
}