# WebhookClient

The `WebhookClient` class provides mechanisms for transmitting event-driven notifications to external endpoints within the `dotnet-auth-server` system. It manages the asynchronous execution of webhook requests, including automatic retry logic, configurable delay intervals, and strict timeout constraints to ensure reliable delivery of event-related payloads.

## API

### Constructors

*   `public WebhookClient()`
    Initializes a new instance of the `WebhookClient` class with default configuration.

### Methods

*   `public async Task<WebhookResult> SendEventWebhookAsync(...)`
    Asynchronously sends a webhook event. Returns a `WebhookResult` representing the outcome of the operation.

### Properties

*   `public bool Success`
    Indicates whether the last webhook operation was successful.
*   `public string? Error`
    Contains error details if the last operation failed; otherwise, null.
*   `public string EventId`
    The unique identifier for the event.
*   `public string EventType`
    The type of the event being transmitted.
*   `public DateTime OccurredAt`
    The timestamp of when the event occurred.
*   `public string? RequestId`
    The unique identifier for the HTTP request, if available.
*   `public JsonElement Data`
    The structured data associated with the event.
*   `public bool Enabled`
    Indicates whether webhook functionality is currently enabled.
*   `public int MaxRetries`
    The maximum number of retry attempts for failed requests.
*   `public int InitialRetryDelayMs`
    The initial delay in milliseconds before the first retry.
*   `public int MaxRetryDelayMs`
    The maximum delay in milliseconds allowed between retry attempts.
*   `public TimeSpan Timeout`
    The timeout duration for the HTTP request.

## Usage

### Example 1: Basic Event Transmission
```csharp
var client = new WebhookClient();
// Configure client settings
client.MaxRetries = 3;
client.Timeout = TimeSpan.FromSeconds(5);

// Send the event
var result = await client.SendEventWebhookAsync(payload);

if (result.Success)
{
    Console.WriteLine($"Event {result.EventId} sent successfully.");
}
```

### Example 2: Handling Transmission Failures
```csharp
var client = new WebhookClient();
var result = await client.SendEventWebhookAsync(payload);

if (!result.Success)
{
    // Access error details from the result
    Console.Error.WriteLine($"Failed to send event {result.EventId}: {result.Error}");
}
```

## Notes

*   **Thread Safety:** The `WebhookClient` is not inherently thread-safe. Instances should not be shared across concurrent operations where configuration properties (e.g., `MaxRetries`, `Timeout`) might be modified.
*   **Asynchronous Execution:** The `SendEventWebhookAsync` method must be awaited to ensure the transmission and retry logic complete before proceeding.
*   **Retry Behavior:** Failed requests will be retried based on the `MaxRetries`, `InitialRetryDelayMs`, and `MaxRetryDelayMs` configuration. Ensure these values are tuned appropriately to prevent excessive load on downstream services.
*   **Timeout Handling:** If the `Timeout` threshold is reached before a response is received, the operation will fail and return a `WebhookResult` indicating the error.
