// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Exceptions;

/// <summary>
/// Thrown when an invalid grant (authorization code, refresh token, etc.) is provided
/// </summary>
public class InvalidGrantException : AuthServerException
{
    public InvalidGrantException(
        string message = "The provided grant is invalid, expired, revoked, or does not match the redirect URI",
        string? errorDescription = null,
        Exception? innerException = null)
        : base(
            "invalid_grant",
            message,
            400,
            errorDescription,
            null,
            innerException)
    {
    }
}
