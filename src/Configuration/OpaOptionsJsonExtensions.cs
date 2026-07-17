#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace DotnetAuthServer.Configuration;

/// <summary>
/// Provides System.Text.Json serialization and deserialization helpers for <see cref="OpaOptions"/>.
/// </summary>
public static class OpaOptionsJsonExtensions
{
	private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
		WriteIndented = false
	};

	/// <summary>
	/// Serializes the <see cref="OpaOptions"/> instance to a JSON string.
	/// </summary>
	/// <param name="value">The options instance to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the options.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static string ToJson(this OpaOptions value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		var options = _jsonOptions;
		if (indented)
		{
			options = new JsonSerializerOptions(_jsonOptions)
			{
				PropertyNamingPolicy = _jsonOptions.PropertyNamingPolicy,
				TypeInfoResolver = _jsonOptions.TypeInfoResolver,
				WriteIndented = true
			};
		}

		return JsonSerializer.Serialize(value, options);
	}

	/// <summary>
	/// Deserializes an <see cref="OpaOptions"/> instance from a JSON string.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>The deserialized options instance, or null if the JSON is null or empty.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
	/// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
	public static OpaOptions? FromJson(string json)
	{
		ArgumentNullException.ThrowIfNull(json);

		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}

		return JsonSerializer.Deserialize<OpaOptions>(json, _jsonOptions);
	}

	/// <summary>
	/// Attempts to deserialize an <see cref="OpaOptions"/> instance from a JSON string.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="value">Receives the deserialized options instance if successful.</param>
	/// <returns>True if deserialization succeeded; otherwise, false.</returns>
	public static bool TryFromJson(string json, out OpaOptions? value)
	{
		value = null;

		if (string.IsNullOrWhiteSpace(json))
		{
			return false;
		}

		try
		{
			value = JsonSerializer.Deserialize<OpaOptions>(json, _jsonOptions);
			return true;
		}
		catch (JsonException)
		{
			return false;
		}
	}
}