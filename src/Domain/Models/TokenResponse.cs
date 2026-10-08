#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Models;

using System.Diagnostics;
using System.Text.Json.Serialization;
using DotnetAuthServer.Diagnostics;

/// <summary>
/// Represents an OAuth2 token response
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class TokenResponse
{
    /// <summary>
    /// The access token
    /// </summary>
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = null!;

    /// <summary>
    /// Token type (usually "Bearer")
    /// </summary>
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// Access token lifetime in seconds
    /// </summary>
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Refresh token (optional, for offline access)
    /// </summary>
    [JsonPropertyName("refresh_token")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Granted scopes
    /// </summary>
    [JsonPropertyName("scope")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Scope { get; set; }

    /// <summary>
    /// ID token for OpenID Connect flows
    /// </summary>
    [JsonPropertyName("id_token")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? IdToken { get; set; }

    /// <summary>
    /// Additional custom claims/properties
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object> CustomProperties { get; set; } = [];

    /// <summary>
    /// Returns a diagnostic representation: the access token's jti and expiry (read without validation),
    /// granted scopes, and truncated prefixes of the token values. Full token strings are never included.
    /// </summary>
    public override string ToString()
    {
        var accessToken = JwtDiagnostics.TryRead(AccessToken);
        var jti = accessToken?.Id ?? "n/a";
        var expiresAt = accessToken is { ValidTo: var validTo } && validTo != DateTime.MinValue
            ? validTo.ToString("O")
            : "n/a";

        return $"TokenResponse {{ Jti = {jti}, ExpiresAt = {expiresAt}, ExpiresIn = {ExpiresIn}s, TokenType = {TokenType}, Scope = {Scope ?? "none"}, AccessToken = {Redaction.Secret(AccessToken)}, RefreshToken = {Redaction.Secret(RefreshToken)}, IdToken = {Redaction.Secret(IdToken)} }}";
    }
}
