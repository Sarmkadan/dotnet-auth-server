#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Events;

/// <summary>
/// Interface for publishing domain events to subscribers.
/// Enables loosely-coupled event-driven patterns within the auth server.
/// </summary>
/// <remarks>
/// Delivery semantics (documented contract, must not change without updating callers):
/// <list type="bullet">
/// <item><description>
/// <see cref="PublishAsync{TEvent}"/> is synchronous: subscribers for <typeparamref name="TEvent"/>
/// are invoked sequentially, in the order they were registered via
/// <see cref="EventPublisher.Subscribe{TEvent}"/>. Invocation order is stable and deterministic.
/// </description></item>
/// <item><description>
/// A throwing subscriber is isolated: its exception is caught and logged, and every remaining
/// subscriber still runs. <see cref="PublishAsync{TEvent}"/> never rethrows a subscriber's
/// exception, so a misbehaving audit/analytics hook cannot fail the caller's OAuth flow.
/// The only exception that propagates out of <see cref="PublishAsync{TEvent}"/> is
/// <see cref="OperationCanceledException"/> raised because <paramref name="cancellationToken"/>
/// (or one derived from it) was cancelled.
/// </description></item>
/// <item><description>
/// <see cref="EnqueueAsync{TEvent}"/> hands the event to a bounded, in-process
/// <see cref="System.Threading.Channels.Channel{T}"/> and returns once the event has been
/// accepted onto the channel; actual subscriber dispatch happens later, off the caller's
/// thread, preserving the same sequential-per-event-type ordering and isolation guarantees
/// as <see cref="PublishAsync{TEvent}"/>. When the channel is full, the call applies
/// backpressure by awaiting until space is available (or the token is cancelled) rather than
/// dropping the event.
/// </description></item>
/// <item><description>
/// A subscriber that fails repeatedly for the same event type is recorded in the dead-letter
/// log (see <see cref="EventPublisher.DeadLetters"/>) once its consecutive failure count for
/// that event type reaches <see cref="EventPublisher.DeadLetterThreshold"/>.
/// </description></item>
/// </list>
/// </remarks>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a domain event to all registered subscribers synchronously, in registration
    /// order. Per-subscriber exceptions are caught and logged rather than propagated, so a
    /// failing subscriber cannot abort the caller's workflow (e.g. token issuance).
    /// </summary>
    /// <param name="event">The domain event instance to publish.</param>
    /// <param name="cancellationToken">Token used to cancel the publish operation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="event"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent;

    /// <summary>
    /// Queues a domain event for asynchronous, off-thread dispatch via a bounded channel.
    /// Returns once the event has been accepted onto the channel; subscribers run later on a
    /// background consumer with the same ordering and isolation guarantees as
    /// <see cref="PublishAsync{TEvent}"/>. If the channel is at capacity, this call awaits
    /// (applying backpressure) until room is available or cancellation is requested.
    /// </summary>
    /// <param name="event">The domain event instance to enqueue.</param>
    /// <param name="cancellationToken">Token used to cancel the enqueue operation while waiting for channel capacity.</param>
    /// <exception cref="ArgumentNullException"><paramref name="event"/> is <see langword="null"/>.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled, or the publisher is shutting down.</exception>
    ValueTask EnqueueAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
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
    /// Should complete quickly to avoid blocking event publication. A thrown exception is
    /// caught and logged by the publisher; it does not propagate to the caller of
    /// <see cref="IEventPublisher.PublishAsync{TEvent}"/> or block sibling subscribers.
    /// </summary>
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
