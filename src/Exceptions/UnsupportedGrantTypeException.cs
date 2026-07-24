#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace DotnetAuthServer.Exceptions;

/// <summary>
/// Thrown when a request contains an unsupported grant type.
/// Maps to the OAuth2 <c>unsupported_grant_type</c> error response.
/// </summary>
public sealed class UnsupportedGrantTypeException : AuthServerException
{
    /// <summary>
    /// Initializes a new instance of <see cref="UnsupportedGrantTypeException"/>.
    /// </summary>
    /// <param name="message">
    /// Optional custom error message. Defaults to a generic unsupported grant type message.
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
    public UnsupportedGrantTypeException(
        string message = "The requested grant type is not supported",
        string? errorDescription = null,
        Exception? innerException = null)
        : base(
            "unsupported_grant_type",
            message,
            400,
            errorDescription ?? message,
            null,
            innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
    }
}
