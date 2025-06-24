#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotnetAuthServer.Configuration;

/// <summary>
/// Provides validation helpers for <see cref="AuthServerOptions"/> configuration options.
/// </summary>
public static class AuthServerOptionsValidation
{
    /// <summary>
    /// Determines whether the specified <see cref="AuthServerOptions"/> instance is valid.
    /// </summary>
    /// <param name="value">The options to check.</param>
    /// <returns>True if the options are valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this AuthServerOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Validate();
    }

    /// <summary>
    /// Ensures that the specified <see cref="AuthServerOptions"/> instance is valid.
    /// </summary>
    /// <param name="value">The options to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the options are invalid, containing the validation errors.</exception>
    public static void EnsureValid(this AuthServerOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        if (!value.Validate())
        {
            throw new ArgumentException(
                "The AuthServerOptions instance is invalid.",
                nameof(value));
        }
    }
}