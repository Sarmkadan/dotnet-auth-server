// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Models;

/// <summary>
/// Represents a user consent request decision
/// </summary>
public class ConsentRequest
{
    /// <summary>
    /// User ID providing consent
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Client ID requesting the scopes
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Scopes being granted
    /// </summary>
    public ICollection<string> GrantedScopes { get; set; } = [];

    /// <summary>
    /// Whether the user granted or denied consent
    /// </summary>
    public bool Approved { get; set; }

    /// <summary>
    /// Reason for denial (if applicable)
    /// </summary>
    public string? DenialReason { get; set; }

    /// <summary>
    /// Whether to remember this consent decision
    /// </summary>
    public bool RememberConsent { get; set; }

    /// <summary>
    /// IP address of the requester
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent of the requester
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Gets scopes as a space-separated string
    /// </summary>
    public string GetScopesString()
    {
        return string.Join(" ", GrantedScopes);
    }

    /// <summary>
    /// Validates the consent request
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(UserId) &&
               !string.IsNullOrWhiteSpace(ClientId) &&
               (Approved == false || GrantedScopes.Count > 0);
    }
}
