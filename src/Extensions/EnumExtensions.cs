#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Extensions;

using System.Reflection;

/// <summary>
/// Extension methods for Enum operations commonly used in OAuth2.
/// Provides convenient conversion between enum values and their string representations.
/// </summary>
public static class EnumExtensions
{
    /// <summary>
    /// Converts an enum value to its string representation.
    /// By default returns the enum member name.
    /// Can be customized with Description attributes.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="enum">The enum value to convert.</param>
    /// <returns>The description from DescriptionAttribute if present, otherwise the enum name.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="enum"/> is <see langword="null"/>.</exception>
    public static string ToDescriptionString<T>(this T @enum) where T : Enum
    {
        ArgumentNullException.ThrowIfNull(@enum);

        var field = @enum.GetType().GetField(@enum.ToString());
        var attribute = field?.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();

        return attribute?.Description ?? @enum.ToString();
    }

    /// <summary>
    /// Converts a string to an enum value.
    /// Case-insensitive matching of enum names.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The string value to parse.</param>
    /// <returns>The parsed enum value, or throws if parsing fails.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> is empty or whitespace.</exception>
    public static T FromString<T>(string value) where T : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        if (Enum.TryParse<T>(value, ignoreCase: true, out var result))
            return result;

        throw new ArgumentException($"Value '{value}' is not a valid value for enum type {typeof(T).Name}.");
    }

    /// <summary>
    /// Gets all values of an enum type.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <returns>An enumerable of all enum values.</returns>
    public static IEnumerable<T> GetValues<T>() where T : Enum
        => Enum.GetValues(typeof(T)).Cast<T>();

    /// <summary>
    /// Checks if a string is a valid value for an enum type.
    /// </summary>
    /// <typeparam name="T">The enum type.</typeparam>
    /// <param name="value">The string value to validate.</param>
    /// <returns><see langword="true"/> if the value is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool IsValidValue<T>(string value) where T : struct, Enum
    {
        ArgumentNullException.ThrowIfNull(value);
        return Enum.TryParse<T>(value, ignoreCase: true, out _);
    }
}
