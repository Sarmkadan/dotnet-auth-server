#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Services;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="UserService"/>
/// </summary>
public static class UserServiceJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles
    };

    /// <summary>
    /// Serializes the <see cref="UserService"/> instance to a JSON string
    /// </summary>
    /// <param name="value">The <see cref="UserService"/> instance to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>A JSON string representation of the <see cref="UserService"/></returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/></exception>
    public static string ToJson(this UserService value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions) { WriteIndented = true }
            : _jsonSerializerOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="UserService"/> instance
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <returns>A <see cref="UserService"/> instance, or <see langword="null"/> if the JSON is <see langword="null"/>, empty, or whitespace</returns>
    /// <exception cref="JsonException">Thrown when JSON deserialization fails</exception>
    public static UserService? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            return JsonSerializer.Deserialize<UserService>(json, _jsonSerializerOptions);
        }
        catch (JsonException ex)
        {
            throw new JsonException("Failed to deserialize UserService from JSON. Ensure the JSON format is valid.", ex);
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="UserService"/> instance
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="value">The resulting <see cref="UserService"/> instance, or <see langword="null"/> if deserialization fails</param>
    /// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/></returns>
    public static bool TryFromJson(string json, out UserService? value)
    {
        value = default;

        if (string.IsNullOrWhiteSpace(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<UserService>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}