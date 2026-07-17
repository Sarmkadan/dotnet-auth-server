#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace DotnetAuthServer.Integration;

/// <summary>
/// Provides JSON serialization and deserialization extensions for HTTP client factory configurations.
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
    /// Serializes the HTTP client factory configuration to a JSON string.
    /// </summary>
    /// <param name="config">The HTTP client factory configuration to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the HTTP client factory configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    public static string ToJson(HttpClientFactoryConfig config, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(config);

        var options = indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;
        return JsonSerializer.Serialize(config, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a HTTP client factory configuration.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized HTTP client factory configuration, or null if the JSON is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is malformed or cannot be deserialized.</exception>
    public static HttpClientFactoryConfig? FromJson(string? json)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        return JsonSerializer.Deserialize<HttpClientFactoryConfig>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a HTTP client factory configuration.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized HTTP client factory configuration if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string? json, out HttpClientFactoryConfig? value)
    {
        value = null;

        ArgumentNullException.ThrowIfNull(json);

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
    /// Configuration data transfer object for HTTP client factory.
    /// </summary>
    public sealed class HttpClientFactoryConfig
    {
        /// <summary>Gets or sets the default timeout for HTTP clients.</summary>
        public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Gets or sets the timeout for webhook HTTP clients.</summary>
        public TimeSpan WebhookTimeout { get; set; } = TimeSpan.FromSeconds(30);

        /// <summary>Gets or sets the user agent string for HTTP requests.</summary>
        public string UserAgent { get; set; } = "DotnetAuthServer/1.0";

        /// <summary>Gets or sets the timeout for external lookup HTTP clients.</summary>
        public TimeSpan ExternalLookupTimeout { get; set; } = TimeSpan.FromSeconds(10);
    }
}