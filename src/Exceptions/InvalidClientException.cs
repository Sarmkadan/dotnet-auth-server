#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace DotnetAuthServer.Exceptions;

/// <summary>
/// Thrown when client authentication fails (e.g., invalid client credentials).
/// Maps to the OAuth2 <c>invalid_client</c> error response.
/// </summary>
public sealed class InvalidClientException : AuthServerException
{
    /// <summary>
    /// Initializes a new instance of <see cref="InvalidClientException"/>.
    /// </summary>
    /// <param name="message">
    /// Optional custom error message. Defaults to a generic client authentication failure message.
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
    public InvalidClientException(
        string message = "Client authentication failed",
        string? errorDescription = null,
        Exception? innerException = null)
        : base(
            "invalid_client",
            message,
            401,
            errorDescription ?? message,
            null,
            innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
    }
}
