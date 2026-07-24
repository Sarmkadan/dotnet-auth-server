#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace DotnetAuthServer.Exceptions;

/// <summary>
/// Thrown when a request contains an invalid or unknown scope.
/// Maps to the OAuth2 <c>invalid_scope</c> error response.
/// </summary>
public sealed class InvalidScopeException : AuthServerException
{
    /// <summary>
    /// Initializes a new instance of <see cref="InvalidScopeException"/>.
    /// </summary>
    /// <param name="message">
    /// Optional custom error message. Defaults to a generic invalid scope message.
    /// </param>
    /// <param name="errorDescription">
    /// Optional detailed error description. If <c>null</c>, <paramref name="message"/> is used.
    /// </param>
    /// <param name="innerException">Optional inner exception.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="message"/> is <c>null</c>.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="message"/> is an empty string.
    /// </exception>
    public InvalidScopeException(
        string message = "The requested scope is invalid, unknown, or malformed",
        string? errorDescription = null,
        Exception? innerException = null)
        : base(
            "invalid_scope",
            message,
            400,
            errorDescription ?? message,
            null,
            innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
    }
}
