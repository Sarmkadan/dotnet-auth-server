#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace DotnetAuthServer.Examples;

public static class ClientCredentialsFlowExampleValidation
{
    // ClientCredentialsFlowExample Validation (minimal as class has no data members)
    public static IReadOnlyList<string> Validate(this ClientCredentialsFlowExample? value)
    {
        var problems = new List<string>();
        if (value is null)
        {
            problems.Add("Value cannot be null.");
        }
        return problems;
    }

    public static bool IsValid(this ClientCredentialsFlowExample? value) => value is not null;

    public static void EnsureValid(this ClientCredentialsFlowExample? value)
    {
        if (!value.IsValid())
            throw new ArgumentException("ClientCredentialsFlowExample is invalid.", nameof(value));
    }

    // TokenResponse Validation
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

    public static bool IsValid(this TokenResponse? value) => value.Validate().Count == 0;

    public static void EnsureValid(this TokenResponse? value)
    {
        var problems = value.Validate();
        if (problems.Count > 0)
            throw new ArgumentException($"TokenResponse is invalid: {string.Join(", ", problems)}", nameof(value));
    }

    // IntrospectResponse Validation
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

    public static bool IsValid(this IntrospectResponse? value) => value.Validate().Count == 0;

    public static void EnsureValid(this IntrospectResponse? value)
    {
        var problems = value.Validate();
        if (problems.Count > 0)
            throw new ArgumentException($"IntrospectResponse is invalid: {string.Join(", ", problems)}", nameof(value));
    }
}
