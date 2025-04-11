#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Models;

using System.Text.Json.Serialization;

/// <summary>
/// Client registration response per RFC 7591 §3.2.1.
/// </summary>
public sealed class ClientRegistrationResponse
{
    /// <summary>
    /// The registered client_id.
    /// </summary>
    [JsonPropertyName("client_id")]
    public string ClientId { get; set; } = null!;

    /// <summary>
    /// The client_secret for confidential clients.
    /// Absent for public clients.
    /// </summary>
    [JsonPropertyName("client_secret")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Unix timestamp at which client_id was issued.
    /// </summary>
    [JsonPropertyName("client_id_issued_at")]
    public long ClientIdIssuedAt { get; set; }

    /// <summary>
    /// Unix timestamp at which client_secret expires, or 0 for no expiry.
    /// </summary>
    [JsonPropertyName("client_secret_expires_at")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public long? ClientSecretExpiresAt { get; set; }

    /// <summary>
    /// Registered client_name.
    /// </summary>
    [JsonPropertyName("client_name")]
    public string ClientName { get; set; } = null!;

    /// <summary>
    /// Registered grant_types.
    /// </summary>
    [JsonPropertyName("grant_types")]
    public ICollection<string> GrantTypes { get; set; } = [];

    /// <summary>
    /// Registered redirect_uris.
    /// </summary>
    [JsonPropertyName("redirect_uris")]
    public ICollection<string> RedirectUris { get; set; } = [];

    /// <summary>
    /// Registered response_types.
    /// </summary>
    [JsonPropertyName("response_types")]
    public ICollection<string> ResponseTypes { get; set; } = [];

    /// <summary>
    /// Registered scope.
    /// </summary>
    [JsonPropertyName("scope")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Scope { get; set; }

    /// <summary>
    /// Token endpoint authentication method.
    /// </summary>
    [JsonPropertyName("token_endpoint_auth_method")]
    public string TokenEndpointAuthMethod { get; set; } = "client_secret_basic";

    /// <summary>
    /// Registered logo_uri.
    /// </summary>
    [JsonPropertyName("logo_uri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LogoUri { get; set; }

    /// <summary>
    /// Registered policy_uri.
    /// </summary>
    [JsonPropertyName("policy_uri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? PolicyUri { get; set; }

    /// <summary>
    /// Registered tos_uri.
    /// </summary>
    [JsonPropertyName("tos_uri")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? TosUri { get; set; }

    /// <summary>
    /// Registered contacts.
    /// </summary>
    [JsonPropertyName("contacts")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public ICollection<string> Contacts { get; set; } = [];
}
