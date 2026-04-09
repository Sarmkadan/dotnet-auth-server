// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Enums;

/// <summary>
/// Token type enumeration for different token purposes
/// </summary>
public enum TokenType
{
    /// <summary>
    /// Bearer token for API access
    /// </summary>
    Bearer,

    /// <summary>
    /// MAC token (Message Authentication Code)
    /// </summary>
    Mac,

    /// <summary>
    /// SAML assertion token
    /// </summary>
    SAML
}
