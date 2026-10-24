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
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class with a message.
    /// </summary>
    /// <param name="message">Exception message.</param>
    /// <param name="errorDescription">Optional detailed description.</param>
    /// <param name="innerException">Optional inner exception.</param>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationException"/> class for an invalid configuration property.
    /// </summary>
    /// <param name="propertyName">Name of the invalid configuration property.</param>
    /// <param name="propertyValue">Value that was supplied.</param>
    /// <param name="expectedFormat">Description of the expected format.</param>
    /// <param name="innerException">Optional inner exception.</param>
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