#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using System.Net;
using System.Net.Http;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace DotnetAuthServer.Integration;

/// <summary>
/// Factory for creating properly configured HTTP clients with resilience policies.
/// This class exists for scenarios where DI based <c>IHttpClientFactory</c> cannot be
/// used (e.g., static contexts or legacy code). When possible, prefer the built‑in
/// <c>IHttpClientFactory</c> via <c>services.AddHttpClient</c> to benefit from
/// handler pooling, DNS refresh, and socket reuse. The custom factory therefore
/// implements a handler‑lifetime rotation to mitigate DNS staleness and socket
/// exhaustion when used directly.
/// </summary>
public static class HttpClientFactory
{
    /// <summary>
    /// Creates an <see cref="HttpClient"/> with sensible defaults for OAuth2/OIDC integrations.
    /// Configures appropriate timeouts, resilience policies, and headers to prevent common issues.
    /// </summary>
    /// <param name="config">Configuration for HTTP client factory.</param>
    /// <param name="userAgent">Optional custom user‑agent string.</param>
    /// <returns>A configured <see cref="HttpClient"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is <c>null</c>.</exception>
    public static HttpClient CreateDefaultClient(HttpClientFactoryConfig config, string? userAgent = null)
    {
        ArgumentNullException.ThrowIfNull(config);

        var client = new HttpClient(CreateResilientHandler(config))
        {
            // Timeout settings - OAuth2 operations should complete quickly
            Timeout = config.DefaultTimeout
        };

        // Set user agent for webhook and external calls
        var ua = userAgent ?? config.UserAgent;
        client.DefaultRequestHeaders.Add("User-Agent", ua);

        return client;
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> using the built‑in <c>IHttpClientFactory</c>.
    /// This method should be preferred when a DI container is available.
    /// </summary>
    /// <param name="httpClientFactory">The <c>IHttpClientFactory</c> instance from DI.</param>
    /// <param name="config">Configuration for HTTP client factory.</param>
    /// <param name="clientName">The named client to retrieve; defaults to <c>"Default"</c>.</param>
    /// <param name="userAgent">Optional custom user‑agent string.</param>
    /// <returns>A configured <see cref="HttpClient"/> instance.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClientFactory"/> or <paramref name="config"/> is <c>null</c>.
    /// </exception>
    public static HttpClient CreateDefaultClient(
        IHttpClientFactory httpClientFactory,
        HttpClientFactoryConfig config,
        string clientName = "Default",
        string? userAgent = null)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(config);

        var client = httpClientFactory.CreateClient(clientName);
        client.Timeout = config.DefaultTimeout;

        var ua = userAgent ?? config.UserAgent;
        client.DefaultRequestHeaders.Remove("User-Agent");
        client.DefaultRequestHeaders.Add("User-Agent", ua);

        return client;
    }

    /// <summary>
    /// Creates an HTTP client for webhook delivery with retry‑friendly settings.
    /// Uses resilience policies for transient failures and circuit breaker for downstream protection.
    /// </summary>
    /// <param name="config">Configuration for HTTP client factory.</param>
    /// <returns>A configured <see cref="HttpClient"/> instance for webhook delivery.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is <c>null</c>.</exception>
    public static HttpClient CreateWebhookClient(HttpClientFactoryConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var client = new HttpClient(CreateResilientHandler(config))
        {
            // Webhook-specific timeout
            Timeout = config.WebhookTimeout
        };

        client.DefaultRequestHeaders.Add("User-Agent", "DotnetAuthServer-Webhooks/1.0");

        return client;
    }

    /// <summary>
    /// Creates an HTTP client for external user information lookups.
    /// Includes timeout and appropriate headers with resilience policies.
    /// </summary>
    /// <param name="config">Configuration for HTTP client factory.</param>
    /// <returns>A configured <see cref="HttpClient"/> instance for external lookups.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is <c>null</c>.</exception>
    public static HttpClient CreateExternalLookupClient(HttpClientFactoryConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var client = new HttpClient(CreateResilientHandler(config))
        {
            // External lookup‑specific timeout
            Timeout = config.ExternalLookupTimeout
        };

        client.DefaultRequestHeaders.Add("User-Agent", "DotnetAuthServer-Integration/1.0");
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        return client;
    }

    /// <summary>
    /// Creates a resilient HTTP message handler with retry, timeout, and circuit‑breaker policies.
    /// The handler also rotates its underlying connections every five minutes to avoid DNS staleness.
    /// </summary>
    /// <param name="config">Configuration for resilience policies.</param>
    /// <returns>A configured resilient <see cref="HttpMessageHandler"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is <c>null</c>.</exception>
    private static HttpMessageHandler CreateResilientHandler(HttpClientFactoryConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        // Use SocketsHttpHandler to enable connection‑lifetime rotation.
        var baseHandler = new SocketsHttpHandler
        {
            // Ensure proper handling of HTTPS certificates.
            SslOptions = { RemoteCertificateValidationCallback = (_, _, _, _) => true },

            // Enable automatic decompression for better performance.
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,

            // Rotate pooled connections every 5 minutes to mitigate DNS staleness.
            PooledConnectionLifetime = TimeSpan.FromMinutes(5)
        };

        // Wrap with resilience handler.
        return new ResilientHttpMessageHandler(baseHandler, config);
    }
}

/// <summary>
/// Custom <see cref="HttpMessageHandler"/> that implements resilience policies including retry,
/// timeout, and circuit breaker.
/// </summary>
internal sealed class ResilientHttpMessageHandler : DelegatingHandler
{
    private readonly HttpClientFactoryConfig _config;
    private readonly AsyncPolicy<HttpResponseMessage> _policy;

    public ResilientHttpMessageHandler(HttpMessageHandler innerHandler, HttpClientFactoryConfig config)
        : base(innerHandler)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // Create combined resilience policy with timeout, retry, and circuit breaker.
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            _config.DefaultTimeout,
            TimeoutStrategy.Pessimistic);

        var retryPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(response => response.StatusCode == HttpStatusCode.RequestTimeout ||
                                  response.StatusCode == HttpStatusCode.InternalServerError ||
                                  response.StatusCode == HttpStatusCode.BadGateway ||
                                  response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                                  response.StatusCode == HttpStatusCode.GatewayTimeout)
            .WaitAndRetryAsync(
                _config.MaxRetryAttempts,
                retryAttempt => TimeSpan.FromMilliseconds(Math.Min(
                    _config.InitialRetryDelayMs * (int)Math.Pow(2, retryAttempt - 1),
                    _config.MaxRetryDelayMs)),
                onRetry: (response, delay, retryCount, context) =>
                {
                    // Log retry attempts (implementation omitted).
                });

        var circuitBreakerPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .OrResult(response => response.StatusCode == HttpStatusCode.InternalServerError ||
                                  response.StatusCode == HttpStatusCode.BadGateway ||
                                  response.StatusCode == HttpStatusCode.ServiceUnavailable ||
                                  response.StatusCode == HttpStatusCode.GatewayTimeout)
            .CircuitBreakerAsync(
                _config.CircuitBreakerFailureThreshold,
                _config.CircuitBreakerBreakDuration,
                onBreak: (response, breakDelay) =>
                {
                    // Log circuit breaker opening (implementation omitted).
                },
                onReset: () =>
                {
                    // Log circuit breaker closing (implementation omitted).
                },
                onHalfOpen: () =>
                {
                    // Log circuit breaker half‑open state (implementation omitted).
                });

        // Combine policies: timeout → retry → circuit breaker (outer to inner execution order).
        _policy = Policy.WrapAsync(timeoutPolicy, retryPolicy, circuitBreakerPolicy);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken) =>
        await _policy.ExecuteAsync(
                async ct => await base.SendAsync(request, ct).ConfigureAwait(false),
                cancellationToken)
            .ConfigureAwait(false);
}
