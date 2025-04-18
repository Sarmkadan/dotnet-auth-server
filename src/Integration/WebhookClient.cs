#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

namespace DotnetAuthServer.Integration;

using System.Text.Json;
using DotnetAuthServer.Events;
using DotnetAuthServer.Exceptions;
using Microsoft.Extensions.Logging;

/// <summary>
/// Client for sending webhook notifications about authorization server events.
/// Allows external systems to be notified in real-time about token issuance,
/// user authentication, consent changes, etc.
/// Implements retry logic and exponential backoff for resilience.
/// </summary>
public sealed class WebhookClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<WebhookClient> _logger;
    private readonly WebhookOptions _options;

    public WebhookClient(HttpClient httpClient, ILogger<WebhookClient> logger, WebhookOptions options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <summary>
    /// Sends a webhook notification for a domain event.
    /// Uses exponential backoff on failures up to the configured maximum retries.
    /// </summary>
    public async Task<WebhookResult> SendEventWebhookAsync(
        string webhookUrl,
        IDomainEvent @event,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(webhookUrl))
            throw new ArgumentException("Webhook URL cannot be null or whitespace", nameof(webhookUrl));

        if (@event is null)
            throw new ArgumentNullException(nameof(@event));

        if (!_options.Enabled || string.IsNullOrWhiteSpace(webhookUrl))
            return new WebhookResult { Success = false, Error = "Webhooks disabled or URL missing" };

        var payload = CreateWebhookPayload(@event);
        var retryCount = 0;
        var delayMs = _options.InitialRetryDelayMs;

        while (retryCount <= _options.MaxRetries)
        {
            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(_options.Timeout);

                var content = new StringContent(
                    JsonSerializer.Serialize(payload),
                    System.Text.Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync(webhookUrl, content, cts.Token);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation(
                        "Webhook sent successfully to {Url} for event {EventType}",
                        webhookUrl,
                        @event.EventType);
                    return new WebhookResult { Success = true };
                }

                // Retry on 5xx errors, give up on 4xx
                if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                {
                    _logger.LogWarning(
                        "Webhook received client error {StatusCode} from {Url}",
                        response.StatusCode,
                        webhookUrl);
                    return new WebhookResult
                    {
                        Success = false,
                        Error = $"HTTP {response.StatusCode}"
                    };
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning(
                    "Webhook delivery timed out for {Url} (attempt {Attempt}/{MaxRetries})",
                    webhookUrl,
                    retryCount + 1,
                    _options.MaxRetries);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(
                    ex,
                    "Webhook delivery attempt {Attempt} failed for {Url} with HTTP error",
                    retryCount + 1,
                    webhookUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Webhook delivery attempt {Attempt} failed for {Url} with unexpected error",
                    retryCount + 1,
                    webhookUrl);
            }

            retryCount++;
            if (retryCount <= _options.MaxRetries)
            {
                await Task.Delay(delayMs, cancellationToken);
                delayMs = Math.Min(delayMs * 2, _options.MaxRetryDelayMs);
            }
        }

        _logger.LogError("Webhook delivery failed after {MaxRetries} attempts to {Url}", _options.MaxRetries, webhookUrl);
        return new WebhookResult { Success = false, Error = "Max retries exceeded" };
    }

    private static WebhookPayload CreateWebhookPayload(IDomainEvent @event)
    {
        return new WebhookPayload
        {
            EventId = @event.EventId,
            EventType = @event.EventType,
            OccurredAt = @event.OccurredAt,
            RequestId = @event.RequestId,
            Data = JsonSerializer.SerializeToElement(@event, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            })
        };
    }
}

/// <summary>
/// Result of a webhook delivery attempt.
/// </summary>
public sealed class WebhookResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Webhook payload envelope containing event metadata and data.
/// </summary>
public sealed class WebhookPayload
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public string? RequestId { get; set; }
    public JsonElement Data { get; set; }
}

/// <summary>
/// Configuration for webhook delivery behavior.
/// </summary>
public sealed class WebhookOptions
{
    public bool Enabled { get; set; } = true;
    public int MaxRetries { get; set; } = 3;
    public int InitialRetryDelayMs { get; set; } = 1000;
    public int MaxRetryDelayMs { get; set; } = 30000;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}
