// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Extensions;

using System.Security.Claims;
using DotnetAuthServer.Configuration;

/// <summary>
/// Extension methods for ClaimsPrincipal to safely extract and validate claims.
/// Provides type-safe access to standard OIDC/OAuth2 claims while handling
/// missing or malformed claims gracefully.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Safely retrieves the subject (sub) claim from a principal.
    /// Subject uniquely identifies the user within the issuer.
    /// </summary>
    public static string? GetSubject(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(Constants.Claims.Sub)?.Value;
    }

    /// <summary>
    /// Safely retrieves the email claim from a principal.
    /// </summary>
    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(Constants.Claims.Email)?.Value;
    }

    /// <summary>
    /// Checks if a principal's email has been verified.
    /// Returns false if claim is missing or not "true".
    /// </summary>
    public static bool IsEmailVerified(this ClaimsPrincipal principal)
    {
        var claim = principal.FindFirst(Constants.Claims.EmailVerified);
        return claim != null && bool.TryParse(claim.Value, out var verified) && verified;
    }

    /// <summary>
    /// Retrieves all roles assigned to a principal.
    /// </summary>
    public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll(Constants.Claims.Roles).Select(c => c.Value);
    }

    /// <summary>
    /// Checks if a principal has a specific role.
    /// Case-insensitive comparison.
    /// </summary>
    public static bool HasRole(this ClaimsPrincipal principal, string role)
    {
        return principal.GetRoles().Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Retrieves the token subject claim from a ClaimsIdentity.
    /// This is the JWT 'sub' claim that identifies the token's principal.
    /// </summary>
    public static string? GetTokenSubject(this ClaimsIdentity identity)
    {
        return identity.FindFirst(Constants.Claims.Sub)?.Value;
    }

    /// <summary>
    /// Retrieves the audience (aud) claim.
    /// In OAuth2 context, this is typically the client ID.
    /// </summary>
    public static string? GetAudience(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(Constants.Claims.Aud)?.Value;
    }

    /// <summary>
    /// Retrieves the scope claim and parses it into individual scopes.
    /// </summary>
    public static IEnumerable<string> GetScopes(this ClaimsPrincipal principal)
    {
        var scopeClaim = principal.FindFirst(Constants.Claims.Scope)?.Value;
        return !string.IsNullOrWhiteSpace(scopeClaim)
            ? scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            : Enumerable.Empty<string>();
    }

    /// <summary>
    /// Checks if a principal has a specific scope.
    /// Case-sensitive comparison per OAuth2 spec.
    /// </summary>
    public static bool HasScope(this ClaimsPrincipal principal, string scope)
    {
        return principal.GetScopes().Contains(scope);
    }

    /// <summary>
    /// Retrieves the issued-at (iat) timestamp from a token.
    /// </summary>
    public static long? GetIssuedAt(this ClaimsPrincipal principal)
    {
        var iatClaim = principal.FindFirst(Constants.Claims.Iat)?.Value;
        return long.TryParse(iatClaim, out var iat) ? iat : null;
    }

    /// <summary>
    /// Retrieves the expiration (exp) timestamp from a token.
    /// </summary>
    public static long? GetExpiration(this ClaimsPrincipal principal)
    {
        var expClaim = principal.FindFirst(Constants.Claims.Exp)?.Value;
        return long.TryParse(expClaim, out var exp) ? exp : null;
    }
}
