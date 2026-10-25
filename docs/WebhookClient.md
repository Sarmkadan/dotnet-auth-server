# WebhookClient

**Namespace:** `DotnetAuthServer.Integration`

Sends webhook notifications about authorization server events to external HTTP endpoints. Each call posts one JSON envelope (`WebhookPayload`) containing the event metadata and the serialized event body. The client makes a single delivery attempt per call and reports the outcome as a `WebhookResult`. It does not throw for delivery failures.

`sealed`. It is registered as a typed client (`AddHttpClient<WebhookClient>()` in `Program.cs`), so it is created per injection with an `HttpClient` from the factory. Its `HttpClient.Timeout` is set from `WebhookOptions.Timeout`. The class keeps no mutable state.

The class summary mentions retry and backoff, but `WebhookClient` itself does not retry. See [Retries](#retries).

---

## Constructor

### `WebhookClient(HttpClient httpClient, ILogger<WebhookClient> logger, WebhookOptions options)`

Stores the three dependencies. Throws `ArgumentNullException` if any argument is `null`.

| Parameter | Type | Description |
|---|---|---|
| `httpClient` | `HttpClient` | Sends the POST request. |
| `logger` | `ILogger<WebhookClient>` | Records delivery results and failures. |
| `options` | `WebhookOptions` | Supplies `Enabled` and `Timeout`. |

---

## Methods

### `Task<WebhookResult> SendEventWebhookAsync(string webhookUrl, IDomainEvent @event, CancellationToken cancellationToken = default)`

Delivers one event to `webhookUrl`. The method returns a result for every outcome except invalid arguments.

| Parameter | Type | Description |
|---|---|---|
| `webhookUrl` | `string` | Absolute URL that receives the POST. |
| `event` | `IDomainEvent` | The event to send. |
| `cancellationToken` | `CancellationToken` | Linked with the per-request timeout (see [Timeout and cancellation](#timeout-and-cancellation)). |

**Returns:** a `WebhookResult`:

| Outcome | `Success` | `Error` |
|---|---|---|
| Response with 2xx status | `true` | `null` |
| Response with 4xx status | `false` | `"HTTP {StatusCode}"`, for example `"HTTP BadRequest"` |
| `WebhookOptions.Enabled` is `false` | `false` | `"Webhooks disabled or URL missing"` |
| Timeout, caller cancellation, `HttpRequestException`, or any other exception during the request | `false` | `"Delivery failed"` |
| Response with 1xx, 3xx or 5xx status | `false` | `"Delivery failed"` |

**Throws:**

| Exception | When |
|---|---|
| `ArgumentException` | `webhookUrl` is `null`, empty or whitespace. |
| `ArgumentNullException` | `event` is `null`. |
| `JsonException` | The event cannot be serialized to JSON. Payload creation runs before the `try` block, so this is not converted to a result. |

These checks run before the enabled check. A disabled client therefore still throws for an invalid URL or a `null` event.

**Behavior:**

1. Validates `webhookUrl` and `event`.
2. If `Enabled` is `false`, returns the disabled result without sending anything.
3. Builds the payload (see [Payload](#payload)).
4. Creates a linked cancellation token that is cancelled after `Timeout` or when `cancellationToken` is cancelled.
5. POSTs the payload as UTF-8 JSON with `Content-Type: application/json`.
6. On a 2xx response, logs an information message and returns `Success = true`.
7. On a 4xx response, logs a warning and returns the `HTTP {StatusCode}` error.
8. For any other status, falls through to the `"Delivery failed"` result. No log entry is written for this case.

---

## Payload

Each request body is a `WebhookPayload` serialized with the default `JsonSerializer` options, so property names are PascalCase on the wire (`EventId`, `EventType`, and so on).

| Field | Source |
|---|---|
| `EventId` | `IDomainEvent.EventId` |
| `EventType` | `IDomainEvent.EventType` |
| `OccurredAt` | `IDomainEvent.OccurredAt` (UTC) |
| `RequestId` | `IDomainEvent.RequestId`, or `null` |
| `Data` | The event object serialized with camelCase property names. |

`Data` is serialized with the static type `IDomainEvent`, because the generic argument is inferred from the parameter type. Only members declared on `IDomainEvent` appear in `Data`. Properties declared on a derived event class are not included.

---

## Timeout and cancellation

- Each call creates a linked token that cancels after `WebhookOptions.Timeout` (default 10 seconds). The `HttpClient.Timeout` set in DI is a second, independent limit.
- Cancellation of `cancellationToken` by the caller is not rethrown. It is logged as a timeout and returns `Error = "Delivery failed"`.
- Both cases write a warning log entry.

---

## Logging

| Event | Level | Message |
|---|---|---|
| 2xx response | Information | Webhook sent successfully to `{Url}` for event `{EventType}` |
| 4xx response | Warning | Webhook received client error `{StatusCode}` from `{Url}` |
| Timeout or cancellation | Warning | Webhook delivery timed out for `{Url}` |
| `HttpRequestException` | Warning | Webhook delivery failed for `{Url}` (with exception) |
| Any other exception | Error | Webhook delivery failed for `{Url}` with unexpected error (with exception) |

The URL is logged as given, including any query string.

---

## Retries

`WebhookClient` makes one attempt per call. A 5xx response or a transport failure returns `Success = false` immediately. The class does not read `WebhookOptions.MaxRetries`, `InitialRetryDelayMs` or `MaxRetryDelayMs`.

Retry behavior depends on the `HttpClient` that is passed in. The DI registration in `Program.cs` does not attach a resilience handler, so calls made through it are not retried. `HttpClientFactory.CreateWebhookClient` builds a client with a resilient handler, but that factory method is not part of this class's registration. Callers that need retries must supply such a client.

---

## Related types

### `WebhookResult`

Result of one delivery attempt.

| Property | Type | Description |
|---|---|---|
| `Success` | `bool` | `true` when the endpoint returned a 2xx status. |
| `Error` | `string?` | Short failure description, or `null` on success. |

Overrides `ToString()` to return `WebhookResult { Success = ..., Error = ... }`.

### `WebhookPayload`

The JSON envelope described in [Payload](#payload).

| Property | Type | Description |
|---|---|---|
| `EventId` | `string` | Defaults to `""`. |
| `EventType` | `string` | Defaults to `""`. |
| `OccurredAt` | `DateTime` | UTC time of the event. |
| `RequestId` | `string?` | Originating request identifier, if any. |
| `Data` | `JsonElement` | Serialized event body. |

Overrides `ToString()`. The output omits `Data`.

### `WebhookOptions`

Configuration bound from the `Webhooks` configuration section (see `Program.cs`). Validated with data annotations on start.

| Property | Type | Default | Used by `WebhookClient` | Validation attribute |
|---|---|---|---|---|
| `Enabled` | `bool` | `true` | Yes. When `false`, delivery is skipped. | `[Required]` |
| `MaxRetries` | `int` | `3` | No. | `[Range(0, 100)]` |
| `InitialRetryDelayMs` | `int` | `1000` | No. | `[Range(100, 10000)]` |
| `MaxRetryDelayMs` | `int` | `30000` | No. | `[Range(1000, 60000)]` |
| `Timeout` | `TimeSpan` | 10 seconds | Yes. Per-request timeout. | None |

The retry properties are validated, but `WebhookClient` does not use them. Use `WebhookClientValidation.Validate`, `IsValid` or `EnsureValid` to check the options, including the rule that `MaxRetryDelayMs` is at least `InitialRetryDelayMs`.

---

## Example

```csharp
public sealed class WebhookNotifier
{
    private readonly WebhookClient _client;

    public WebhookNotifier(WebhookClient client) => _client = client;

    public async Task NotifyAsync(string url, IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        var result = await _client.SendEventWebhookAsync(url, domainEvent, cancellationToken);

        if (!result.Success)
        {
            // Log or queue for a later attempt; the client does not retry.
        }
    }
}
```
