// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Entities;

/// <summary>
/// Represents an OAuth2 scope definition
/// </summary>
public class Scope
{
    /// <summary>
    /// Scope identifier (e.g., "openid", "profile", "api:read")
    /// </summary>
    public string ScopeId { get; set; } = null!;

    /// <summary>
    /// Human-readable scope name
    /// </summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Detailed scope description for user consent
    /// </summary>
    public string Description { get; set; } = null!;

    /// <summary>
    /// Whether this scope is required for authentication
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Whether this scope requires user consent
    /// </summary>
    public bool RequiresConsent { get; set; } = true;

    /// <summary>
    /// Whether this scope is for OpenID Connect (vs OAuth2)
    /// </summary>
    public bool IsOpenIdScope { get; set; }

    /// <summary>
    /// Whether this scope is active and can be requested
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Claims included in ID token for this scope
    /// </summary>
    public ICollection<string> IdTokenClaims { get; set; } = [];

    /// <summary>
    /// Claims included in access token for this scope
    /// </summary>
    public ICollection<string> AccessTokenClaims { get; set; } = [];

    /// <summary>
    /// Roles that can access this scope (for RBAC)
    /// </summary>
    public ICollection<string> AllowedRoles { get; set; } = [];

    /// <summary>
    /// Scope creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Validates the scope has all required properties
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ScopeId) &&
               !string.IsNullOrWhiteSpace(DisplayName) &&
               !string.IsNullOrWhiteSpace(Description);
    }

    /// <summary>
    /// Checks if a user role has access to this scope
    /// </summary>
    public bool CanUserAccessScope(IEnumerable<string> userRoles)
    {
        // If no role restrictions, everyone can access
        if (AllowedRoles.Count == 0) return true;

        // Check if user has at least one allowed role
        return userRoles.Any(role =>
            AllowedRoles.Contains(role, StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets all claims that will be included for this scope
    /// </summary>
    public IEnumerable<string> GetAllClaims()
    {
        var allClaims = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var claim in IdTokenClaims.Union(AccessTokenClaims))
        {
            allClaims.Add(claim);
        }
        return allClaims;
    }
}
