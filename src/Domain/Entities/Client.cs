// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Entities;

/// <summary>
/// Represents an OAuth2/OIDC client application
/// </summary>
public class Client
{
    /// <summary>
    /// Unique client identifier
    /// </summary>
    public string ClientId { get; set; } = null!;

    /// <summary>
    /// Client display name
    /// </summary>
    public string ClientName { get; set; } = null!;

    /// <summary>
    /// Client secret for confidential clients (hashed)
    /// </summary>
    public string? ClientSecretHash { get; set; }

    /// <summary>
    /// Client description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this is a confidential (has secret) or public client
    /// </summary>
    public bool IsConfidential { get; set; }

    /// <summary>
    /// Whether the client is active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Registered redirect URIs for authorization code flow
    /// </summary>
    public ICollection<string> RedirectUris { get; set; } = [];

    /// <summary>
    /// Allowed logout redirect URIs
    /// </summary>
    public ICollection<string> PostLogoutRedirectUris { get; set; } = [];

    /// <summary>
    /// Allowed grant types for this client
    /// </summary>
    public ICollection<string> AllowedGrantTypes { get; set; } = [];

    /// <summary>
    /// Allowed scopes for this client
    /// </summary>
    public ICollection<string> AllowedScopes { get; set; } = [];

    /// <summary>
    /// Client origin for CORS
    /// </summary>
    public ICollection<string> AllowedCorsOrigins { get; set; } = [];

    /// <summary>
    /// Access token lifetime in seconds
    /// </summary>
    public int AccessTokenLifetime { get; set; } = 3600;

    /// <summary>
    /// Refresh token lifetime in seconds
    /// </summary>
    public int RefreshTokenLifetime { get; set; } = 2592000; // 30 days

    /// <summary>
    /// Whether refresh tokens are automatically rotated
    /// </summary>
    public bool RefreshTokenRotation { get; set; } = true;

    /// <summary>
    /// Whether PKCE is required for this client
    /// </summary>
    public bool RequirePkce { get; set; }

    /// <summary>
    /// Whether user consent is required
    /// </summary>
    public bool RequireConsent { get; set; } = true;

    /// <summary>
    /// Client contact information
    /// </summary>
    public ICollection<string> Contacts { get; set; } = [];

    /// <summary>
    /// Client logo URI
    /// </summary>
    public string? LogoUri { get; set; }

    /// <summary>
    /// Client privacy policy URI
    /// </summary>
    public string? PolicyUri { get; set; }

    /// <summary>
    /// Client terms of service URI
    /// </summary>
    public string? TermsOfServiceUri { get; set; }

    /// <summary>
    /// Custom claims to include in tokens
    /// </summary>
    public Dictionary<string, object> CustomClaims { get; set; } = [];

    /// <summary>
    /// Client creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Validates client configuration for OAuth2 compliance
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ClientId) &&
               !string.IsNullOrWhiteSpace(ClientName) &&
               (IsConfidential == false || !string.IsNullOrWhiteSpace(ClientSecretHash)) &&
               RedirectUris.Count > 0 &&
               AllowedGrantTypes.Count > 0;
    }

    /// <summary>
    /// Checks if a redirect URI is registered and valid for this client
    /// </summary>
    public bool IsRedirectUriValid(string? redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri)) return false;
        return RedirectUris.Contains(redirectUri, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a logout redirect URI is registered and valid
    /// </summary>
    public bool IsPostLogoutRedirectUriValid(string? logoutRedirectUri)
    {
        if (string.IsNullOrWhiteSpace(logoutRedirectUri)) return true;
        return PostLogoutRedirectUris.Count == 0 ||
               PostLogoutRedirectUris.Contains(logoutRedirectUri, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a grant type is allowed for this client
    /// </summary>
    public bool IsGrantTypeAllowed(string grantType)
    {
        return AllowedGrantTypes.Contains(grantType, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if a scope is allowed for this client
    /// </summary>
    public bool IsScopeAllowed(string scope)
    {
        return AllowedScopes.Contains(scope, StringComparer.OrdinalIgnoreCase);
    }
}
