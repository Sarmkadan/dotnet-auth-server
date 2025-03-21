// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Exceptions;

/// <summary>
/// Thrown when client authentication fails or client is invalid
/// </summary>
public class InvalidClientException : AuthServerException
{
    public InvalidClientException(
        string message = "Client authentication failed",
        string? errorDescription = null,
        Exception? innerException = null)
        : base(
            "invalid_client",
            message,
            401,
            errorDescription,
            null,
            innerException)
    {
    }
}
