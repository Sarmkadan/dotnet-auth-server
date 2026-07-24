#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace DotnetAuthServer.Exceptions;

/// <summary>
/// Thrown when an invalid grant (authorization code, refresh token, etc.) is provided.
/// </summary>
public sealed class InvalidGrantException : AuthServerException
{
    /// <summary>
    /// Initializes a new instance of <see cref="InvalidGrantException"/>.
    /// </summary>
    /// <param name="message">
    /// Optional custom error message. Defaults to a generic invalid‑grant message.
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
    public InvalidGrantException(
        string message = "The provided grant is invalid, expired, revoked, or does not match the redirect URI",
        string? errorDescription = null,
        Exception? innerException = null)
        : base(
            "invalid_grant",
            message,
            400,
            errorDescription ?? message,
            null,
            innerException)
    {
        ArgumentException.ThrowIfNullOrEmpty(message);
    }
}
