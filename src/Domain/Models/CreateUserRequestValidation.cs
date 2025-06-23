#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Models;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

/// <summary>
/// Provides validation helpers for <see cref="CreateUserRequest"/> instances.
/// </summary>
public static class CreateUserRequestValidation
{
    private const string UsernamePattern = "^[a-zA-Z0-9._-]{3,50}$";
    private static readonly Regex UsernameRegex = new(UsernamePattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    /// <summary>
    /// Validates the given <see cref="CreateUserRequest"/> and returns a list of human-readable validation errors.
    /// </summary>
    /// <param name="value">The request to validate.</param>
    /// <returns>An empty list if valid; otherwise, a list of error messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this CreateUserRequest value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Username
        if (string.IsNullOrWhiteSpace(value.Username))
        {
            errors.Add("Username is required.");
        }
        else if (value.Username.Length is < 3 or > 50)
        {
            errors.Add("Username must be between 3 and 50 characters long.");
        }
        else if (!UsernameRegex.IsMatch(value.Username))
        {
            errors.Add("Username can only contain alphanumeric characters, dots, underscores, and hyphens.");
        }

        // Validate Email
        if (string.IsNullOrWhiteSpace(value.Email))
        {
            errors.Add("Email is required.");
        }
        else if (!IsValidEmail(value.Email))
        {
            errors.Add("Email must be a valid email address.");
        }

        // Validate Password
        if (string.IsNullOrWhiteSpace(value.Password))
        {
            errors.Add("Password is required.");
        }
        else if (value.Password.Length < 8)
        {
            errors.Add("Password must be at least 8 characters long.");
        }

        // Validate Roles collection (if not null)
        if (value.Roles is not null)
        {
            var invalidRoles = value.Roles
                .Where(role => string.IsNullOrWhiteSpace(role))
                .ToList();

            if (invalidRoles.Count > 0)
            {
                errors.Add("Roles collection contains empty or null entries.");
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the given <see cref="CreateUserRequest"/> is valid.
    /// </summary>
    /// <param name="value">The request to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this CreateUserRequest value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the given <see cref="CreateUserRequest"/> is valid, throwing an <see cref="ArgumentException"/>
    /// with a detailed error message if it is not.
    /// </summary>
    /// <param name="value">The request to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the request is invalid, containing a list of all validation errors.</exception>
    public static void EnsureValid(this CreateUserRequest value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);

        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"CreateUserRequest is invalid. Errors: {string.Join(" ", errors)}",
                nameof(value));
        }
    }

    private static bool IsValidEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }

        try
        {
            // Use simple validation that matches the [EmailAddress] attribute behavior
            var emailParts = email.Split('@');
            if (emailParts.Length != 2)
            {
                return false;
            }

            var localPart = emailParts[0];
            var domainPart = emailParts[1];

            if (string.IsNullOrWhiteSpace(localPart) || string.IsNullOrWhiteSpace(domainPart))
            {
                return false;
            }

            if (localPart.Length > 64 || domainPart.Length > 255)
            {
                return false;
            }

            // Check for valid characters
            var validLocalPattern = "^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+$";
            var validDomainPattern = "^[a-zA-Z0-9-]+(\\.[a-zA-Z0-9-]+)*$";

            return Regex.IsMatch(localPart, validLocalPattern, RegexOptions.None, TimeSpan.FromSeconds(1))
                   && Regex.IsMatch(domainPart, validDomainPattern, RegexOptions.None, TimeSpan.FromSeconds(1))
                   && domainPart.Contains('.')
                   && !domainPart.StartsWith('.')
                   && !domainPart.EndsWith('.');
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }
}