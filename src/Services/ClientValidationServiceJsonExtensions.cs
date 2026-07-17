#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Services;

using System.Text.Json;
using System.Text.Json.Serialization;

/// <summary>
/// Provides System.Text.Json serialization/deserialization extensions for <see cref="ClientValidationService"/>.
/// </summary>
/// <remarks>
/// Service instances with injected dependencies cannot be meaningfully serialized.
/// These methods throw <see cref="NotSupportedException"/> to prevent misuse.
/// </remarks>
public static class ClientValidationServiceJsonExtensions
{
    /// <summary>
    /// Serializes the <see cref="ClientValidationService"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The service instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the service.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="NotSupportedException">Thrown when attempting to serialize a service with injected dependencies.</exception>
    public static string ToJson(this ClientValidationService value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        throw new NotSupportedException(
            "ClientValidationService instances with injected dependencies cannot be serialized. " +
            "This service should not be persisted or transmitted.");
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="ClientValidationService"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized service instance, or null if the JSON represents a null value.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    /// <exception cref="NotSupportedException">Thrown when attempting to deserialize a service with injected dependencies.</exception>
    public static ClientValidationService? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        throw new NotSupportedException(
            "ClientValidationService instances with injected dependencies cannot be deserialized. " +
            "This service should not be persisted or transmitted.");
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="ClientValidationService"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized service instance if successful, otherwise null.</param>
    /// <returns>True if deserialization succeeded; otherwise false.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static bool TryFromJson(string json, out ClientValidationService? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        value = null;
        return false;
    }
}