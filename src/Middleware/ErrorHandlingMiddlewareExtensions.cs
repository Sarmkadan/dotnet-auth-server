#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Middleware;

using System;
using System.Text.Json;
using DotnetAuthServer.Exceptions;

/// <summary>
/// Extension methods for <see cref="ErrorHandlingMiddleware"/> that provide additional
/// functionality for error handling and response manipulation.
/// </summary>
public static class ErrorHandlingMiddlewareExtensions
{
    private static readonly System.Reflection.FieldInfo _errorField = typeof(ErrorHandlingMiddleware).GetField(
        "_error", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) ?? throw new InvalidOperationException("Error field not found in ErrorHandlingMiddleware");

    private static readonly System.Reflection.FieldInfo _errorDescriptionField = typeof(ErrorHandlingMiddleware).GetField(
        "_errorDescription", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) ?? throw new InvalidOperationException("ErrorDescription field not found in ErrorHandlingMiddleware");

    private static readonly System.Reflection.FieldInfo _errorUriField = typeof(ErrorHandlingMiddleware).GetField(
        "_errorUri", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance) ?? throw new InvalidOperationException("ErrorUri field not found in ErrorHandlingMiddleware");

    /// <summary>
    /// Creates a standardized error response from the middleware's error properties.
    /// </summary>
    /// <param name="middleware">The error handling middleware instance.</param>
    /// <returns>A new ErrorResponse object populated with the middleware's error data.</returns>
    public static ErrorResponse ToErrorResponse(this ErrorHandlingMiddleware middleware)
    {
        if (middleware is null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        return new ErrorResponse
        {
            Error = _errorField.GetValue(middleware) as string,
            ErrorDescription = _errorDescriptionField.GetValue(middleware) as string,
            ErrorUri = _errorUriField.GetValue(middleware) as string
        };
    }

    /// <summary>
    /// Sets the error properties on the middleware instance from an exception.
    /// </summary>
    /// <param name="middleware">The error handling middleware instance.</param>
    /// <param name="exception">The exception to extract error information from.</param>
    public static void SetErrorFromException(this ErrorHandlingMiddleware middleware, Exception exception)
    {
        if (middleware is null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        if (exception is AuthServerException authException)
        {
            _errorField.SetValue(middleware, authException.ErrorCode);
            _errorDescriptionField.SetValue(middleware, authException.Message);
            _errorUriField.SetValue(middleware, authException.ErrorUri);
        }
        else if (exception is InvalidOperationException)
        {
            _errorField.SetValue(middleware, "invalid_request");
            _errorDescriptionField.SetValue(middleware, exception.Message);
            _errorUriField.SetValue(middleware, null);
        }
        else
        {
            _errorField.SetValue(middleware, "server_error");
            _errorDescriptionField.SetValue(middleware, "An internal server error occurred");
            _errorUriField.SetValue(middleware, null);
        }
    }

    /// <summary>
    /// Serializes the middleware's error response to JSON using snake_case naming convention.
    /// </summary>
    /// <param name="middleware">The error handling middleware instance.</param>
    /// <returns>A JSON string representation of the error response.</returns>
    public static string SerializeErrorToJson(this ErrorHandlingMiddleware middleware)
    {
        if (middleware is null)
        {
            throw new ArgumentNullException(nameof(middleware));
        }

        var error = _errorField.GetValue(middleware) as string;
        var errorDescription = _errorDescriptionField.GetValue(middleware) as string;
        var errorUri = _errorUriField.GetValue(middleware) as string;

        var response = new
        {
            error,
            error_description = errorDescription,
            error_uri = errorUri
        };

        return JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false
        });
    }

    /// <summary>
    /// Checks if the middleware has any error information set.
    /// </summary>
    /// <param name="middleware">The error handling middleware instance.</param>
    /// <returns>True if any error property is set; otherwise false.</returns>
    public static bool HasError(this ErrorHandlingMiddleware middleware)
    {
        if (middleware is null)
        {
            return false;
        }

        var error = _errorField.GetValue(middleware) as string;
        var errorDescription = _errorDescriptionField.GetValue(middleware) as string;

        return !string.IsNullOrEmpty(error) || !string.IsNullOrEmpty(errorDescription);
    }

    /// <summary>
    /// Clears all error information from the middleware instance.
    /// </summary>
    /// <param name="middleware">The error handling middleware instance.</param>
    public static void ClearError(this ErrorHandlingMiddleware middleware)
    {
        if (middleware is not null)
        {
            _errorField.SetValue(middleware, null);
            _errorDescriptionField.SetValue(middleware, null);
            _errorUriField.SetValue(middleware, null);
        }
    }

    /// <summary>
    /// Response object containing error information.
    /// </summary>
    public sealed class ErrorResponse
    {
        public string? Error { get; set; }
        public string? ErrorDescription { get; set; }
        public string? ErrorUri { get; set; }
    }
}