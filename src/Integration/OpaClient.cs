#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Integration;

using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetAuthServer.Configuration;

/// <summary>
/// HTTP client wrapper for the Open Policy Agent (OPA) REST API.
/// Sends policy queries to OPA and returns allow/deny decisions.
/// </summary>
public sealed class OpaClient sealed
{
    private readonly HttpClient _http;
    private readonly OpaOptions _options;
    private readonly ILogger<OpaClient> _logger;

    public OpaClient(HttpClient http, OpaOptions options, ILogger<OpaClient> logger)
    {
        _http = http;
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Evaluates a named policy against the provided principal by calling OPA.
    /// Returns null when OPA is unreachable and <see cref="OpaOptions.FailClosedOnError"/>
    /// is false, allowing the caller to fall back to the built-in evaluator.
    /// </summary>
    public async Task<bool?> EvaluatePolicyAsync(
        string policyName,
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        // Build the input document from the principal's claims
        var input = new OpaInput
        {
            Input = new OpaInputDocument
            {
                Subject = principal.FindFirst("sub")?.Value ?? "",
                Roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
                Scopes = (principal.FindFirst("scope")?.Value ?? "")
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .ToList(),
                Claims = principal.Claims
                    .GroupBy(c => c.Type)
                    .ToDictionary(g => g.Key, g => g.First().Value)
            }
        };

        var url = $"{_options.BaseUrl.TrimEnd('/')}/v1/data/{_options.PolicyPath}/{policyName}";

        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutSeconds));

            var response = await _http.PostAsJsonAsync(url, input, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "OPA returned {StatusCode} for policy {Policy}",
                    (int)response.StatusCode, policyName);
                return _options.FailClosedOnError ? false : null;
            }

            var result = await response.Content.ReadFromJsonAsync<OpaResult>(
                cancellationToken: cts.Token);

            return result?.Result ?? false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("OPA request timed out for policy {Policy}", policyName);
            return _options.FailClosedOnError ? false : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OPA request failed for policy {Policy}", policyName);
            return _options.FailClosedOnError ? false : null;
        }
    }

    // -------------------------------------------------------------------------
    // JSON shapes for the OPA Data API

    private sealed class OpaInput
    {
        [JsonPropertyName("input")]
        public OpaInputDocument Input { get; set; } = new();
    }

    private sealed class OpaInputDocument
    {
        [JsonPropertyName("subject")]
        public string Subject { get; set; } = "";

        [JsonPropertyName("roles")]
        public List<string> Roles { get; set; } = [];

        [JsonPropertyName("scopes")]
        public List<string> Scopes { get; set; } = [];

        [JsonPropertyName("claims")]
        public Dictionary<string, string> Claims { get; set; } = [];
    }

    private sealed class OpaResult
    {
        [JsonPropertyName("result")]
        public bool Result { get; set; }
    }
}
