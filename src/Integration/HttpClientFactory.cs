// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Integration;

/// <summary>
/// Factory for creating properly configured HTTP clients for external integrations.
/// Ensures consistency in timeout settings, certificate validation, and user agents
/// across all outbound requests.
/// </summary>
public static class HttpClientFactory
{
    /// <summary>
    /// Creates an HTTP client with sensible defaults for OAuth2/OIDC integrations.
    /// Configures appropriate timeouts and headers to prevent common issues.
    /// </summary>
    public static HttpClient CreateDefaultClient(string? userAgent = null)
    {
        var client = new HttpClient();

        // Timeout settings - OAuth2 operations should complete quickly
        client.Timeout = TimeSpan.FromSeconds(30);

        // Set user agent for webhook and external calls
        var ua = userAgent ?? "DotnetAuthServer/1.0";
        client.DefaultRequestHeaders.Add("User-Agent", ua);

        return client;
    }

    /// <summary>
    /// Creates an HTTP client for webhook delivery with retry-friendly settings.
    /// Uses shorter timeouts and allows connection pooling.
    /// </summary>
    public static HttpClient CreateWebhookClient(WebhookOptions options)
    {
        var client = new HttpClient();
        client.Timeout = options.Timeout;
        client.DefaultRequestHeaders.Add("User-Agent", "DotnetAuthServer-Webhooks/1.0");

        return client;
    }

    /// <summary>
    /// Creates an HTTP client for external user information lookups.
    /// Includes timeout and appropriate headers.
    /// </summary>
    public static HttpClient CreateExternalLookupClient()
    {
        var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(10);
        client.DefaultRequestHeaders.Add("User-Agent", "DotnetAuthServer-Integration/1.0");
        client.DefaultRequestHeaders.Add("Accept", "application/json");

        return client;
    }
}
