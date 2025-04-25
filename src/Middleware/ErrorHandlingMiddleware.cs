// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Middleware;

using System.Net;
using System.Text.Json;
using DotnetAuthServer.Domain.Models;
using DotnetAuthServer.Exceptions;

/// <summary>
/// Middleware for handling exceptions and converting them to appropriate HTTP responses.
/// This ensures consistent error formatting across the entire API and prevents
/// sensitive internal error details from leaking to clients.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
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

        var response = new ErrorResponse();

        if (exception is AuthServerException authException)
        {
            context.Response.StatusCode = authException.StatusCode;
            response.Error = authException.ErrorCode;
            response.ErrorDescription = authException.Message;
            response.ErrorUri = authException.ErrorUri;
        }
        else if (exception is InvalidOperationException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            response.Error = "invalid_request";
            response.ErrorDescription = exception.Message;
        }
        else
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            response.Error = "server_error";
            response.ErrorDescription = "An internal server error occurred";
        }

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseNamingPolicy };
        return context.Response.WriteAsJsonAsync(response, options);
    }

    private class ErrorResponse
    {
        public string? Error { get; set; }
        public string? ErrorDescription { get; set; }
        public string? ErrorUri { get; set; }
    }
}
