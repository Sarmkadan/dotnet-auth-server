#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

namespace DotnetAuthServer.Integration;

/// <summary>
/// Configuration options for HTTP client factory resilience policies.
/// Controls retry, timeout, and circuit breaker behavior for outbound HTTP calls.
/// </summary>
public sealed class HttpClientFactoryConfig
{
    /// <summary>
    /// Gets or sets the default timeout for HTTP clients.
    /// </summary>
    public TimeSpan DefaultTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the timeout for webhook HTTP clients.
    /// </summary>
    public TimeSpan WebhookTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the user agent string for HTTP requests.
    /// </summary>
    public string UserAgent { get; set; } = "DotnetAuthServer/1.0";

    /// <summary>
    /// Gets or sets the timeout for external lookup HTTP clients.
    /// </summary>
    public TimeSpan ExternalLookupTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the number of retry attempts for transient failures.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the initial delay between retry attempts in milliseconds.
    /// </summary>
    public int InitialRetryDelayMs { get; set; } = 200;

    /// <summary>
    /// Gets or sets the maximum delay between retry attempts in milliseconds.
    /// </summary>
    public int MaxRetryDelayMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the minimum number of failures required before opening the circuit.
    /// </summary>
    public int CircuitBreakerFailureThreshold { get; set; } = 5;

    /// <summary>
    /// Gets or sets the duration of the circuit's closed state after opening.
    /// </summary>
    public TimeSpan CircuitBreakerBreakDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the sampling duration for circuit breaker metrics.
    /// </summary>
    public TimeSpan CircuitBreakerSamplingDuration { get; set; } = TimeSpan.FromSeconds(60);
}