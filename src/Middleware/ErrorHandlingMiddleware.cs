#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Middleware;

using System.Net;
using System.Text.Json;
using DotnetAuthServer.Exceptions;

/// <summary>
/// Middleware for handling exceptions and converting them to appropriate HTTP responses.
/// This ensures consistent error formatting across the entire API and prevents
/// sensitive internal error details from leaking to clients.
/// </summary>
public sealed class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public string Error { get; private set; } = string.Empty;
    public string? ErrorDescription { get; private set; }
    public string? ErrorUri { get; private set; }

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public override string ToString() => $"ErrorHandlingMiddleware {{ Error = {Error}, ErrorDescription = {ErrorDescription}, ErrorUri = {ErrorUri} }}";

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation("Processing request {Method} {Path}", context.Request.Method, context.Request.Path);
        try
        {
            await _next(context);
            _logger.LogInformation("Finished processing request {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception occurred at {Path}", context.Request.Path);
            await HandleExceptionAsync(context, exception);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        if (exception is AuthServerException authException)
        {
            context.Response.StatusCode = authException.StatusCode;

            // RFC 6749 Section 5.2: Error Response
            // The response body MUST be application/json
            // For "invalid_client" error, the server MAY include the "WWW-Authenticate" header
            // to indicate a challenge response (RFC 6749 Section 5.2)
            if (string.Equals(authException.ErrorCode, "invalid_client", StringComparison.Ordinal))
            {
                context.Response.Headers.WWWAuthenticate = $"error, error=\"{authException.ErrorCode}\", error_description=\"{authException.ErrorDescription}\"{(authException.ErrorUri != null ? $", error_uri=\"{authException.ErrorUri}\"" : string.Empty)}";
            }

            var errorResponse = authException.ToErrorResponse();
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
            return context.Response.WriteAsJsonAsync(errorResponse, options);
        }
        else if (exception is InvalidOperationException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            var errorResponse = new Dictionary<string, object>
            {
                { "error", "invalid_request" },
                { "error_description", exception.Message }
            };
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
            return context.Response.WriteAsJsonAsync(errorResponse, options);
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            var errorResponse = new Dictionary<string, object>
            {
                { "error", "server_error" },
                { "error_description", "An internal server error occurred" }
            };
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };
            return context.Response.WriteAsJsonAsync(errorResponse, options);
        }
    }
}