#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

namespace DotnetAuthServer.Events;

using System.Collections.Generic;
using DotnetAuthServer.Exceptions;
using Microsoft.Extensions.Logging;

/// <summary>
/// In-process event publisher for synchronous event distribution.
/// Maintains a registry of subscribers and notifies them when matching events occur.
/// Suitable for single-server deployments. For distributed systems, consider
/// using a message broker like RabbitMQ or Kafka.
/// </summary>
public sealed class EventPublisher : IEventPublisher
{
    private readonly ILogger<EventPublisher> _logger;
    private readonly Dictionary<Type, List<object>> _subscribers = new();
    private readonly object _lock = new();

    public EventPublisher(ILogger<EventPublisher> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Registers a subscriber to handle a specific event type.
    /// Subscribers are stored by generic type for type-safe dispatch.
    /// </summary>
    public void Subscribe<TEvent>(IEventSubscriber<TEvent> subscriber)
        where TEvent : IDomainEvent
    {
        if (subscriber is null)
            throw new ArgumentNullException(nameof(subscriber));

        lock (_lock)
        {
            var eventType = typeof(TEvent);
            if (!_subscribers.ContainsKey(eventType))
                _subscribers[eventType] = new List<object>();

            _subscribers[eventType].Add(subscriber);
            _logger.LogDebug("Subscriber registered for event type {EventType}", eventType.Name);
        }
    }

    /// <summary>
    /// Publishes an event to all registered subscribers.
    /// Executes synchronously in order of subscription.
    /// Catches and logs exceptions from subscribers to prevent cascade failures.
    /// </summary>
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        if (@event is null)
            throw new ArgumentNullException(nameof(@event));

        var eventType = typeof(TEvent);

        _logger.LogInformation(
            "Publishing event {EventType} (ID: {EventId}, RequestId: {RequestId})",
            @event.EventType,
            @event.EventId,
            @event.RequestId);

        List<object>? handlers;
        lock (_lock)
        {
            _subscribers.TryGetValue(eventType, out handlers);
        }

        if (handlers is null || handlers.Count == 0)
        {
            _logger.LogDebug("No subscribers found for event type {EventType}", eventType.Name);
            return;
        }

        var exceptions = new List<Exception>();
        foreach (var handler in handlers)
        {
            try
            {
                var subscriber = handler as IEventSubscriber<TEvent>;
                if (subscriber is not null)
                {
                    await subscriber.HandleAsync(@event, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Event {EventType} handling was cancelled", @event.EventType);
                // Don't suppress cancellation exceptions
                throw;
            }
            catch (AuthServerException ex)
            {
                _logger.LogError(
                    ex,
                    "AuthServerException handling event {EventType} in subscriber {SubscriberType}",
                    @event.EventType,
                    handler.GetType().Name);
                exceptions.Add(ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error handling event {EventType} in subscriber {SubscriberType}",
                    @event.EventType,
                    handler.GetType().Name);
                exceptions.Add(ex);
            }
        }

        // If any subscriber failed with AuthServerException, rethrow it
        if (exceptions.Count > 0)
        {
            if (exceptions.Count == 1)
            {
                throw exceptions[0];
            }

            throw new AggregateException("Multiple subscribers failed to handle event", exceptions);
        }
    }
}
