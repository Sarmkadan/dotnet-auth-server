// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Middleware;

/// <summary>
/// Middleware for establishing request context including request IDs for tracing
/// and correlation. Enables end-to-end request tracking through distributed systems
/// and makes debugging production issues significantly easier.
/// </summary>
public class RequestContextMiddleware
{
    private readonly RequestDelegate _next;

    public RequestContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers.ContainsKey("X-Request-ID")
            ? context.Request.Headers["X-Request-ID"].ToString()
            : Guid.NewGuid().ToString("N");

        context.Items["RequestId"] = requestId;
        context.Response.Headers.Add("X-Request-ID", requestId);

        // Include request ID in logs via logging scope
        using (var scope = new LoggingScope(requestId))
        {
            await _next(context);
        }
    }

    private class LoggingScope : IDisposable
    {
        public LoggingScope(string requestId)
        {
            LogicalContext.RequestId = requestId;
        }

        public void Dispose()
        {
            LogicalContext.RequestId = null;
        }
    }
}

/// <summary>
/// Thread-safe storage for request-scoped logical context data.
/// Used throughout the request pipeline to maintain consistency.
/// </summary>
public static class LogicalContext
{
    private static readonly AsyncLocal<string?> RequestIdStorage = new();

    public static string? RequestId
    {
        get => RequestIdStorage.Value;
        set => RequestIdStorage.Value = value;
    }
}
