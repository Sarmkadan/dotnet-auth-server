#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Events;

/// <summary>
/// Event published when a new OAuth2/OIDC client is dynamically registered.
/// Dynamic registration is a high-risk action that creates new credentials and
/// access paths. This event feeds SIEM systems for security monitoring.
/// </summary>
public sealed class ClientRegisteredEvent : IDomainEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public string? RequestId { get; set; }
    public string EventType => "client_registered";

    /// <summary>
    /// The newly registered client ID.
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name of the client.
    /// </summary>
    public string ClientName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the client is confidential (has credentials) or public.
    /// </summary>
    public bool IsConfidential { get; set; }

    /// <summary>
    /// Grant types supported by the client.
    /// </summary>
    public IEnumerable<string> GrantTypes { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Scopes requested during registration.
    /// </summary>
    public IEnumerable<string> RequestedScopes { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Redirect URIs registered by the client.
    /// </summary>
    public IEnumerable<string> RedirectUris { get; set; } = Enumerable.Empty<string>();

    /// <summary>
    /// Authentication method for token endpoint.
    /// </summary>
    public string TokenEndpointAuthMethod { get; set; } = "none";

    /// <summary>
    /// Client IP address for audit/security purposes.
    /// </summary>
    public string? ClientIpAddress { get; set; }

    /// <summary>
    /// Whether PKCE is required for this client.
    /// </summary>
    public bool RequirePkce { get; set; }
}