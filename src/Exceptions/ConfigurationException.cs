#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

namespace DotnetAuthServer.Exceptions;

/// <summary>
/// Thrown when there is a configuration error in the authorization server
/// </summary>
public sealed class ConfigurationException : AuthServerException
{
    public ConfigurationException(
        string message = "Server configuration error",
        string? errorDescription = null,
        Exception? innerException = null)
        : base(
            "server_error",
            message,
            500,
            errorDescription,
            null,
            innerException)
    {
    }

    public ConfigurationException(
        string propertyName,
        string propertyValue,
        string expectedFormat,
        Exception? innerException = null)
        : base(
            "server_error",
            $"Invalid configuration for {propertyName}: '{propertyValue}'. Expected format: {expectedFormat}",
            500,
            null,
            null,
            innerException)
    {
    }
}