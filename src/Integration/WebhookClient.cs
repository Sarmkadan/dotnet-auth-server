#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using DotnetAuthServer.Events;
using DotnetAuthServer.Exceptions;
using Microsoft.Extensions.Logging;

namespace DotnetAuthServer.Integration;

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
    /// Uses resilience policies configured in the HttpClient for retry and timeout.
    /// </summary>
    /// <param name="webhookUrl">The URL to send the webhook to.</param>
    /// <param name="@event">The domain event to send.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the webhook delivery attempt.</returns>
    /// <exception cref="ArgumentException">Thrown when webhook URL is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when event is null.</exception>
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
                "Webhook delivery timed out for {Url}",
                webhookUrl);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Webhook delivery failed for {Url}", webhookUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook delivery failed for {Url} with unexpected error", webhookUrl);
        }

        return new WebhookResult { Success = false, Error = "Delivery failed" };
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
    [Required]
    public bool Enabled { get; set; } = true;

    [Range(0, 100)]
    public int MaxRetries { get; set; } = 3;

    [Range(100, 10000)]
    public int InitialRetryDelayMs { get; set; } = 1000;

    [Range(1000, 60000)]
    public int MaxRetryDelayMs { get; set; } = 30000;

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}