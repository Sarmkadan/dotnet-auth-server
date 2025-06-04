#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace DotnetAuthServer.Integration;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="HttpClientFactory"/>.
/// Enables round-trip serialization of HTTP client factory configurations.
/// </summary>
public static class HttpClientFactoryJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Serializes the <see cref="HttpClientFactory"/> static class configuration to a JSON string.
    /// </summary>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the HTTP client factory configuration.</returns>
    public static string ToJson(bool indented = false)
    {
        var config = new HttpClientFactoryConfig
        {
            DefaultTimeout = TimeSpan.FromSeconds(30),
            WebhookTimeout = TimeSpan.FromSeconds(30),
            UserAgent = "DotnetAuthServer/1.0",
            ExternalLookupTimeout = TimeSpan.FromSeconds(10)
        };

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;
        return JsonSerializer.Serialize(config, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="HttpClientFactory"/> configuration.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized HTTP client factory configuration, or null if the JSON is null or empty.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is malformed or cannot be deserialized.</exception>
    public static HttpClientFactoryConfig? FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<HttpClientFactoryConfig>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="HttpClientFactory"/> configuration.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized HTTP client factory configuration if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    public static bool TryFromJson(string json, out HttpClientFactoryConfig? value)
    {
        value = null;

        if (string.IsNullOrEmpty(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<HttpClientFactoryConfig>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    /// <summary>
    /// Configuration data transfer object for <see cref="HttpClientFactory"/>.
    /// </summary>
    public sealed class HttpClientFactoryConfig
    {
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public TimeSpan WebhookTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public string UserAgent { get; set; } = "DotnetAuthServer/1.0";
        public TimeSpan ExternalLookupTimeout { get; set; } = TimeSpan.FromSeconds(10);
    }
}