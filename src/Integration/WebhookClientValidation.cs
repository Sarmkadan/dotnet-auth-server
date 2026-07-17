#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

namespace DotnetAuthServer.Integration;

using System;
using System.Collections.Generic;
using System.Globalization;

/// <summary>
/// Provides validation helpers for webhook client configuration and related types.
/// </summary>
public static class WebhookClientValidation
{
    /// <summary>
    /// Validates a webhook client configuration.
    /// </summary>
    /// <param name="options">The webhook options to validate</param>
    /// <returns>A list of validation problems; empty if valid</returns>
    /// <exception cref="ArgumentNullException">options is null</exception>
    public static IReadOnlyList<string> Validate(this WebhookOptions? options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var problems = new List<string>();

        if (options.MaxRetries < 0)
        {
            problems.Add($"MaxRetries must be non-negative, but was {options.MaxRetries}");
        }

        if (options.InitialRetryDelayMs < 100)
        {
            problems.Add($"InitialRetryDelayMs must be at least 100ms, but was {options.InitialRetryDelayMs}");
        }

        if (options.MaxRetryDelayMs < options.InitialRetryDelayMs)
        {
            problems.Add($"MaxRetryDelayMs ({options.MaxRetryDelayMs}ms) must be greater than or equal to InitialRetryDelayMs ({options.InitialRetryDelayMs}ms)");
        }

        if (options.Timeout <= TimeSpan.Zero)
        {
            problems.Add($"Timeout must be positive, but was {options.Timeout}");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Checks if a webhook client configuration is valid.
    /// </summary>
    /// <param name="options">The webhook options to check</param>
    /// <returns>true if valid; false otherwise</returns>
    public static bool IsValid(this WebhookOptions? options) => options is not null && Validate(options).Count == 0;

    /// <summary>
    /// Ensures that a webhook client configuration is valid, throwing an exception if not.
    /// </summary>
    /// <param name="options">The webhook options to validate</param>
    /// <exception cref="ArgumentNullException">options is null</exception>
    /// <exception cref="ArgumentException">options is invalid</exception>
    public static void EnsureValid(this WebhookOptions? options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var problems = Validate(options);
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"Webhook configuration is invalid:{Environment.NewLine}- {string.Join($"{Environment.NewLine}- ", problems)}");
        }
    }
}