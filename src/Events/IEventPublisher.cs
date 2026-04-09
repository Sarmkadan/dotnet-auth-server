// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Events;

/// <summary>
/// Interface for publishing domain events to subscribers.
/// Enables loosely-coupled event-driven patterns within the auth server.
/// Implementations can be synchronous or asynchronous depending on use case.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a domain event to all registered subscribers.
    /// Subscribers are notified synchronously in registration order.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;
}

/// <summary>
/// Interface for subscribing to domain events.
/// Implementations receive notifications when matching events are published.
/// </summary>
public interface IEventSubscriber<TEvent>
    where TEvent : IDomainEvent
{
    /// <summary>
    /// Handles publication of a domain event.
    /// Should complete quickly to avoid blocking event publication.
    /// </summary>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
