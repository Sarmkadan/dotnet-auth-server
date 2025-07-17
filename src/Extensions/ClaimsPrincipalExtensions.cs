#nullable enable

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
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> to extract the claim from.</param>
    /// <returns>The subject claim value if present; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="principal"/> is <see langword="null"/>.</exception>
    public static string? GetSubject(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return principal.FindFirst(Constants.Claims.Sub)?.Value;
    }

    /// <summary>
    /// Safely retrieves the email claim from a principal.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> to extract the claim from.</param>
    /// <returns>The email claim value if present; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="principal"/> is <see langword="null"/>.</exception>
    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return principal.FindFirst(Constants.Claims.Email)?.Value;
    }

    /// <summary>
    /// Checks if a principal's email has been verified.
    /// Returns false if claim is missing or not "true".
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> to check.</param>
    /// <returns><see langword="true"/> if the email is verified; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="principal"/> is <see langword="null"/>.</exception>
    public static bool IsEmailVerified(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        var claim = principal.FindFirst(Constants.Claims.EmailVerified);
        return claim is not null && bool.TryParse(claim.Value, out var verified) && verified;
    }

    /// <summary>
    /// Retrieves all roles assigned to a principal.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> to extract roles from.</param>
    /// <returns>An enumerable of role names.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="principal"/> is <see langword="null"/>.</exception>
    public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return principal.FindAll(Constants.Claims.Roles).Select(c => c.Value);
    }

    /// <summary>
    /// Checks if a principal has a specific role.
    /// Case-insensitive comparison.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> to check.</param>
    /// <param name="role">The role name to check for.</param>
    /// <returns><see langword="true"/> if the principal has the specified role; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="principal"/> is <see langword="null"/>.
    /// or <paramref name="role"/> is <see langword="null"/>.
    /// </exception>
    public static bool HasRole(this ClaimsPrincipal principal, string role)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(role);
        return principal.GetRoles().Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Retrieves the token subject claim from a ClaimsIdentity.
    /// This is the JWT 'sub' claim that identifies the token's principal.
    /// </summary>
    /// <param name="identity">The <see cref="ClaimsIdentity"/> to extract the claim from.</param>
    /// <returns>The subject claim value if present; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="identity"/> is <see langword="null"/>.</exception>
    public static string? GetTokenSubject(this ClaimsIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);
        return identity.FindFirst(Constants.Claims.Sub)?.Value;
    }

    /// <summary>
    /// Retrieves the audience (aud) claim.
    /// In OAuth2 context, this is typically the client ID.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> to extract the claim from.</param>
    /// <returns>The audience claim value if present; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="principal"/> is <see langword="null"/>.</exception>
    public static string? GetAudience(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return principal.FindFirst(Constants.Claims.Aud)?.Value;
    }

    /// <summary>
    /// Retrieves the scope claim and parses it into individual scopes.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> to extract scopes from.</param>
    /// <returns>An enumerable of scope names.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="principal"/> is <see langword="null"/>.</exception>
    public static IEnumerable<string> GetScopes(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        var scopeClaim = principal.FindFirst(Constants.Claims.Scope)?.Value;
        return !string.IsNullOrWhiteSpace(scopeClaim)
            ? scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            : Enumerable.Empty<string>();
    }

    /// <summary>
    /// Checks if a principal has a specific scope.
    /// Case-sensitive comparison per OAuth2 spec.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> to check.</param>
    /// <param name="scope">The scope name to check for.</param>
    /// <returns><see langword="true"/> if the principal has the specified scope; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="principal"/> is <see langword="null"/>.
    /// or <paramref name="scope"/> is <see langword="null"/>.
    /// </exception>
    public static bool HasScope(this ClaimsPrincipal principal, string scope)
    {
        ArgumentNullException.ThrowIfNull(principal);
        ArgumentNullException.ThrowIfNull(scope);
        return principal.GetScopes().Contains(scope);
    }

    /// <summary>
    /// Retrieves the issued-at (iat) timestamp from a token.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> to extract the claim from.</param>
    /// <returns>The issued-at timestamp if present and valid; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="principal"/> is <see langword="null"/>.</exception>
    public static long? GetIssuedAt(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        var iatClaim = principal.FindFirst(Constants.Claims.Iat)?.Value;
        return long.TryParse(iatClaim, out var iat) ? iat : null;
    }

    /// <summary>
    /// Retrieves the expiration (exp) timestamp from a token.
    /// </summary>
    /// <param name="principal">The <see cref="ClaimsPrincipal"/> to extract the claim from.</param>
    /// <returns>The expiration timestamp if present and valid; otherwise, <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="principal"/> is <see langword="null"/>.</exception>
    public static long? GetExpiration(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        var expClaim = principal.FindFirst(Constants.Claims.Exp)?.Value;
        return long.TryParse(expClaim, out var exp) ? exp : null;
    }
}
