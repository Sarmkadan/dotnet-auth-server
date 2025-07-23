#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetAuthServer.Examples;

/// <summary>
/// Provides validation methods for ClientCredentialsFlowExample and TokenResponse.
/// </summary>
public static class ClientCredentialsFlowExampleValidation
{
    /// <summary>
    /// Validates the ClientCredentialsFlowExample instance.
    /// </summary>
    /// <param name="value">The ClientCredentialsFlowExample instance to validate.</param>
    /// <returns>A list of validation problems.</returns>
    public static IReadOnlyList<string> Validate(this ClientCredentialsFlowExample? value)
    {
        var problems = new List<string>();
        if (value is null)
        {
            problems.Add("Value cannot be null.");
        }
        return problems;
    }

    /// <summary>
    /// Checks if the ClientCredentialsFlowExample instance is valid.
    /// </summary>
    /// <param name="value">The ClientCredentialsFlowExample instance to check.</param>
    /// <returns>True if the instance is valid, false otherwise.</returns>
    public static bool IsValid(this ClientCredentialsFlowExample? value) => value is not null;

    /// <summary>
    /// Ensures the ClientCredentialsFlowExample instance is valid.
    /// </summary>
    /// <param name="value">The ClientCredentialsFlowExample instance to ensure is valid.</param>
    /// <exception cref="ArgumentException">Thrown if the instance is invalid.</exception>
    public static void EnsureValid(this ClientCredentialsFlowExample? value)
    {
        if (!value.IsValid())
            throw new ArgumentException("ClientCredentialsFlowExample is invalid.", nameof(value));
    }

    /// <summary>
    /// Validates the TokenResponse instance.
    /// </summary>
    /// <param name="value">The TokenResponse instance to validate.</param>
    /// <returns>A list of validation problems.</returns>
    public static IReadOnlyList<string> Validate(this TokenResponse? value)
    {
        var problems = new List<string>();
        if (value is null)
        {
            problems.Add("TokenResponse cannot be null.");
            return problems;
        }

        if (string.IsNullOrWhiteSpace(value.AccessToken))
            problems.Add("AccessToken cannot be empty.");
        if (string.IsNullOrWhiteSpace(value.TokenType))
            problems.Add("TokenType cannot be empty.");
        if (value.ExpiresIn <= 0)
            problems.Add("ExpiresIn must be greater than zero.");
        if (string.IsNullOrWhiteSpace(value.Scope))
            problems.Add("Scope cannot be empty.");

        return problems;
    }

    /// <summary>
    /// Checks if the TokenResponse instance is valid.
    /// </summary>
    /// <param name="value">The TokenResponse instance to check.</param>
    /// <returns>True if the instance is valid, false otherwise.</returns>
    public static bool IsValid(this TokenResponse? value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures the TokenResponse instance is valid.
    /// </summary>
    /// <param name="value">The TokenResponse instance to ensure is valid.</param>
    /// <exception cref="ArgumentException">Thrown if the instance is invalid.</exception>
    public static void EnsureValid(this TokenResponse? value)
    {
        var problems = value.Validate();
        if (problems.Count > 0)
            throw new ArgumentException($"TokenResponse is invalid: {string.Join(", ", problems)}", nameof(value));
    }

    /// <summary>
    /// Validates the IntrospectResponse instance.
    /// </summary>
    /// <param name="value">The IntrospectResponse instance to validate.</param>
    /// <returns>A list of validation problems.</returns>
    public static IReadOnlyList<string> Validate(this IntrospectResponse? value)
    {
        var problems = new List<string>();
        if (value is null)
        {
            problems.Add("IntrospectResponse cannot be null.");
            return problems;
        }

        // Active, Scope, ClientId, Subject, ExpiresAt are optional/nullable or bool, so not much to validate.
        return problems;
    }

    /// <summary>
    /// Checks if the IntrospectResponse instance is valid.
    /// </summary>
    /// <param name="value">The IntrospectResponse instance to check.</param>
    /// <returns>True if the instance is valid, false otherwise.</returns>
    public static bool IsValid(this IntrospectResponse? value) => value.Validate().Count == 0;

    /// <summary>
    /// Ensures the IntrospectResponse instance is valid.
    /// </summary>
    /// <param name="value">The IntrospectResponse instance to ensure is valid.</param>
    /// <exception cref="ArgumentException">Thrown if the instance is invalid.</exception>
    public static void EnsureValid(this IntrospectResponse? value)
    {
        var problems = value.Validate();
        if (problems.Count > 0)
            throw new ArgumentException($"IntrospectResponse is invalid: {string.Join(", ", problems)}", nameof(value));
    }
}
