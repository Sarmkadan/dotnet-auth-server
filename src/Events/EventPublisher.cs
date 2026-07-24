#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

namespace DotnetAuthServer.Events;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

/// <summary>
/// A dead-lettered domain event: a subscriber that failed to handle events of a given type
/// repeatedly, past <see cref="EventPublisher.DeadLetterThreshold"/> consecutive attempts.
/// </summary>
/// <param name="EventTypeName">The <c>EventType</c> name of the domain event (e.g. <c>token_issued</c>).</param>
/// <param name="SubscriberTypeName">The CLR type name of the failing subscriber.</param>
/// <param name="EventId">The identifier of the last event instance that failed.</param>
/// <param name="ConsecutiveFailures">The number of consecutive failures recorded for this subscriber/event-type pair.</param>
/// <param name="LastFailureAt">UTC timestamp of the most recent failure.</param>
/// <param name="LastException">The most recent exception message recorded for this failure.</param>
public sealed record DeadLetterEntry(
    string EventTypeName,
    string SubscriberTypeName,
    string EventId,
    int ConsecutiveFailures,
    DateTime LastFailureAt,
    string LastException);

/// <summary>
/// In-process event publisher for synchronous and asynchronous event distribution.
/// Maintains a registry of subscribers and notifies them when matching events occur.
/// Suitable for single-server deployments. For distributed systems, consider
/// using a message broker like RabbitMQ or Kafka.
/// </summary>
/// <remarks>
/// See <see cref="IEventPublisher"/> for the documented ordering, isolation and backpressure
/// contract. This type additionally owns a bounded <see cref="Channel{T}"/> and a background
/// consumer loop used by <see cref="EnqueueAsync{TEvent}"/>, and maintains a dead-letter log
/// of subscribers that fail repeatedly.
/// </remarks>
public sealed class EventPublisher : IEventPublisher, IAsyncDisposable
{
    /// <summary>
    /// Default capacity of the bounded async-dispatch channel. Once full, <see cref="EnqueueAsync{TEvent}"/>
    /// awaits (applies backpressure) rather than dropping events.
    /// </summary>
    public const int DefaultChannelCapacity = 512;

    /// <summary>
    /// Number of consecutive handling failures for a given (event type, subscriber) pair after
    /// which the event is recorded in <see cref="DeadLetters"/>.
    /// </summary>
    public const int DeadLetterThreshold = 3;

    private readonly ILogger<EventPublisher> _logger;
    private readonly Dictionary<Type, List<object>> _subscribers = new();
    private readonly object _lock = new();

    private readonly Channel<Func<CancellationToken, Task>> _channel;
    private readonly CancellationTokenSource _shutdownCts = new();
    private readonly Task _dispatchLoop;

    private readonly ConcurrentDictionary<(Type EventType, Type SubscriberType), int> _consecutiveFailures = new();
    private readonly ConcurrentQueue<DeadLetterEntry> _deadLetters = new();

    /// <summary>
    /// Snapshot of events whose subscribers have failed repeatedly (see <see cref="DeadLetterThreshold"/>).
    /// Newest entries are appended at the end.
    /// </summary>
    public IReadOnlyCollection<DeadLetterEntry> DeadLetters => _deadLetters.ToArray();

    /// <summary>
    /// Initializes a new <see cref="EventPublisher"/> and starts its background async-dispatch loop.
    /// </summary>
    /// <param name="logger">Logger used for subscriber registration, dispatch, and dead-letter diagnostics.</param>
    /// <param name="channelCapacity">Maximum number of queued events awaiting async dispatch before <see cref="EnqueueAsync{TEvent}"/> applies backpressure.</param>
    /// <exception cref="ArgumentNullException"><paramref name="logger"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="channelCapacity"/> is less than 1.</exception>
    public EventPublisher(ILogger<EventPublisher> logger, int channelCapacity = DefaultChannelCapacity)
    {
        ArgumentNullException.ThrowIfNull(logger);
        if (channelCapacity < 1)
            throw new ArgumentOutOfRangeException(nameof(channelCapacity), channelCapacity, "Channel capacity must be at least 1.");

        _logger = logger;
        _channel = Channel.CreateBounded<Func<CancellationToken, Task>>(new BoundedChannelOptions(channelCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false,
        });

        _dispatchLoop = Task.Run(() => RunDispatchLoopAsync(_shutdownCts.Token));
    }

    /// <summary>
    /// Registers a subscriber to handle a specific event type.
    /// Subscribers are stored by generic type for type-safe dispatch, and invoked in the
    /// order they were registered.
    /// </summary>
    /// <param name="subscriber">The subscriber instance to register.</param>
    /// <exception cref="ArgumentNullException"><paramref name="subscriber"/> is <see langword="null"/>.</exception>
    public void Subscribe<TEvent>(IEventSubscriber<TEvent> subscriber)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(subscriber);

        lock (_lock)
        {
            var eventType = typeof(TEvent);
            if (!_subscribers.TryGetValue(eventType, out var list))
            {
                list = new List<object>();
                _subscribers[eventType] = list;
            }

            list.Add(subscriber);
            _logger.LogDebug("Subscriber registered for event type {EventType}", eventType.Name);
        }
    }

    /// <inheritdoc />
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        cancellationToken.ThrowIfCancellationRequested();

        _logger.LogInformation(
            "Publishing event {EventType} (ID: {EventId}, RequestId: {RequestId})",
            @event.EventType,
            @event.EventId,
            @event.RequestId);

        var handlers = GetHandlersSnapshot<TEvent>();
        if (handlers.Count == 0)
        {
            _logger.LogDebug("No subscribers found for event type {EventType}", typeof(TEvent).Name);
            return;
        }

        foreach (var handler in handlers)
        {
            await InvokeSubscriberIsolatedAsync(handler, @event, cancellationToken);
        }
    }

    /// <inheritdoc />
    public ValueTask EnqueueAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        ArgumentNullException.ThrowIfNull(@event);

        _logger.LogDebug(
            "Enqueueing event {EventType} (ID: {EventId}) for async dispatch",
            @event.EventType,
            @event.EventId);

        Task DispatchWork(CancellationToken ct) => DispatchAsync(@event, ct);

        return _channel.Writer.WriteAsync(DispatchWork, cancellationToken);
    }

    /// <summary>
    /// Background loop that reads queued dispatch work off the channel and executes it
    /// sequentially, one event at a time, preserving per-event-type ordering established by
    /// the writer. Runs until the publisher is disposed.
    /// </summary>
    private async Task RunDispatchLoopAsync(CancellationToken shutdownToken)
    {
        try
        {
            await foreach (var dispatch in _channel.Reader.ReadAllAsync(shutdownToken))
            {
                try
                {
                    await dispatch(shutdownToken);
                }
                catch (OperationCanceledException)
                {
                    // Dispatch itself was cancelled by shutdown; stop draining further work.
                    break;
                }
                catch (Exception ex)
                {
                    // DispatchAsync isolates subscriber failures internally; reaching here means
                    // something unexpected escaped that isolation. Log and keep the loop alive.
                    _logger.LogError(ex, "Unhandled failure in async event dispatch loop");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }
    }

    private async Task DispatchAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        var handlers = GetHandlersSnapshot<TEvent>();
        if (handlers.Count == 0)
        {
            _logger.LogDebug("No subscribers found for event type {EventType}", typeof(TEvent).Name);
            return;
        }

        foreach (var handler in handlers)
        {
            await InvokeSubscriberIsolatedAsync(handler, @event, cancellationToken);
        }
    }

    private List<object> GetHandlersSnapshot<TEvent>()
        where TEvent : IDomainEvent
    {
        lock (_lock)
        {
            return _subscribers.TryGetValue(typeof(TEvent), out var handlers)
                ? new List<object>(handlers)
                : new List<object>();
        }
    }

    /// <summary>
    /// Invokes a single subscriber, catching and logging any exception it throws so that it
    /// cannot abort publication or block sibling subscribers. Tracks consecutive failures per
    /// (event type, subscriber type) and records a dead-letter entry once
    /// <see cref="DeadLetterThreshold"/> is reached.
    /// </summary>
    private async Task InvokeSubscriberIsolatedAsync<TEvent>(object handler, TEvent @event, CancellationToken cancellationToken)
        where TEvent : IDomainEvent
    {
        if (handler is not IEventSubscriber<TEvent> subscriber)
            return;

        var key = (typeof(TEvent), handler.GetType());

        try
        {
            await subscriber.HandleAsync(@event, cancellationToken);
            _consecutiveFailures.TryRemove(key, out _);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Genuine cancellation of the publish/dispatch operation itself; propagate.
            throw;
        }
        catch (Exception ex)
        {
            var failureCount = _consecutiveFailures.AddOrUpdate(key, 1, (_, count) => count + 1);

            _logger.LogError(
                ex,
                "Error handling event {EventType} (ID: {EventId}) in subscriber {SubscriberType} (consecutive failures: {FailureCount})",
                @event.EventType,
                @event.EventId,
                handler.GetType().Name,
                failureCount);

            if (failureCount >= DeadLetterThreshold)
            {
                var entry = new DeadLetterEntry(
                    EventTypeName: @event.EventType,
                    SubscriberTypeName: handler.GetType().Name,
                    EventId: @event.EventId,
                    ConsecutiveFailures: failureCount,
                    LastFailureAt: DateTime.UtcNow,
                    LastException: ex.Message);

                _deadLetters.Enqueue(entry);

                _logger.LogCritical(
                    "Subscriber {SubscriberType} dead-lettered for event type {EventType} after {FailureCount} consecutive failures (last event ID: {EventId})",
                    handler.GetType().Name,
                    @event.EventType,
                    failureCount,
                    @event.EventId);
            }
        }
    }

    /// <summary>
    /// Stops accepting further async-dispatch work, drains any in-flight dispatch, and releases
    /// the background dispatch loop. Called automatically by dependency-injection containers
    /// that own this instance's lifetime.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        _channel.Writer.TryComplete();
        _shutdownCts.Cancel();

        try
        {
            await _dispatchLoop;
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown.
        }

        _shutdownCts.Dispose();
    }
}
