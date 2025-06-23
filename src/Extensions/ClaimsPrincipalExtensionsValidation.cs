#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Extensions;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using DotnetAuthServer.Configuration;

/// <summary>
/// Validation extension methods for ClaimsPrincipal to check the validity of claims
/// and their values. Provides comprehensive validation for standard OIDC/OAuth2 claims.
/// </summary>
public static class ClaimsPrincipalExtensionsValidation
{
    /// <summary>
    /// Validates all claims in the principal and returns a list of human-readable problems.
    /// Each problem describes a specific validation failure.
    /// </summary>
    /// <param name="principal">The claims principal to validate.</param>
    /// <returns>An immutable list of validation problems (empty if valid).</returns>
    /// <exception cref="ArgumentNullException">Thrown if principal is null.</exception>
    public static IReadOnlyList<string> Validate(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var problems = new List<string>();

        // Validate Subject claim
        var subject = principal.GetSubject();
        if (string.IsNullOrWhiteSpace(subject))
        {
            problems.Add("Subject claim is missing or empty");
        }

        // Validate Email claim
        var email = principal.GetEmail();
        if (email is not null && string.IsNullOrWhiteSpace(email))
        {
            problems.Add("Email claim is empty");
        }

        // Validate EmailVerified claim
        var emailVerified = principal.IsEmailVerified();
        var emailVerifiedClaim = principal.FindFirst(Constants.Claims.EmailVerified);
        if (emailVerifiedClaim is not null && !bool.TryParse(emailVerifiedClaim.Value, out _))
        {
            problems.Add("EmailVerified claim has invalid boolean format");
        }

        // Validate Roles claims
        var roles = principal.GetRoles().ToList();
        if (roles.Count == 0)
        {
            problems.Add("No roles claims found");
        }
        else
        {
            foreach (var role in roles)
            {
                if (string.IsNullOrWhiteSpace(role))
                {
                    problems.Add("Role claim contains empty string");
                }
            }
        }

        // Validate Token Subject claim (from identity)
        var identity = principal.Identities.FirstOrDefault();
        var tokenSubject = identity?.GetTokenSubject();
        if (tokenSubject is not null && string.IsNullOrWhiteSpace(tokenSubject))
        {
            problems.Add("Token Subject claim is empty");
        }

        // Validate Audience claim
        var audience = principal.GetAudience();
        if (audience is not null && string.IsNullOrWhiteSpace(audience))
        {
            problems.Add("Audience claim is empty");
        }

        // Validate Scope claim
        var scopes = principal.GetScopes().ToList();
        if (scopes.Count == 0)
        {
            problems.Add("No scope claims found");
        }
        else
        {
            foreach (var scope in scopes)
            {
                if (string.IsNullOrWhiteSpace(scope))
                {
                    problems.Add("Scope claim contains empty string");
                }
            }
        }

        // Validate IssuedAt timestamp
        var issuedAt = principal.GetIssuedAt();
        if (issuedAt.HasValue)
        {
            if (issuedAt.Value <= 0)
            {
                problems.Add("IssuedAt timestamp is invalid (must be positive)");
            }
            else if (issuedAt.Value > DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 86400 * 365 * 10) // More than 10 years in future
            {
                problems.Add("IssuedAt timestamp is unreasonably far in the future");
            }
        }

        // Validate Expiration timestamp
        var expiration = principal.GetExpiration();
        if (expiration.HasValue)
        {
            if (expiration.Value <= 0)
            {
                problems.Add("Expiration timestamp is invalid (must be positive)");
            }
            else if (expiration.Value < DateTimeOffset.UtcNow.ToUnixTimeSeconds() - 86400) // More than 1 day in past
            {
                problems.Add("Expiration timestamp is in the past");
            }
            else if (issuedAt.HasValue && expiration.Value < issuedAt.Value)
            {
                problems.Add("Expiration timestamp is before IssuedAt timestamp");
            }
        }
        else
        {
            problems.Add("Expiration timestamp is missing");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Checks if all claims in the principal are valid.
    /// </summary>
    /// <param name="principal">The claims principal to check.</param>
    /// <returns>True if all claims are valid; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if principal is null.</exception>
    public static bool IsValid(this ClaimsPrincipal principal)
    {
        return principal.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures all claims in the principal are valid, throwing an exception if any are invalid.
    /// </summary>
    /// <param name="principal">The claims principal to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if principal is null.</exception>
    /// <exception cref="ArgumentException">Thrown if any claims are invalid, containing a list of problems.</exception>
    public static void EnsureValid(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var problems = principal.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"ClaimsPrincipal validation failed:{Environment.NewLine}- " + string.Join($"{Environment.NewLine}- ", problems));
        }
    }
}