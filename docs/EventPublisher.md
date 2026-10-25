# EventPublisher

The `EventPublisher` class is the in-process implementation of [`IEventPublisher`](IEventPublisher.md). It keeps a registry of typed subscribers, delivers domain events to them either synchronously (`PublishAsync`) or through a bounded background queue (`EnqueueAsync`), isolates subscriber failures, and records subscribers that fail repeatedly in a dead-letter log.

It is a sealed class. The host registers it as a singleton behind the `IEventPublisher` interface in `Program.cs`, so application code that only publishes depends on `IEventPublisher`. Subscribing is available only on the concrete type, because `Subscribe` is not part of the interface.

The class implements `IAsyncDisposable`. The dependency-injection container calls `DisposeAsync` when the singleton is disposed.

## Constants

### DefaultChannelCapacity
`public const int DefaultChannelCapacity = 512`

The default capacity of the bounded channel used by `EnqueueAsync`. The constructor uses it when no `channelCapacity` argument is supplied.

### DeadLetterThreshold
`public const int DeadLetterThreshold = 3`

The number of consecutive handling failures for one (event type, subscriber type) pair at which a dead-letter entry is recorded. See [Failure handling](#failure-handling) below.

## Types

### DeadLetterEntry
`public sealed record DeadLetterEntry(string EventTypeName, string SubscriberTypeName, string EventId, int ConsecutiveFailures, DateTime LastFailureAt, string LastException)`

An immutable record describing one dead-lettered failure. It is declared in the same file as `EventPublisher`.

| Property | Description |
|---|---|
| `EventTypeName` | The `EventType` value of the domain event (for example `token_issued`). |
| `SubscriberTypeName` | The short name (`Type.Name`, not the full CLR name) of the failing subscriber's runtime type. |
| `EventId` | The `EventId` of the event instance whose handling failed most recently. |
| `ConsecutiveFailures` | The consecutive failure count for this (event type, subscriber type) pair at the time the entry was written. |
| `LastFailureAt` | UTC timestamp at which the entry was written. |
| `LastException` | The `Message` of the exception thrown by the subscriber. |

## API

### Constructor

`public EventPublisher(ILogger<EventPublisher> logger, int channelCapacity = DefaultChannelCapacity)`

Creates the publisher and immediately starts its background dispatch loop with `Task.Run`. The loop runs until `DisposeAsync` is called.

| Parameter | Description |
|---|---|
| `logger` | Receives registration, dispatch, and dead-letter diagnostics. |
| `channelCapacity` | Maximum number of queued dispatch items. When the queue is full, `EnqueueAsync` waits for room. Must be at least 1. |

Exceptions:
- `ArgumentNullException`: `logger` is `null`.
- `ArgumentOutOfRangeException`: `channelCapacity` is less than 1.

### Subscribe

`public void Subscribe<TEvent>(IEventSubscriber<TEvent> subscriber) where TEvent : IDomainEvent`

Registers a subscriber for events of type `TEvent`. Subscribers are stored per event type and invoked in registration order. The same instance may be registered more than once, and each registration is invoked separately.

The method takes a lock, so it is safe to call from several threads. A publish operation takes a snapshot of the subscriber list when it starts. A subscriber added during a publish is not invoked for that publish.

Exceptions:
- `ArgumentNullException`: `subscriber` is `null`. This exception is thrown synchronously.

### PublishAsync

`public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IDomainEvent`

Delivers `@event` to every subscriber for `TEvent` on the calling task. Subscribers run one after another in registration order. The returned task completes after the last subscriber has finished. If there are no subscribers, the method logs at debug level and returns.

Subscriber exceptions do not propagate. They are logged, counted, and possibly dead-lettered (see [Failure handling](#failure-handling)), and the remaining subscribers still run.

Exceptions:
- `ArgumentNullException`: `@event` is `null`. Because the method is `async`, this exception is delivered through the returned `Task`, not thrown synchronously.
- `OperationCanceledException`: `cancellationToken` was cancelled before the call or while a subscriber was running. This is the only exception that leaves the method. It is raised only when the token that was passed in is cancelled, not when a subscriber throws `OperationCanceledException` for some other reason.

### EnqueueAsync

`public ValueTask EnqueueAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : IDomainEvent`

Places `@event` on the bounded channel and returns. A single background loop takes items off the channel and dispatches them. The dispatch uses the same subscriber list, order, and failure handling as `PublishAsync`.

The method does not wait for subscribers to run. Its task completes when the item has been written to the channel. If the channel is full, the call waits until there is room or until `cancellationToken` is cancelled. Events are not dropped.

Because a single loop processes the channel, queued events are dispatched one at a time in the order they were accepted. Events sent through `PublishAsync` are not ordered relative to queued events.

Exceptions:
- `ArgumentNullException`: `@event` is `null`. This exception is thrown synchronously, because the method is not `async`.
- `OperationCanceledException`: `cancellationToken` was cancelled while the call was waiting for channel capacity.
- `ChannelClosedException`: the publisher is shutting down or has been disposed, so the channel no longer accepts items. The `IEventPublisher` contract describes this case as `OperationCanceledException`. The concrete type throws `ChannelClosedException` instead, so callers that may run during shutdown should catch both.

### DeadLetters

`public IReadOnlyCollection<DeadLetterEntry> DeadLetters { get; }`

Returns a snapshot of the dead-letter log, with the oldest entry first. Each access creates a new array, so the result does not change after it is returned. The log is kept in memory, is not capped, and has no method for removing entries. A process restart clears it.

### DisposeAsync

`public ValueTask DisposeAsync()`

Stops the publisher:
1. Marks the channel as complete, so `EnqueueAsync` can no longer write to it.
2. Cancels the internal shutdown token. This token is also passed to the dispatch loop, so a subscriber that is running at that moment sees a cancelled token.
3. Waits for the dispatch loop to exit, then releases the internal `CancellationTokenSource`.

Items that are still in the queue and have not started dispatch are not delivered. Despite its name, `DisposeAsync` does not drain the queue. Call it only after all producers have finished. Calls to `PublishAsync` that are already running are not affected by it.

## Failure handling

Each subscriber invocation is wrapped in its own `try`/`catch`. The publisher tracks a counter for each (event type, subscriber runtime type) pair:

- A successful handle removes the counter for that pair.
- A thrown exception increments the counter and logs an error that includes the event type, event ID, subscriber name, and the current count.
- When the count is `DeadLetterThreshold` (3) or higher, a `DeadLetterEntry` is added to `DeadLetters` and a critical message is logged. The counter is not reset after an entry is written, so each further failure adds another entry until the subscriber succeeds once.

The counter is keyed by the subscriber's runtime type, not by instance. Two registered instances of the same class share one counter.

A subscriber that is cancelled because the token it received was cancelled is not counted as a failure. Its `OperationCanceledException` propagates to the caller only when the token is cancelled. Otherwise the exception is handled like any other failure.

## Usage

Register the publisher, subscribe with the concrete type, then publish:

```csharp
var publisher = new EventPublisher(loggerFactory.CreateLogger<EventPublisher>());

publisher.Subscribe<TokenIssuedEvent>(auditSubscriber);

// Synchronous: returns after every subscriber has run.
await publisher.PublishAsync(new TokenIssuedEvent { /* ... */ });

// Asynchronous: returns once the event is queued.
await publisher.EnqueueAsync(new TokenIssuedEvent { /* ... */ }, cancellationToken);

await publisher.DisposeAsync();
```

To check for repeatedly failing subscribers:

```csharp
foreach (var entry in publisher.DeadLetters)
{
    logger.LogWarning(
        "{Subscriber} failed {Count} times on {EventType}: {Error}",
        entry.SubscriberTypeName,
        entry.ConsecutiveFailures,
        entry.EventTypeName,
        entry.LastException);
}
```

## Remarks

- Use `PublishAsync` when the caller must know that subscribers have finished, for example before it writes a response. Use `EnqueueAsync` when the caller must not wait for subscribers, such as in a request path that should return quickly.
- Subscriber exceptions never reach the publisher's caller, so a failing audit hook cannot break token issuance. A subscriber that keeps failing is visible only through the logs and `DeadLetters`.
- The publisher is in-process only. Events are lost if the process stops. For delivery across processes, use a message broker.
- The constants are `public`, so they can be read but not changed. The channel capacity is set only through the constructor.
- For the full delivery contract, see [`IEventPublisher`](IEventPublisher.md).
