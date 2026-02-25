#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Client registration request per RFC 7591 §2.
/// </summary>
public sealed class ClientRegistrationRequest
{
    /// <summary>
    /// Human-readable name of the client application.
    /// </summary>
    [JsonPropertyName("client_name")]
    public string? ClientName { get; set; }

    /// <summary>
    /// Grant types the client intends to use.
    /// Defaults to ["authorization_code"] when omitted.
    /// </summary>
    [JsonPropertyName("grant_types")]
    public ICollection<string> GrantTypes { get; set; } = ["authorization_code"];

    /// <summary>
    /// Redirect URIs for authorization_code and implicit flows.
    /// Required when grant_types contains "authorization_code" or "implicit".
    /// </summary>
    [JsonPropertyName("redirect_uris")]
    public ICollection<string> RedirectUris { get; set; } = [];

    /// <summary>
    /// Response types the client will use.
    /// Defaults to ["code"] when omitted.
    /// </summary>
    [JsonPropertyName("response_types")]
    public ICollection<string> ResponseTypes { get; set; } = ["code"];

    /// <summary>
    /// Requested scopes as a space-delimited string.
    /// </summary>
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    /// <summary>
    /// "none" for public clients; "client_secret_post" or "client_secret_basic"
    /// for confidential clients.  Defaults to "client_secret_basic".
    /// </summary>
    [JsonPropertyName("token_endpoint_auth_method")]
    public string TokenEndpointAuthMethod { get; set; } = "client_secret_basic";

    /// <summary>
    /// URI of the client logo.
    /// </summary>
    [JsonPropertyName("logo_uri")]
    public string? LogoUri { get; set; }

    /// <summary>
    /// URI of the client privacy policy.
    /// </summary>
    [JsonPropertyName("policy_uri")]
    public string? PolicyUri { get; set; }

    /// <summary>
    /// URI of the client terms of service.
    /// </summary>
    [JsonPropertyName("tos_uri")]
    public string? TosUri { get; set; }

    /// <summary>
    /// Array of e-mail addresses for the client contacts.
    /// </summary>
    [JsonPropertyName("contacts")]
    public ICollection<string> Contacts { get; set; } = [];

    /// <summary>
    /// URI of the client home page.
    /// </summary>
    [JsonPropertyName("client_uri")]
    public string? ClientUri { get; set; }

    /// <summary>
    /// Returns true when the request carries the minimum required fields.
    /// </summary>
    public bool IsValid()
    {
        if (string.IsNullOrWhiteSpace(ClientName))
            return false;

        // authorization_code and implicit flows require at least one redirect_uri
        var needsRedirect = GrantTypes.Contains("authorization_code", StringComparer.OrdinalIgnoreCase)
                         || GrantTypes.Contains("implicit", StringComparer.OrdinalIgnoreCase);
        if (needsRedirect && RedirectUris.Count == 0)
            return false;

        return true;
    }
}
