// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Models;

/// <summary>
/// Represents an OAuth2/OIDC authorization request
/// </summary>
public class AuthorizationRequest
{
    /// <summary>
    /// Client identifier
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Response type (code, token, id_token, code id_token, etc.)
    /// </summary>
    public string? ResponseType { get; set; }

    /// <summary>
    /// Redirect URI where the authorization code/token will be sent
    /// </summary>
    public string? RedirectUri { get; set; }

    /// <summary>
    /// Requested scopes (space-separated)
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// CSRF token for state verification
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Nonce for ID token validation (OIDC)
    /// </summary>
    public string? Nonce { get; set; }

    /// <summary>
    /// PKCE code challenge
    /// </summary>
    public string? CodeChallenge { get; set; }

    /// <summary>
    /// PKCE code challenge method (plain or S256)
    /// </summary>
    public string? CodeChallengeMethod { get; set; }

    /// <summary>
    /// Display parameter (page, popup, touch, wap)
    /// </summary>
    public string? Display { get; set; }

    /// <summary>
    /// Prompt parameter (none, login, consent, select_account)
    /// </summary>
    public string? Prompt { get; set; }

    /// <summary>
    /// Maximum age of authentication (in seconds)
    /// </summary>
    public int? MaxAge { get; set; }

    /// <summary>
    /// Preferred UI locale
    /// </summary>
    public string? UiLocales { get; set; }

    /// <summary>
    /// Preferred language for messages
    /// </summary>
    public string? AcrValues { get; set; }

    /// <summary>
    /// Hint about the user's identity (email, login, etc.)
    /// </summary>
    public string? LoginHint { get; set; }

    /// <summary>
    /// Hint about the user's preferred identity provider
    /// </summary>
    public string? IdTokenHint { get; set; }

    /// <summary>
    /// Additional custom parameters
    /// </summary>
    public Dictionary<string, string> CustomParameters { get; set; } = [];

    /// <summary>
    /// Validates the authorization request has required parameters
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ClientId) &&
               !string.IsNullOrWhiteSpace(ResponseType) &&
               !string.IsNullOrWhiteSpace(RedirectUri) &&
               !string.IsNullOrWhiteSpace(Scope);
    }

    /// <summary>
    /// Gets the requested scopes as a list
    /// </summary>
    public IEnumerable<string> GetRequestedScopes()
    {
        if (string.IsNullOrWhiteSpace(Scope))
            return Enumerable.Empty<string>();

        return Scope.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Checks if PKCE is requested
    /// </summary>
    public bool HasPkce()
    {
        return !string.IsNullOrWhiteSpace(CodeChallenge);
    }

    /// <summary>
    /// Checks if this is an OpenID Connect request (openid scope)
    /// </summary>
    public bool IsOpenIdRequest()
    {
        return GetRequestedScopes().Contains("openid", StringComparer.OrdinalIgnoreCase);
    }
}
