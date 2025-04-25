// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Exceptions;

/// <summary>
/// Thrown when the client is not authorized to use a requested grant type or other operation
/// </summary>
public class UnauthorizedClientException : AuthServerException
{
    public UnauthorizedClientException(
        string message = "The client is not authorized to use this grant type",
        string? errorDescription = null,
        Exception? innerException = null)
        : base(
            "unauthorized_client",
            message,
            403,
            errorDescription,
            null,
            innerException)
    {
    }
}
