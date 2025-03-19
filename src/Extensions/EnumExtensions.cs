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
    public static string ToDescriptionString<T>(this T @enum) where T : Enum
    {
        var field = @enum.GetType().GetField(@enum.ToString());
        var attribute = field?.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>();

        return attribute?.Description ?? @enum.ToString();
    }

    /// <summary>
    /// Converts a string to an enum value.
    /// Case-insensitive matching of enum names.
    /// </summary>
    public static T? FromString<T>(string value) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        if (Enum.TryParse<T>(value, ignoreCase: true, out var result))
            return result;

        return null;
    }

    /// <summary>
    /// Gets all values of an enum type.
    /// </summary>
    public static IEnumerable<T> GetValues<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T)).Cast<T>();
    }

    /// <summary>
    /// Checks if a string is a valid value for an enum type.
    /// </summary>
    public static bool IsValidValue<T>(string value) where T : Enum
    {
        return Enum.TryParse<T>(value, ignoreCase: true, out _);
    }
}
