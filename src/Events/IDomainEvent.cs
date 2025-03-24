// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Events;

/// <summary>
/// Base interface for domain events published throughout the authorization server.
/// Enables decoupled communication between different parts of the system.
/// Examples: token issued, user authenticated, consent granted.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier for this specific event instance.
    /// Useful for deduplication and tracking.
    /// </summary>
    string EventId { get; }

    /// <summary>
    /// Timestamp when the event occurred (UTC).
    /// </summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// Request ID for correlation with HTTP request that triggered this event.
    /// Helps with request tracing across logs.
    /// </summary>
    string? RequestId { get; }

    /// <summary>
    /// Human-readable name of the event type.
    /// </summary>
    string EventType { get; }
}
