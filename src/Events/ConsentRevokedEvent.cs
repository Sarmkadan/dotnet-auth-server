#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Events;

/// <summary>
/// Event published when a user revokes consent for a client application to access their data.
/// Critical for compliance logging (GDPR, CCPA) and understanding permission changes.
/// </summary>
public sealed class ConsentRevokedEvent : IDomainEvent
{
    /// <summary>
    /// Unique identifier of this event.
    /// </summary>
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// UTC timestamp at which the event occurred.
    /// </summary>
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Identifier of the HTTP request that produced the event, if available.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Machine-readable event type name.
    /// </summary>
    public string EventType => "consent_revoked";

    /// <summary>
    /// User who revoked consent.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Client application for which consent was revoked.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Scopes that were revoked.
    /// </summary>
    public IEnumerable<string> RevokedScopes { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Whether consent was permanent or session-scoped.
    /// </summary>
    public bool IsPermanent { get; set; }

    /// <summary>
    /// Reason for revocation.
    /// </summary>
    public string Reason { get; set; } = "user_revoked";

    /// <summary>
    /// Client IP address for audit/security purposes.
    /// </summary>
    public string? ClientIpAddress { get; set; }
}