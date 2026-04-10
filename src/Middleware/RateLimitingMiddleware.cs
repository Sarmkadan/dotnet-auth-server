// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Middleware;

using System.Collections.Concurrent;
using System.Net;

/// <summary>
/// Middleware for enforcing rate limits on sensitive OAuth2 endpoints.
/// Uses a token bucket algorithm to allow bursts while preventing abuse.
/// This is critical for preventing brute force attacks on token endpoints and
/// ensuring fair resource allocation among clients.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();
    private readonly int _requestsPerMinute;
    private readonly int _burstSize;

    // Endpoints that require stricter rate limiting (tokens, authorization)
    private readonly HashSet<string> _sensitiveEndpoints = new(StringComparer.OrdinalIgnoreCase)
    {
        "/oauth/token",
        "/oauth/authorize",
        "/oauth/introspect",
        "/oauth/revoke"
    };

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _requestsPerMinute = 60;
        _burstSize = 10;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "";
        var isSensitive = _sensitiveEndpoints.Any(ep => path.StartsWith(ep, StringComparison.OrdinalIgnoreCase));

        if (isSensitive)
        {
            var clientId = ExtractClientIdentifier(context);
            if (!AllowRequest(clientId))
            {
                _logger.LogWarning("Rate limit exceeded for client {ClientId}", clientId);
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.Headers.Add("Retry-After", "60");
                await context.Response.WriteAsJsonAsync(new { error = "rate_limit_exceeded" });
                return;
            }
        }

        await _next(context);
    }

    private string ExtractClientIdentifier(HttpContext context)
    {
        // Try to extract from Authorization header or client_id parameter
        if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return authHeader.ToString().Substring(0, Math.Min(20, authHeader.ToString().Length));
        }

        if (context.Request.Query.TryGetValue("client_id", out var clientIdParam))
        {
            return clientIdParam.ToString();
        }

        // Fallback to IP address
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private bool AllowRequest(string key)
    {
        var bucket = _buckets.GetOrAdd(key, _ => new TokenBucket(_requestsPerMinute, _burstSize));
        return bucket.TryConsumeToken();
    }

    private class TokenBucket
    {
        private double _tokens;
        private DateTime _lastRefillTime;
        private readonly double _refillRate;
        private readonly double _capacity;
        private readonly object _lock = new();

        public TokenBucket(int requestsPerMinute, int burstSize)
        {
            _capacity = burstSize;
            _tokens = _capacity;
            _refillRate = requestsPerMinute / 60.0;
            _lastRefillTime = DateTime.UtcNow;
        }

        public bool TryConsumeToken()
        {
            lock (_lock)
            {
                Refill();
                if (_tokens >= 1.0)
                {
                    _tokens -= 1.0;
                    return true;
                }
                return false;
            }
        }

        private void Refill()
        {
            var now = DateTime.UtcNow;
            var timePassed = (now - _lastRefillTime).TotalSeconds;
            _tokens = Math.Min(_capacity, _tokens + timePassed * _refillRate);
            _lastRefillTime = now;
        }
    }
}
