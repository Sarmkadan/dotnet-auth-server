// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Formatters;

using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetAuthServer.Domain.Models;

/// <summary>
/// Formatter for OAuth2 token responses following RFC 6749.
/// Ensures consistent JSON formatting with proper snake_case field names
/// and compact output suitable for client parsing.
/// </summary>
public class JsonTokenResponseFormatter
{
    private static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseNamingPolicy,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a token response to JSON following OAuth2 specification.
    /// Includes only non-null fields to minimize response size.
    /// </summary>
    public static string FormatTokenResponse(TokenResponse response)
    {
        var dto = new TokenResponseDto
        {
            AccessToken = response.AccessToken,
            TokenType = response.TokenType,
            ExpiresIn = response.ExpiresIn,
            RefreshToken = response.RefreshToken,
            Scope = response.Scope
        };

        return JsonSerializer.Serialize(dto, DefaultOptions);
    }

    /// <summary>
    /// Parses a JSON token response back into a TokenResponse object.
    /// Handles both standard and non-standard fields gracefully.
    /// </summary>
    public static TokenResponse? ParseTokenResponse(string json)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<TokenResponseDto>(json, DefaultOptions);
            if (dto == null)
                return null;

            return new TokenResponse
            {
                AccessToken = dto.AccessToken,
                TokenType = dto.TokenType,
                ExpiresIn = dto.ExpiresIn,
                RefreshToken = dto.RefreshToken,
                Scope = dto.Scope
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// DTO for JSON serialization of token responses.
    /// Uses snake_case property names per OAuth2 specification.
    /// </summary>
    private class TokenResponseDto
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }
}
