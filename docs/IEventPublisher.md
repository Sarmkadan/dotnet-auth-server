# IEventPublisher Interface

Namespace: `DotnetAuthServer.Events`

## Summary

Interface for publishing domain events to subscribers.
Enables loosely-coupled event-driven patterns within the auth server.

## Remarks

### Delivery semantics (documented contract, must not change without updating callers):

- `PublishAsync{TEvent}` is synchronous: subscribers for `{TEvent}` are invoked sequentially, in the order they were registered via `EventPublisher.Subscribe{TEvent}`. Invocation order is stable and deterministic.
- A throwing subscriber is isolated: its exception is caught and logged, and every remaining subscriber still runs. `PublishAsync{TEvent}` never rethrows a subscriber's exception, so a misbehaving audit/analytics hook cannot fail the caller's OAuth flow. The only exception that propagates out of `PublishAsync{TEvent}` is `OperationCanceledException` raised because `cancellationToken` (or one derived from it) was cancelled.
- `EnqueueAsync{TEvent}` hands the event to a bounded, in-process `System.Threading.Channels.Channel{T}` and returns once the event has been accepted onto the channel; actual subscriber dispatch happens later, off the caller's thread, preserving the same sequential-per-event-type ordering and isolation guarantees as `PublishAsync{TEvent}`. When the channel is full, the call applies backpressure by awaiting until space is available (or the token is cancelled) rather than dropping the event.
- A subscriber that fails repeatedly for the same event type is recorded in the dead-letter log (see `EventPublisher.DeadLetters`) once its consecutive failure count for that event type reaches `EventPublisher.DeadLetterThreshold`.

## Methods

### PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)

Publishes a domain event to all registered subscribers synchronously, in registration order. Per-subscriber exceptions are caught and logged rather than propagated, so a failing subscriber cannot abort the caller's workflow (e.g. token issuance).

#### Parameters

- `@event`: The domain event instance to publish.
- `cancellationToken`: Token used to cancel the publish operation.

#### Exceptions

- `ArgumentNullException`: `{@event}` is `null`.
- `OperationCanceledException`: `{cancellationToken}` was cancelled.

#### Type Parameters

- `TEvent`: The type of the domain event, must implement `IDomainEvent`.

#### Returns

A `Task` representing the asynchronous operation.

### EnqueueAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)

Queues a domain event for asynchronous, off-thread dispatch via a bounded channel. Returns once the event has been accepted onto the channel; subscribers run later on a background consumer with the same ordering and isolation guarantees as `PublishAsync{TEvent}`. If the channel is at capacity, this call awaits (applying backpressure) until room is available or cancellation is requested.

#### Parameters

- `@event`: The domain event instance to enqueue.
- `cancellationToken`: Token used to cancel the enqueue operation while waiting for channel capacity.

#### Exceptions

- `ArgumentNullException`: `{@event}` is `null`.
- `OperationCanceledException`: `{cancellationToken}` was cancelled, or the publisher is shutting down.

#### Type Parameters

- `TEvent`: The type of the domain event, must implement `IDomainEvent`.

#### Returns

A `ValueTask` representing the asynchronous operation.

## See Also

- `IEventSubscriber{TEvent}`
- `EventPublisher`
- `IDomainEvent`