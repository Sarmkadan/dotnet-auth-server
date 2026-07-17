using System.Text.Json;

namespace DotnetAuthServer.Services;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="SecretsService"/>.
/// Enables round-trip serialization of secrets service state.
/// </summary>
public static class SecretsServiceJsonExtensions
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes the <see cref="SecretsService"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The secrets service instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the secrets service.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this SecretsService value, bool indented = false) =>
        JsonSerializer.Serialize(value, indented ? new JsonSerializerOptions(JsonSerializerOptions) { WriteIndented = true } : JsonSerializerOptions);

    /// <summary>
    /// Deserializes a JSON string to a <see cref="SecretsService"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="SecretsService"/> instance, or null if deserialization fails.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized to a <see cref="SecretsService"/> instance.</exception>
    public static SecretsService? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        return JsonSerializer.Deserialize<SecretsService>(json, JsonSerializerOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="SecretsService"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized instance if successful.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty.</exception>
    public static bool TryFromJson(string json, [System.Diagnostics.CodeAnalysis.NotNull] out SecretsService? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = FromJson(json);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}