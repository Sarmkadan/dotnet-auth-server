# HttpClientFactoryConfig

The `HttpClientFactoryConfig` class defines the settings that control the timeouts, user agent, retry policy, and circuit breaker applied to outbound HTTP calls created by the static [`HttpClientFactory`](HttpClientFactory.md) helper. It is a sealed, mutable options class with no constructor parameters; every property has a default value, so `new HttpClientFactoryConfig()` yields a usable configuration.

Each factory method reads a different subset of these properties:

| Factory method | Timeout property | User agent | Resilience policy |
|---|---|---|---|
| `CreateDefaultClient(config, userAgent)` | `DefaultTimeout` | `UserAgent` (or the `userAgent` argument) | Yes |
| `CreateWebhookClient(config)` | `WebhookTimeout` | Fixed: `DotnetAuthServer-Webhooks/1.0` | Yes |
| `CreateExternalLookupClient(config)` | `ExternalLookupTimeout` | Fixed: `DotnetAuthServer-Integration/1.0` | Yes |

## API

### DefaultTimeout
Gets or sets the default timeout for HTTP clients, as a `TimeSpan`. The default is 30 seconds. It sets `HttpClient.Timeout` for clients created by `CreateDefaultClient`, and it is also the duration of the Polly timeout policy that wraps every resilient handler, including the handlers used by the webhook and external lookup clients.

### WebhookTimeout
Gets or sets the timeout for clients created by `CreateWebhookClient`. The default is 30 seconds. Because the Polly timeout policy always uses `DefaultTimeout`, the effective limit for a webhook request is the shorter of `WebhookTimeout` and `DefaultTimeout`.

### UserAgent
Gets or sets the `User-Agent` header value sent by clients created with `CreateDefaultClient`. The default is `DotnetAuthServer/1.0`. A non-null `userAgent` argument passed to `CreateDefaultClient` takes precedence over this value. The webhook and external lookup factory methods do not read this property.

### ExternalLookupTimeout
Gets or sets the timeout for clients created by `CreateExternalLookupClient`. The default is 10 seconds. As with `WebhookTimeout`, the effective limit is the shorter of this value and `DefaultTimeout`.

### MaxRetryAttempts
Gets or sets the number of retries performed after the initial attempt for transient failures. The default is 3, which means up to four total attempts per request. Retries apply to `HttpRequestException` and to responses with status 408, 500, 502, 503, or 504. Retries are not restricted by HTTP method, so non-idempotent requests such as `POST` are retried as well.

### InitialRetryDelayMs
Gets or sets the delay before the first retry, in milliseconds. The default is 200. Each subsequent retry doubles the previous delay (200 ms, 400 ms, 800 ms, and so on), capped at `MaxRetryDelayMs`.

### MaxRetryDelayMs
Gets or sets the upper bound for the delay between retries, in milliseconds. The default is 5000.

### CircuitBreakerFailureThreshold
Gets or sets the number of consecutive failures that opens the circuit. The default is 5. Failures are counted with the same conditions the retry policy uses (`HttpRequestException` or the 500, 502, 503, and 504 status codes; 408 is not counted). Each retry attempt is counted separately, because the circuit breaker sits inside the retry policy.

### CircuitBreakerBreakDuration
Gets or sets how long the circuit stays open before a trial request is allowed through, as a `TimeSpan`. The default is 30 seconds. While the circuit is open, requests fail immediately without reaching the network.

### CircuitBreakerSamplingDuration
Gets or sets the sampling duration for circuit breaker metrics. The default is 60 seconds. This value is currently not applied: the circuit breaker is configured with the consecutive-failure threshold and does not read this property. Changing it has no effect on runtime behavior.

## Usage

The following example creates a configuration for a service that calls a slow external identity provider. It lengthens the lookup timeout, reduces retries, and makes the circuit open sooner.

```csharp
var config = new HttpClientFactoryConfig
{
    DefaultTimeout = TimeSpan.FromSeconds(20),
    ExternalLookupTimeout = TimeSpan.FromSeconds(15),
    UserAgent = "DotnetAuthServer/2.0 (+https://example.com/bot)",
    MaxRetryAttempts = 2,
    InitialRetryDelayMs = 250,
    MaxRetryDelayMs = 2000,
    CircuitBreakerFailureThreshold = 3,
    CircuitBreakerBreakDuration = TimeSpan.FromSeconds(60)
};

using var lookupClient = HttpClientFactory.CreateExternalLookupClient(config);
```

When a DI container is available, prefer `IHttpClientFactory` and pass it to the overload of `CreateDefaultClient` that accepts it. In that case the `Timeout` and `User-Agent` settings are applied to the client returned by the factory, but the retry and circuit breaker policies are not added.

```csharp
using var client = HttpClientFactory.CreateDefaultClient(
    httpClientFactory,
    config,
    clientName: "Default",
    userAgent: "DotnetAuthServer-Jwks/1.0");
```

## Remarks

- Each call to a `Create*` method builds a new `HttpMessageHandler`, so the circuit breaker state is not shared between clients. Dispose each client when it is no longer needed.
- Policy values are read when the handler is constructed. Changing a property on the configuration object afterward does not affect clients that were already created.
- The resilience handler sets `SocketsHttpHandler.PooledConnectionLifetime` to 5 minutes and disables TLS certificate validation (`RemoteCertificateValidationCallback` always returns `true`). Callers that need certificate validation must not rely on these clients.
- Because the timeout policy wraps the retries, `DefaultTimeout` limits the total time spent on a request, including all retry attempts.
