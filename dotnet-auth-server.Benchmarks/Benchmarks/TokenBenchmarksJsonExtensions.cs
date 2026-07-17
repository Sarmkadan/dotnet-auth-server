using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetAuthServer.Benchmarks;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="TokenBenchmarks"/>.
/// </summary>
public static class TokenBenchmarksJsonExtensions
{
	private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	private static readonly JsonSerializerOptions _jsonOptionsIndented = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
	};

	/// <summary>
	/// Serializes the <see cref="TokenBenchmarks"/> instance to a JSON string.
	/// </summary>
	/// <param name="value">The token benchmarks instance to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the token benchmarks.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
	public static string ToJson(this TokenBenchmarks value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		return JsonSerializer.Serialize(value, indented ? _jsonOptionsIndented : _jsonOptions);
	}

	/// <summary>
	/// Deserializes a JSON string to a <see cref="TokenBenchmarks"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>The deserialized <see cref="TokenBenchmarks"/> instance, or <see langword="null"/> if the JSON represents a null value.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
	/// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
	public static TokenBenchmarks? FromJson(string json)
	{
		ArgumentNullException.ThrowIfNull(json);

		return JsonSerializer.Deserialize<TokenBenchmarks>(json, _jsonOptions);
	}

	/// <summary>
	/// Attempts to deserialize a JSON string to a <see cref="TokenBenchmarks"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="value">Receives the deserialized instance if successful; otherwise, <see langword="null"/>.</param>
	/// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or consists only of whitespace.</exception>
	public static bool TryFromJson(string json, out TokenBenchmarks? value)
	{
		ArgumentNullException.ThrowIfNull(json);
		ArgumentException.ThrowIfNullOrWhiteSpace(json);

		try
		{
			value = JsonSerializer.Deserialize<TokenBenchmarks>(json, _jsonOptions);
			return true;
		}
		catch (JsonException)
		{
			value = null;
			return false;
		}
	}
}