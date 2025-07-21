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
    private const string ErrorFieldName = "_error";
    private const string ErrorDescriptionFieldName = "_errorDescription";
    private const string ErrorUriFieldName = "_errorUri";

    private static readonly System.Reflection.FieldInfo _errorField = GetErrorField();
    private static readonly System.Reflection.FieldInfo _errorDescriptionField = GetErrorDescriptionField();
    private static readonly System.Reflection.FieldInfo _errorUriField = GetErrorUriField();

    private static System.Reflection.FieldInfo GetErrorField()
    {
        var field = typeof(ErrorHandlingMiddleware).GetField(
            ErrorFieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field ?? throw new InvalidOperationException("Error field not found in ErrorHandlingMiddleware");
    }

    private static System.Reflection.FieldInfo GetErrorDescriptionField()
    {
        var field = typeof(ErrorHandlingMiddleware).GetField(
            ErrorDescriptionFieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field ?? throw new InvalidOperationException("ErrorDescription field not found in ErrorHandlingMiddleware");
    }

    private static System.Reflection.FieldInfo GetErrorUriField()
    {
        var field = typeof(ErrorHandlingMiddleware).GetField(
            ErrorUriFieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return field ?? throw new InvalidOperationException("ErrorUri field not found in ErrorHandlingMiddleware");
    }

    /// <summary>
    /// Creates a standardized error response from the middleware's error properties.
    /// </summary>
    /// <param name="middleware">The error handling middleware instance.</param>
    /// <returns>A new ErrorResponse object populated with the middleware's error data.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="middleware"/> is <see langword="null"/>.</exception>
    public static ErrorResponse ToErrorResponse(this ErrorHandlingMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);

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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="middleware"/> or <paramref name="exception"/> is <see langword="null"/>.</exception>
    public static void SetErrorFromException(this ErrorHandlingMiddleware middleware, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        ArgumentNullException.ThrowIfNull(exception);

        switch (exception)
        {
            case AuthServerException authException:
                _errorField.SetValue(middleware, authException.ErrorCode);
                _errorDescriptionField.SetValue(middleware, authException.Message);
                _errorUriField.SetValue(middleware, authException.ErrorUri);
                break;

            case InvalidOperationException:
                _errorField.SetValue(middleware, "invalid_request");
                _errorDescriptionField.SetValue(middleware, exception.Message);
                _errorUriField.SetValue(middleware, null);
                break;

            default:
                _errorField.SetValue(middleware, "server_error");
                _errorDescriptionField.SetValue(middleware, "An internal server error occurred");
                _errorUriField.SetValue(middleware, null);
                break;
        }
    }

    /// <summary>
    /// Serializes the middleware's error response to JSON using snake_case naming convention.
    /// </summary>
    /// <param name="middleware">The error handling middleware instance.</param>
    /// <returns>A JSON string representation of the error response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="middleware"/> is <see langword="null"/>.</exception>
    public static string SerializeErrorToJson(this ErrorHandlingMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);

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
        /// <summary>Gets or sets the error code.</summary>
        public string? Error { get; set; }

        /// <summary>Gets or sets the human-readable error description.</summary>
        public string? ErrorDescription { get; set; }

        /// <summary>Gets or sets a URI that provides additional information about the error.</summary>
        public string? ErrorUri { get; set; }
    }
}