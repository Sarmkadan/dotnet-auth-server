#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DotnetAuthServer.Examples;

/// <summary>
/// System.Text.Json serialization/deserialization helpers for ResourceServerStartupExample
/// Provides complete JSON serialization support with proper error handling
/// </summary>
public static class ResourceServerStartupExampleJsonExtensions
{
    /// <summary>
    /// JSON serialization options with camelCase naming convention
    /// </summary>
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Converts a ResourceServerStartupExample instance to a JSON string
    /// </summary>
    /// <param name="value">The instance to serialize</param>
    /// <param name="indented">Whether to format the JSON with indentation</param>
    /// <returns>JSON string representation of the instance</returns>
    /// <exception cref="ArgumentNullException">Thrown when value is null</exception>
    public static string ToJson(this ResourceServerStartupExample value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        return JsonSerializer.Serialize(value, indented ? new JsonSerializerOptions(_jsonOptions)
        {
            WriteIndented = true
        } : _jsonOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to a ResourceServerStartupExample instance
    /// </summary>
    /// <param name="json">JSON string to deserialize. Must be valid JSON; whitespace-only strings are treated as invalid.</param>
    /// <returns>Deserialized instance if successful, null if JSON is invalid or cannot be deserialized</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null, empty, or consists only of whitespace</exception>
    public static ResourceServerStartupExample? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ResourceServerStartupExample>(json, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a ResourceServerStartupExample instance
    /// </summary>
    /// <param name="json">JSON string to deserialize</param>
    /// <param name="value">Output parameter containing the deserialized instance</param>
    /// <returns>True if deserialization succeeded, false otherwise</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null, empty, or consists only of whitespace</exception>
    public static bool TryFromJson(string json, out ResourceServerStartupExample? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            value = null;
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<ResourceServerStartupExample>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}