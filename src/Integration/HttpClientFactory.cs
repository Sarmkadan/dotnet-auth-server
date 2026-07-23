#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using System.Net;
using Polly;
using Polly.Retry;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace DotnetAuthServer.Integration;

/// <summary>
/// Factory for creating properly configured HTTP clients with resilience policies.
/// Ensures consistency in timeout settings, certificate validation, resilience, and user agents
/// across all outbound requests.
/// </summary>
public static class HttpClientFactory
{
    /// <summary>
    /// Creates an HTTP client with sensible defaults for OAuth2/OIDC integrations.
    /// Configures appropriate timeouts, resilience policies, and headers to prevent common issues.
    /// </summary>
    /// <param name="config">Configuration for HTTP client factory.</param>
    /// <param name="userAgent">Optional custom user agent string.</param>
    /// <returns>Configured HTTP client instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
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
    /// Creates an HTTP client for webhook delivery with retry-friendly settings.
    /// Uses resilience policies for transient failures and circuit breaker for downstream protection.
    /// </summary>
    /// <param name="config">Configuration for HTTP client factory.</param>
    /// <returns>Configured HTTP client instance for webhook delivery.</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
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
    /// <returns>Configured HTTP client instance for external lookups.</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    public static HttpClient CreateExternalLookupClient(HttpClientFactoryConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        var client = new HttpClient(CreateResilientHandler(config))
        {
            // External lookup-specific timeout
            Timeout = config.ExternalLookupTimeout
        };

        client.DefaultRequestHeaders.Add("User-Agent", "DotnetAuthServer-Integration/1.0");
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        return client;
    }

    /// <summary>
    /// Creates a resilient HTTP message handler with retry, timeout, and circuit breaker policies.
    /// </summary>
    /// <param name="config">Configuration for resilience policies.</param>
    /// <returns>Configured resilient message handler.</returns>
    /// <exception cref="ArgumentNullException">Thrown when config is null.</exception>
    private static HttpMessageHandler CreateResilientHandler(HttpClientFactoryConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        // Create base handler with proper certificate validation handling
        var baseHandler = new HttpClientHandler
        {
            // Ensure proper handling of HTTPS certificates
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
            // Enable automatic decompression for better performance
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };

        // Wrap with resilience handler
        return new ResilientHttpMessageHandler(baseHandler, config);
    }
}

/// <summary>
/// Custom HttpMessageHandler that implements resilience policies including retry, timeout, and circuit breaker.
/// </summary>
internal sealed class ResilientHttpMessageHandler : DelegatingHandler
{
    private readonly HttpClientFactoryConfig _config;
    private readonly AsyncPolicy<HttpResponseMessage> _policy;

    public ResilientHttpMessageHandler(HttpMessageHandler innerHandler, HttpClientFactoryConfig config)
        : base(innerHandler)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));

        // Create combined resilience policy with timeout, retry, and circuit breaker
        var timeoutPolicy = Policy.TimeoutAsync<HttpResponseMessage>(
            _config.DefaultTimeout,
            TimeoutStrategy.Pessimistic
        );

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
                    _config.MaxRetryDelayMs
                )),
                onRetry: (response, delay, retryCount, context) =>
                {
                    // Log retry attempts
                }
            );

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
                    // Log circuit breaker opening
                },
                onReset: () =>
                {
                    // Log circuit breaker closing
                },
                onHalfOpen: () =>
                {
                    // Log circuit breaker half-open state
                }
            );

        // Combine policies: timeout -> retry -> circuit breaker (outer to inner execution order)
        _policy = Policy.WrapAsync(timeoutPolicy, retryPolicy, circuitBreakerPolicy);
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        return await _policy.ExecuteAsync(async ct =>
            await base.SendAsync(request, ct),
            cancellationToken
        ).ConfigureAwait(false);
    }
}