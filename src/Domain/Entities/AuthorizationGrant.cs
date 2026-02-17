#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Entities;

/// <summary>
/// Represents an authorization grant (authorization code) in OAuth2
/// </summary>
public sealed class AuthorizationGrant sealed
{
    /// <summary>
    /// Unique grant identifier
    /// </summary>
    public string GrantId { get; set; } = null!;

    /// <summary>
    /// Authorization code (short-lived, sent to client)
    /// </summary>
    public string Code { get; set; } = null!;

    /// <summary>
    /// Client ID requesting the authorization
    /// </summary>
    public string ClientId { get; set; } = null!;

    /// <summary>
    /// User ID that granted the authorization
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Requested scopes
    /// </summary>
    public string RequestedScopes { get; set; } = null!;

    /// <summary>
    /// Granted scopes (may be subset of requested)
    /// </summary>
    public string GrantedScopes { get; set; } = null!;

    /// <summary>
    /// Redirect URI used in authorization request
    /// </summary>
    public string RedirectUri { get; set; } = null!;

    /// <summary>
    /// State parameter for CSRF protection
    /// </summary>
    public string? State { get; set; }

    /// <summary>
    /// Nonce parameter for replay protection
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
    /// Response type (code, id_token, token, etc.)
    /// </summary>
    public string ResponseType { get; set; } = "code";

    /// <summary>
    /// Authorization code expiration timestamp
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether this grant has been used to obtain tokens
    /// </summary>
    public bool IsUsed { get; set; }

    /// <summary>
    /// Timestamp when the grant was used
    /// </summary>
    public DateTime? UsedAt { get; set; }

    /// <summary>
    /// Whether this grant is revoked
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Grant creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Checks if the authorization code is still valid
    /// </summary>
    public bool IsValid()
    {
        return !IsUsed && !IsRevoked && DateTime.UtcNow < ExpiresAt;
    }

    /// <summary>
    /// Checks if the code has expired
    /// </summary>
    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresAt;
    }

    /// <summary>
    /// Marks the grant as used
    /// </summary>
    public void MarkAsUsed()
    {
        if (IsUsed)
            throw new InvalidOperationException("Authorization grant has already been used");

        IsUsed = true;
        UsedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Revokes the authorization grant
    /// </summary>
    public void Revoke()
    {
        IsRevoked = true;
    }

    /// <summary>
    /// Validates PKCE code verifier against the challenge.
    /// Returns false (never throws) for any malformed or disallowed input,
    /// which lets the caller surface a proper OAuth2 error response.
    /// </summary>
    public bool ValidatePkceCodeVerifier(string? codeVerifier)
    {
        if (string.IsNullOrWhiteSpace(CodeChallenge))
            return true; // PKCE not required for this grant

        if (string.IsNullOrWhiteSpace(codeVerifier))
            return false; // PKCE was required but not provided

        // RFC 7636 §4.1: code_verifier = 43*128unreserved
        // unreserved = ALPHA / DIGIT / "-" / "." / "_" / "~"
        // Reject anything outside this set (e.g. '+', '=', '/') with false,
        // not an exception, so the caller can return invalid_grant.
        if (codeVerifier.Length < 43 || codeVerifier.Length > 128)
            return false;

        foreach (var c in codeVerifier)
        {
            if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
                  (c >= '0' && c <= '9') || c == '-' || c == '.' || c == '_' || c == '~'))
                return false;
        }

        try
        {
            if (CodeChallengeMethod == "plain")
                return codeVerifier == CodeChallenge;

            if (CodeChallengeMethod == "S256")
            {
                // S256: code_challenge = BASE64URL(SHA256(ASCII(code_verifier)))
                var sha256 = System.Security.Cryptography.SHA256.HashData(
                    System.Text.Encoding.ASCII.GetBytes(codeVerifier));
                var challenge = Convert.ToBase64String(sha256)
                    .TrimEnd('=')
                    .Replace('+', '-')
                    .Replace('/', '_');
                return string.Equals(challenge, CodeChallenge, StringComparison.Ordinal);
            }
        }
        catch
        {
            // Guard against any unexpected encoding or format exceptions;
            // always return false so the caller surfaces invalid_grant.
            return false;
        }

        return false;
    }
}
