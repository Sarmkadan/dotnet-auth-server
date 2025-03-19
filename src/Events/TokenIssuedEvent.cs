// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Events;

/// <summary>
/// Event published when an access token is successfully issued.
/// Subscribers can use this for audit logging, analytics, or webhook notifications.
/// </summary>
public class TokenIssuedEvent : IDomainEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? RequestId { get; set; }
    public string EventType => "token_issued";

    /// <summary>
    /// The user for whom the token was issued.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// The client application requesting the token.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Grant type used (authorization_code, refresh_token, client_credentials, etc.)
    /// </summary>
    public string GrantType { get; set; } = string.Empty;

    /// <summary>
    /// Scopes granted in the token.
    /// </summary>
    public IEnumerable<string> Scopes { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Token lifetime in seconds.
    /// </summary>
    public int ExpiresInSeconds { get; set; }

    /// <summary>
    /// Client IP address for audit/security purposes.
    /// </summary>
    public string? ClientIpAddress { get; set; }
}
