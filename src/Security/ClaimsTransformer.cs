#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Security;

using System.Security.Claims;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Domain.Entities;

/// <summary>
/// Transforms and enriches claims for authenticated users. Applies role-based claims,
/// scope-derived permissions, and custom attribute claims from the user profile.
/// </summary>
public sealed class ClaimsTransformer
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<ClaimsTransformer> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="ClaimsTransformer"/>.
    /// </summary>
    /// <param name="userRepository">Repository for user data lookups.</param>
    /// <param name="logger">Logger instance.</param>
    public ClaimsTransformer(IUserRepository userRepository, ILogger<ClaimsTransformer> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Transforms a base set of claims by enriching it with user profile data, roles, and
    /// scope-derived permission claims.
    /// </summary>
    /// <param name="userId">The subject identifier to enrich claims for.</param>
    /// <param name="existingClaims">The claims already present (e.g., from the JWT).</param>
    /// <param name="grantedScopes">Space-separated scope string from the token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The enriched claims collection.</returns>
    public async Task<IList<Claim>> TransformAsync(
        string userId,
        IEnumerable<Claim> existingClaims,
        string grantedScopes,
        CancellationToken cancellationToken = default)
    {
        var claims = new List<Claim>(existingClaims);
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);

        if (user is null)
        {
            _logger.LogWarning("ClaimsTransformer: user {UserId} not found, returning original claims", userId);
            return claims;
        }

        AddRoleClaims(claims, user);
        AddProfileClaims(claims, user);
        AddScopePermissions(claims, grantedScopes);

        _logger.LogDebug("Transformed claims for user {UserId}: {Count} total claims", userId, claims.Count);
        return claims;
    }

    /// <summary>
    /// Filters a claims collection to only those allowed by the granted scopes.
    /// Claims outside the permitted scope set are stripped.
    /// </summary>
    /// <param name="claims">The full set of claims.</param>
    /// <param name="grantedScopes">Space-separated scope string determining allowed claims.</param>
    /// <returns>Filtered claims list.</returns>
    public IList<Claim> FilterByScopes(IEnumerable<Claim> claims, string grantedScopes)
    {
        var scopes = new HashSet<string>(
            (grantedScopes ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries),
            StringComparer.OrdinalIgnoreCase);

        var allowed = new List<Claim>();

        foreach (var claim in claims)
        {
            if (IsClaimAllowedByScope(claim, scopes))
            {
                allowed.Add(claim);
            }
        }

        return allowed;
    }

    /// <summary>
    /// Merges two claim sets, deduplicating by type+value pair.
    /// </summary>
    /// <param name="primary">Primary claims (take precedence).</param>
    /// <param name="secondary">Secondary claims (added if not already present).</param>
    /// <returns>Merged claims list.</returns>
    public IList<Claim> MergeClaims(IEnumerable<Claim> primary, IEnumerable<Claim> secondary)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<Claim>();

        foreach (var claim in primary)
        {
            var key = $"{claim.Type}|{claim.Value}";
            if (seen.Add(key))
            {
                result.Add(claim);
            }
        }

        foreach (var claim in secondary)
        {
            var key = $"{claim.Type}|{claim.Value}";
            if (seen.Add(key))
            {
                result.Add(claim);
            }
        }

        return result;
    }

    /// <summary>
    /// Extracts the subject identifier from a claims principal.
    /// </summary>
    /// <param name="principal">The claims principal to inspect.</param>
    /// <returns>The subject claim value, or <c>null</c> if not found.</returns>
    public static string? GetSubject(ClaimsPrincipal principal)
    {
        return principal.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? principal.FindFirstValue("sub");
    }

    private static void AddRoleClaims(List<Claim> claims, User user)
    {
        if (user.Roles is null) return;

        foreach (var role in user.Roles)
        {
            if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value == role))
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }
        }
    }

    private static void AddProfileClaims(List<Claim> claims, User user)
    {
        if (!string.IsNullOrEmpty(user.FullName) && !claims.Any(c => c.Type == "name"))
        {
            claims.Add(new Claim("name", user.FullName));
        }

        if (!string.IsNullOrEmpty(user.Email) && !claims.Any(c => c.Type == ClaimTypes.Email))
        {
            claims.Add(new Claim(ClaimTypes.Email, user.Email));
        }

        claims.Add(new Claim("email_verified", user.EmailVerified.ToString().ToLowerInvariant()));
    }

    private static void AddScopePermissions(List<Claim> claims, string grantedScopes)
    {
        var scopes = (grantedScopes ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var scope in scopes)
        {
            if (!claims.Any(c => c.Type == "permission" && c.Value == scope))
            {
                claims.Add(new Claim("permission", scope));
            }
        }
    }

    private static bool IsClaimAllowedByScope(Claim claim, HashSet<string> scopes)
    {
        // Standard identity claims are always allowed
        if (claim.Type is "sub" or "iss" or "aud" or "exp" or "iat" or "nbf" or "jti" or "client_id")
        {
            return true;
        }

        // Profile claims require "profile" or "openid" scope
        if (claim.Type is "name" or "preferred_username" or "picture")
        {
            return scopes.Contains("profile") || scopes.Contains("openid");
        }

        // Email claims require "email" scope
        if (claim.Type is "email" or "email_verified")
        {
            return scopes.Contains("email");
        }

        // Role/permission claims always pass through
        if (claim.Type == ClaimTypes.Role || claim.Type == "role" || claim.Type == "permission")
        {
            return true;
        }

        return true;
    }
}
