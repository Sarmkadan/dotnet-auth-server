#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Events;

/// <summary>
/// Event published when an access token is revoked or invalidated.
/// Essential for audit logging, security monitoring, and compliance tracking.
/// Used for logout operations, compromised token recovery, and token rotation.
/// </summary>
public sealed class TokenRevokedEvent : IDomainEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? RequestId { get; set; }
    public string EventType => "token_revoked";

    /// <summary>
    /// The user whose token was revoked (if applicable).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// The client application associated with the revoked token.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// The type of token that was revoked (access_token, refresh_token, etc.).
    /// </summary>
    public string TokenType { get; set; } = string.Empty;

    /// <summary>
    /// The unique identifier of the revoked token (jti for access tokens).
    /// </summary>
    public string? TokenIdentifier { get; set; }

    /// <summary>
    /// Reason for revocation (logout, security incident, token rotation, etc.).
    /// </summary>
    public string Reason { get; set; } = "unknown";

    /// <summary>
    /// Client IP address for audit/security purposes.
    /// </summary>
    public string? ClientIpAddress { get; set; }
}