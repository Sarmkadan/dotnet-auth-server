// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Entities;

using DotnetAuthServer.Domain.Enums;

/// <summary>
/// Represents user consent for a client's scope access
/// </summary>
public class Consent
{
    /// <summary>
    /// Unique consent identifier
    /// </summary>
    public string ConsentId { get; set; } = null!;

    /// <summary>
    /// User ID that provided consent
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Client ID requesting the scopes
    /// </summary>
    public string ClientId { get; set; } = null!;

    /// <summary>
    /// Scopes the user consented to
    /// </summary>
    public string GrantedScopes { get; set; } = null!;

    /// <summary>
    /// Current consent status
    /// </summary>
    public ConsentStatus Status { get; set; } = ConsentStatus.Pending;

    /// <summary>
    /// Consent expiration timestamp (null for indefinite)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Whether this is an offline consent (long-lived)
    /// </summary>
    public bool IsOfflineConsent { get; set; }

    /// <summary>
    /// Reason for denial (if denied)
    /// </summary>
    public string? DenialReason { get; set; }

    /// <summary>
    /// User IP address when consent was granted
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent when consent was granted
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Consent timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Checks if the consent is still valid and approved
    /// </summary>
    public bool IsValidAndApproved()
    {
        if (Status != ConsentStatus.Approved) return false;
        if (ExpiresAt != null && DateTime.UtcNow >= ExpiresAt) return false;
        return true;
    }

    /// <summary>
    /// Checks if the consent has expired
    /// </summary>
    public bool IsExpired()
    {
        return ExpiresAt != null && DateTime.UtcNow >= ExpiresAt;
    }

    /// <summary>
    /// Grants consent for the requested scopes
    /// </summary>
    public void Grant(string scopes, string? ipAddress = null, string? userAgent = null)
    {
        Status = ConsentStatus.Approved;
        GrantedScopes = scopes;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Denies consent
    /// </summary>
    public void Deny(string? reason = null)
    {
        Status = ConsentStatus.Rejected;
        DenialReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Revokes previously granted consent
    /// </summary>
    public void Revoke(string? reason = null)
    {
        Status = ConsentStatus.Expired;
        DenialReason = reason ?? "Manually revoked";
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a specific scope is in the granted scopes
    /// </summary>
    public bool HasScopeConsent(string scope)
    {
        if (!IsValidAndApproved()) return false;
        return GrantedScopes.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Contains(scope, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the list of scopes user consented to
    /// </summary>
    public IEnumerable<string> GetGrantedScopes()
    {
        return GrantedScopes.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }
}
