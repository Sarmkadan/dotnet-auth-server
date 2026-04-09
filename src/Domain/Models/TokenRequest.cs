// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Models;

/// <summary>
/// Represents an OAuth2 token request
/// </summary>
public class TokenRequest
{
    /// <summary>
    /// Grant type (authorization_code, refresh_token, client_credentials, etc.)
    /// </summary>
    public string? GrantType { get; set; }

    /// <summary>
    /// Client ID
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Client secret (for confidential clients)
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Authorization code (for authorization_code grant)
    /// </summary>
    public string? Code { get; set; }

    /// <summary>
    /// Redirect URI (must match the one used in authorization request)
    /// </summary>
    public string? RedirectUri { get; set; }

    /// <summary>
    /// Refresh token (for refresh_token grant)
    /// </summary>
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Username (for resource owner password credentials grant)
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Password (for resource owner password credentials grant)
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Scope (space-separated list)
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// PKCE code verifier
    /// </summary>
    public string? CodeVerifier { get; set; }

    /// <summary>
    /// Requested token type (e.g., urn:ietf:params:oauth:token-type:jwt)
    /// </summary>
    public string? RequestedTokenType { get; set; }

    /// <summary>
    /// Subject token (for token exchange)
    /// </summary>
    public string? SubjectToken { get; set; }

    /// <summary>
    /// Subject token type (for token exchange)
    /// </summary>
    public string? SubjectTokenType { get; set; }

    /// <summary>
    /// Actor token (for token exchange)
    /// </summary>
    public string? ActorToken { get; set; }

    /// <summary>
    /// Actor token type (for token exchange)
    /// </summary>
    public string? ActorTokenType { get; set; }

    /// <summary>
    /// Additional parameters from the request
    /// </summary>
    public Dictionary<string, string> CustomParameters { get; set; } = [];

    /// <summary>
    /// Validates the token request has required parameters
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(GrantType) &&
               !string.IsNullOrWhiteSpace(ClientId);
    }

    /// <summary>
    /// Validates the request for a specific grant type
    /// </summary>
    public bool IsValidForGrantType(string grantType)
    {
        if (!IsValid()) return false;

        return grantType switch
        {
            "authorization_code" => !string.IsNullOrWhiteSpace(Code) &&
                                   !string.IsNullOrWhiteSpace(RedirectUri),
            "refresh_token" => !string.IsNullOrWhiteSpace(RefreshToken),
            "client_credentials" => true,
            "password" => !string.IsNullOrWhiteSpace(Username) &&
                         !string.IsNullOrWhiteSpace(Password),
            "urn:ietf:params:oauth:grant-type:token-exchange" =>
                !string.IsNullOrWhiteSpace(SubjectToken) &&
                !string.IsNullOrWhiteSpace(SubjectTokenType),
            _ => false
        };
    }
}
