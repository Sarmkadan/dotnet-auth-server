// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Enums;

/// <summary>
/// OAuth2 and OpenID Connect grant types supported by the authorization server
/// </summary>
public enum GrantType
{
    /// <summary>
    /// Authorization Code Flow - for confidential clients
    /// </summary>
    AuthorizationCode,

    /// <summary>
    /// Client Credentials Flow - for machine-to-machine authentication
    /// </summary>
    ClientCredentials,

    /// <summary>
    /// Resource Owner Password Credentials Flow - for legacy/native apps
    /// </summary>
    ResourceOwnerPasswordCredentials,

    /// <summary>
    /// Refresh Token Grant - to obtain new access tokens
    /// </summary>
    RefreshToken,

    /// <summary>
    /// Implicit Flow - for browser-based clients (legacy)
    /// </summary>
    Implicit,

    /// <summary>
    /// Hybrid Flow - combination of authorization code and implicit
    /// </summary>
    Hybrid
}
