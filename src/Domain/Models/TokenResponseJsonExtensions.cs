#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Domain.Models;

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

/// <summary>
/// Provides extension methods for serializing and deserializing <see cref="TokenResponse"/> instances to and from JSON.
/// </summary>
public static class TokenResponseJsonExtensions
{
    private static readonly JsonSerializerOptions _defaultOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
    };

    /// <summary>
    /// Serializes the <see cref="TokenResponse"/> to a JSON string.
    /// </summary>
    /// <param name="value">The token response to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the token response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this TokenResponse value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_defaultOptions)
            {
                PropertyNamingPolicy = _defaultOptions.PropertyNamingPolicy,
                WriteIndented = true,
                TypeInfoResolver = _defaultOptions.TypeInfoResolver,
            }
            : _defaultOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string into a <see cref="TokenResponse"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized token response, or null if <paramref name="json"/> is empty, whitespace, or invalid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static TokenResponse? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<TokenResponse>(json, _defaultOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a <see cref="TokenResponse"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized token response if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out TokenResponse? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        value = null;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<TokenResponse>(json, _defaultOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}