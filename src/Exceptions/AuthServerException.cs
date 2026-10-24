#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Exceptions;

/// <summary>
/// Base exception for authorization server errors
/// </summary>
public class AuthServerException : Exception
{
    /// <summary>
    /// OAuth2 error code
    /// </summary>
    public string ErrorCode { get; set; }

    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Detailed error description
    /// </summary>
    public string? ErrorDescription { get; set; }

    /// <summary>
    /// URI for error documentation
    /// </summary>
    public string? ErrorUri { get; set; }

    /// <summary>
    /// Additional error details
    /// </summary>
    public Dictionary<string, object> Details { get; set; } = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthServerException"/> class.
    /// </summary>
    /// <param name="errorCode">OAuth2 error code.</param>
    /// <param name="message">Exception message.</param>
    /// <param name="statusCode">HTTP status code to return; defaults to 400.</param>
    /// <param name="errorDescription">Detailed description; defaults to <paramref name="message"/>.</param>
    /// <param name="errorUri">Optional URI for error documentation.</param>
    /// <param name="innerException">Optional inner exception.</param>
    public AuthServerException(
        string errorCode,
        string message,
        int statusCode = 400,
        string? errorDescription = null,
        string? errorUri = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
        ErrorDescription = errorDescription ?? message;
        ErrorUri = errorUri;
    }

    /// <summary>
    /// Creates an error response suitable for OAuth2 endpoints
    /// </summary>
    public Dictionary<string, object> ToErrorResponse()
    {
        var response = new Dictionary<string, object>
        {
            { "error", ErrorCode },
            { "error_description", ErrorDescription ?? Message }
        };

        if (ErrorUri is not null)
            response["error_uri"] = ErrorUri;

        foreach (var detail in Details)
            response[detail.Key] = detail.Value;

        return response;
    }

    /// <summary>
    /// Returns a concise, informative representation of the exception.
    /// </summary>
    public override string ToString() =>
        $"AuthServerException {{ ErrorCode = {ErrorCode}, StatusCode = {StatusCode}, ErrorDescription = {ErrorDescription}, ErrorUri = {ErrorUri}, Details = {Details} }}";
}
