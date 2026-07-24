#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Exceptions;

/// <summary>
/// Thrown when the authorization server does not support the requested grant type
/// </summary>
public sealed class UnsupportedGrantTypeException : AuthServerException
{
    public UnsupportedGrantTypeException(
        string message = "The authorization grant type is not supported by the authorization server",
        string? errorDescription = null,
        Exception? innerException = null)
        : base(
            "unsupported_grant_type",
            message,
            400,
            errorDescription,
            null,
            innerException)
    {
    }
}