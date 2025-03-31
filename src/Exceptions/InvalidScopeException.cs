#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Exceptions;

/// <summary>
/// Thrown when requested scopes are invalid or not available
/// </summary>
public class InvalidScopeException : AuthServerException sealed
{
    public InvalidScopeException(
        string message = "The requested scope is invalid, unknown, or malformed",
        string? errorDescription = null,
        Exception? innerException = null)
        : base(
            "invalid_scope",
            message,
            400,
            errorDescription,
            null,
            innerException)
    {
    }
}
