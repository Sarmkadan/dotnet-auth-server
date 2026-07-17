#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace DotnetAuthServer.Configuration;

/// <summary>
/// Provides validation helpers for <see cref="LoggingOptions"/> configuration.
/// </summary>
public static class LoggingOptionsValidation
{
    /// <summary>
    /// Validates the specified <see cref="LoggingOptions"/> instance.
    /// </summary>
    /// <param name="value">The logging options to validate.</param>
    /// <returns>A list of validation errors; empty if validation succeeds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this LoggingOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate MinimumLevel
        if (value.MinimumLevel == default)
        {
            errors.Add("LoggingOptions.MinimumLevel must not be the default value (Information is the minimum acceptable level).");
        }

        // Validate MaxBodyLogLength
        if (value.MaxBodyLogLength < 0)
        {
            errors.Add("LoggingOptions.MaxBodyLogLength must be a non-negative integer.");
        }

        // Validate ExcludedPaths
        if (value.ExcludedPaths is null)
        {
            errors.Add("LoggingOptions.ExcludedPaths must not be null.");
        }
        else
        {
            if (value.ExcludedPaths.Count == 0)
            {
                errors.Add("LoggingOptions.ExcludedPaths must contain at least one path.");
            }

            for (var i = 0; i < value.ExcludedPaths.Count; i++)
            {
                var path = value.ExcludedPaths[i];
                if (string.IsNullOrWhiteSpace(path))
                {
                    errors.Add($"LoggingOptions.ExcludedPaths[{i}] must not be null, empty, or whitespace.");
                }
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="LoggingOptions"/> instance is valid.
    /// </summary>
    /// <param name="value">The logging options to check.</param>
    /// <returns><see langword="true"/> if the instance is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this LoggingOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="LoggingOptions"/> instance is valid.
    /// </summary>
    /// <param name="value">The logging options to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the instance is invalid, containing a list of validation errors.</exception>
    public static void EnsureValid(this LoggingOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                "LoggingOptions validation failed. Details: " + string.Join(" ", errors),
                nameof(value));
        }
    }
}