#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Extensions;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="StringExtensions"/>.
/// StringExtensions is a static class and cannot be directly serialized.
/// This class provides serialization support for scenarios where StringExtensions
/// functionality needs to be represented in JSON format.
/// </summary>
public static class StringExtensionsJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes type information about StringExtensions to a JSON string.
    /// StringExtensions is a static class containing extension methods for string operations.
    /// This method provides a JSON representation of the StringExtensions type metadata.
    /// </summary>
    /// <param name="typeMarker">A dummy parameter to enable extension method syntax (must not be null).</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representing StringExtensions type information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="typeMarker"/> is null.</exception>
    public static string ToJson(this object typeMarker, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(typeMarker);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;

        var metadata = new { Type = "StringExtensions", Version = "1.0" };
        return JsonSerializer.Serialize(metadata, options);
    }

    /// <summary>
    /// Deserializes JSON content. Since StringExtensions is a static class,
    /// this method always returns null rather than attempting to deserialize an instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>Always returns null since StringExtensions cannot be instantiated.</returns>
    public static object? FromJson(string json)
    {
        // StringExtensions is a static class and cannot be deserialized
        return null;
    }

    /// <summary>
    /// Attempts to deserialize JSON content.
    /// Since StringExtensions is static, this always returns false and sets value to null.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives null since StringExtensions cannot be instantiated.</param>
    /// <returns>Always returns false since StringExtensions cannot be deserialized.</returns>
    public static bool TryFromJson(string json, out object? value)
    {
        value = null;
        return false;
    }
}