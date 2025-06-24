#nullable enable

namespace DotnetAuthServer.Services;

/// <summary>
/// Provides validation helpers for <see cref="PolicyEnforcementService"/> instances and related policy objects.
/// </summary>
public static class PolicyEnforcementServiceValidation
{
    /// <summary>
    /// Validates a <see cref="PolicyEnforcementService"/> instance.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <returns>An empty list if valid; otherwise, a list of human-readable validation errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this PolicyEnforcementService value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // PolicyEnforcementService itself doesn't have public properties to validate
        // The validation happens at the method parameter level

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="PolicyEnforcementService"/> instance is valid.
    /// </summary>
    /// <param name="value">The service instance to check.</param>
    /// <returns>True if the instance is valid; otherwise, false.</returns>
    public static bool IsValid(this PolicyEnforcementService value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that a <see cref="PolicyEnforcementService"/> instance is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the service instance is invalid.</exception>
    public static void EnsureValid(this PolicyEnforcementService value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                "PolicyEnforcementService validation failed. " +
                string.Join(" ", errors),
                nameof(value));
        }
    }

    /// <summary>
    /// Validates a <see cref="Policy"/> instance.
    /// </summary>
    /// <param name="value">The policy to validate.</param>
    /// <returns>An empty list if valid; otherwise, a list of human-readable validation errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this Policy value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (value.Rules is null)
        {
            errors.Add("Policy.Rules cannot be null.");
        }
        else if (value.Rules.Count == 0)
        {
            errors.Add("Policy.Rules must contain at least one rule.");
        }

        if (!Enum.IsDefined(typeof(PolicyCombineMode), value.CombineWith))
        {
            errors.Add($"Policy.CombineWith has invalid value: {value.CombineWith}.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="Policy"/> instance is valid.
    /// </summary>
    /// <param name="value">The policy to check.</param>
    /// <returns>True if the policy is valid; otherwise, false.</returns>
    public static bool IsValid(this Policy value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that a <see cref="Policy"/> instance is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The policy to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the policy is invalid.</exception>
    public static void EnsureValid(this Policy value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                "Policy validation failed: " +
                string.Join(" ", errors),
                nameof(value));
        }
    }

    /// <summary>
    /// Validates a <see cref="PolicyRule"/> instance.
    /// </summary>
    /// <param name="value">The policy rule to validate.</param>
    /// <returns>An empty list if valid; otherwise, a list of human-readable validation errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this PolicyRule value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (!Enum.IsDefined(typeof(PolicyRuleType), value.Type))
        {
            errors.Add($"PolicyRule.Type has invalid value: {value.Type}.");
        }

        if (!Enum.IsDefined(typeof(PolicyMatchMode), value.Match))
        {
            errors.Add($"PolicyRule.Match has invalid value: {value.Match}.");
        }

        // Attribute is required for Attribute and Claim rule types
        if ((value.Type == PolicyRuleType.Attribute || value.Type == PolicyRuleType.Claim) && string.IsNullOrWhiteSpace(value.Attribute))
        {
            errors.Add("PolicyRule.Attribute is required for Attribute and Claim rule types and cannot be null or whitespace.");
        }

        // Values list can be empty but should be validated for null
        if (value.Values is null)
        {
            errors.Add("PolicyRule.Values cannot be null.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="PolicyRule"/> instance is valid.
    /// </summary>
    /// <param name="value">The policy rule to check.</param>
    /// <returns>True if the policy rule is valid; otherwise, false.</returns>
    public static bool IsValid(this PolicyRule value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that a <see cref="PolicyRule"/> instance is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The policy rule to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the policy rule is invalid.</exception>
    public static void EnsureValid(this PolicyRule value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                "PolicyRule validation failed: " +
                string.Join(" ", errors),
                nameof(value));
        }
    }

    /// <summary>
    /// Validates a <see cref="string"/> policy name.
    /// </summary>
    /// <param name="value">The policy name to validate.</param>
    /// <returns>An empty list if valid; otherwise, a list of human-readable validation errors.</returns>
    public static IReadOnlyList<string> Validate(this string? value, string paramName = "policyName")
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{paramName} cannot be null or whitespace.");
        }
        else if (value.Length > 100)
        {
            errors.Add($"{paramName} cannot exceed 100 characters.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a policy name is valid.
    /// </summary>
    /// <param name="value">The policy name to check.</param>
    /// <returns>True if the policy name is valid; otherwise, false.</returns>
    public static bool IsValid(this string? value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that a policy name is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The policy name to validate.</param>
    /// <param name="paramName">The name of the parameter being validated.</param>
    /// <exception cref="ArgumentException">Thrown when the policy name is invalid.</exception>
    public static void EnsureValid(this string? value, string paramName = "policyName")
    {
        var errors = value.Validate(paramName);
        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join(" ", errors), paramName);
        }
    }
}