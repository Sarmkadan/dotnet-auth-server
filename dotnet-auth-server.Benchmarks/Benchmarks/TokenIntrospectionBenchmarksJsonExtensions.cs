using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetAuthServer.Handlers;
using DotnetAuthServer.Services;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Models;

namespace DotnetAuthServer.Benchmarks;

/// <summary>
/// Provides System.Text.Json serialization and deserialization methods for <see cref="TokenIntrospectionBenchmarks"/>.
/// </summary>
public static class TokenIntrospectionBenchmarksJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Serializes the <see cref="TokenIntrospectionBenchmarks"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The benchmarks instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the benchmarks instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static string ToJson(this TokenIntrospectionBenchmarks value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions) { WriteIndented = true }
            : _jsonSerializerOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="TokenIntrospectionBenchmarks"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A deserialized <see cref="TokenIntrospectionBenchmarks"/> instance, or null if the JSON is invalid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="json"/> is empty or whitespace.</exception>
    public static TokenIntrospectionBenchmarks? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(json, nameof(json));

        try
        {
            return JsonSerializer.Deserialize<TokenIntrospectionBenchmarks>(json, _jsonSerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="TokenIntrospectionBenchmarks"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized instance if successful, otherwise null.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out TokenIntrospectionBenchmarks? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            value = JsonSerializer.Deserialize<TokenIntrospectionBenchmarks>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}