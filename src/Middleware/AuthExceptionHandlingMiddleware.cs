#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using DotnetAuthServer.Exceptions;

namespace DotnetAuthServer.Middleware;

/// <summary>
/// Middleware that catches <see cref="AuthServerException"/> derivatives and
/// translates them into RFC 6749‑compliant JSON error responses.
/// </summary>
public sealed class AuthExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    /// <summary>
    /// Creates a new instance of <see cref="AuthExceptionHandlingMiddleware"/>.
    /// </summary>
    /// <param name="next">The next request delegate in the pipeline.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="next"/> is <c>null</c>.</exception>
    public AuthExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    /// <summary>
    /// Invokes the middleware for the current <see cref="HttpContext"/>.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is <c>null</c>.</exception>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (AuthServerException ex)
        {
            await HandleExceptionAsync(context, ex).ConfigureAwait(false);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, AuthServerException ex) =>
        ex switch
        {
            InvalidGrantException => WriteErrorResponseAsync(context, 400, "invalid_grant", ex.Message),
            InvalidClientException => WriteErrorResponseAsync(context, 401, "invalid_client", ex.Message, addWwwAuthenticate: true),
            UnsupportedGrantTypeException => WriteErrorResponseAsync(context, 400, "unsupported_grant_type", ex.Message),
            InvalidScopeException => WriteErrorResponseAsync(context, 400, "invalid_scope", ex.Message),
            ValidationException => WriteErrorResponseAsync(context, 400, "invalid_request", ex.Message),
            _ => WriteErrorResponseAsync(context, 500, "server_error", "An internal server error occurred.")
        };

    private static async Task WriteErrorResponseAsync(
        HttpContext context,
        int statusCode,
        string error,
        string? description,
        bool addWwwAuthenticate = false)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        if (addWwwAuthenticate)
        {
            // Per RFC 6749 §5.2, the WWW‑Authenticate header is required for invalid_client.
            context.Response.Headers["WWW-Authenticate"] = "Basic";
        }

        var payload = new
        {
            error,
            error_description = description ?? error
        };

        var json = JsonSerializer.Serialize(payload);
        await context.Response.WriteAsync(json).ConfigureAwait(false);
    }
}
