// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Middleware;

using System.Diagnostics;

/// <summary>
/// Middleware for logging HTTP requests and responses with timing information.
/// Provides observability into API usage patterns, latencies, and potential issues.
/// Excludes sensitive endpoints (like /swagger) to reduce noise and security concerns.
/// </summary>
public class LoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<LoggingMiddleware> _logger;
    private readonly HashSet<string> _excludedPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/swagger",
        "/health",
        "/.well-known/jwks.json"
    };

    public LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var shouldLog = !_excludedPaths.Any(path => context.Request.Path.StartsWithSegments(path));

        if (shouldLog)
        {
            var stopwatch = Stopwatch.StartNew();
            var originalBodyStream = context.Response.Body;

            try
            {
                using (var memoryStream = new MemoryStream())
                {
                    context.Response.Body = memoryStream;

                    await _next(context);

                    stopwatch.Stop();

                    var statusCode = context.Response.StatusCode;
                    var logLevel = statusCode >= 500 ? LogLevel.Error : LogLevel.Information;

                    _logger.Log(
                        logLevel,
                        "HTTP {Method} {Path} completed with {StatusCode} in {Elapsed}ms",
                        context.Request.Method,
                        context.Request.Path,
                        statusCode,
                        stopwatch.ElapsedMilliseconds);

                    await memoryStream.CopyToAsync(originalBodyStream);
                }
            }
            finally
            {
                context.Response.Body = originalBodyStream;
            }
        }
        else
        {
            await _next(context);
        }
    }
}
