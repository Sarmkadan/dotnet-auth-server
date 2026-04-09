// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Events;

/// <summary>
/// Event published when a user grants consent for a client application to access their data.
/// Essential for compliance logging (GDPR, CCPA) and understanding user permissions.
/// </summary>
public class ConsentGrantedEvent : IDomainEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? RequestId { get; set; }
    public string EventType => "consent_granted";

    /// <summary>
    /// User who granted consent.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Client application for which consent was granted.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Scopes to which user consented.
    /// </summary>
    public IEnumerable<string> GrantedScopes { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Whether consent is permanent or session-scoped.
    /// </summary>
    public bool IsPermanent { get; set; }

    /// <summary>
    /// Client IP address.
    /// </summary>
    public string? ClientIpAddress { get; set; }
}
