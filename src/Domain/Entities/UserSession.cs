#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Entities;

/// <summary>
/// Represents an authenticated user session tied to a specific OAuth2 token grant.
/// Created when a user successfully authenticates and receives tokens. Each session
/// tracks the originating client, IP, user-agent and granted scopes for audit and
/// revocation purposes.
/// </summary>
public sealed class UserSession
{
    /// <summary>Unique session identifier.</summary>
    public string SessionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Subject (user) that owns this session.</summary>
    public string UserId { get; set; } = null!;

    /// <summary>OAuth2 client that initiated the session.</summary>
    public string ClientId { get; set; } = null!;

    /// <summary>IP address at session creation time.</summary>
    public string? IpAddress { get; set; }

    /// <summary>User-Agent header at session creation time.</summary>
    public string? UserAgent { get; set; }

    /// <summary>Space-separated list of scopes granted in this session.</summary>
    public string GrantedScopes { get; set; } = string.Empty;

    /// <summary>Session creation timestamp (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Session expiry timestamp (UTC).</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Timestamp of the last activity recorded for this session (UTC).</summary>
    public DateTime? LastActivityAt { get; set; }

    /// <summary>Whether this session has been explicitly revoked.</summary>
    public bool IsRevoked { get; set; }

    /// <summary>Reason for revocation, set when <see cref="IsRevoked"/> is true.</summary>
    public string? RevocationReason { get; set; }

    /// <summary>
    /// Returns true when the session is active (not revoked and not expired).
    /// </summary>
    public bool IsActive() => !IsRevoked && ExpiresAt > DateTime.UtcNow;

    /// <summary>
    /// Marks the session as revoked with an optional reason.
    /// </summary>
    public void Revoke(string? reason = null)
    {
        IsRevoked = true;
        RevocationReason = reason;
    }

    /// <summary>
    /// Updates the last activity timestamp to now.
    /// </summary>
    public void Touch() => LastActivityAt = DateTime.UtcNow;
}
