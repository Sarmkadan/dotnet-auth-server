// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Handlers;

using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Models;

/// <summary>
/// Handler for OAuth2 token introspection (RFC 7662).
/// Allows authenticated clients to query information about tokens
/// without needing to parse JWTs themselves. Used by resource servers
/// to validate access tokens received from clients.
/// </summary>
public class TokenIntrospectionHandler
{
    private readonly AuthServerOptions _options;
    private readonly ILogger<TokenIntrospectionHandler> _logger;

    public TokenIntrospectionHandler(AuthServerOptions options, ILogger<TokenIntrospectionHandler> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Introspects a token and returns its active status and claims.
    /// Returns minimal information (active=false) for invalid tokens to prevent information disclosure.
    /// </summary>
    public IntrospectionResponse IntrospectToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Introspection request with missing token");
            return new IntrospectionResponse { Active = false };
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.JwtSigningKey));

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateIssuer = true,
                ValidIssuer = _options.IssuerUrl,
                ValidateAudience = false, // Audience varies by client
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(5)
            };

            var principal = handler.ValidateToken(token, validationParameters, out var validatedToken);

            if (validatedToken is JwtSecurityToken jwtToken)
            {
                return new IntrospectionResponse
                {
                    Active = true,
                    Scope = jwtToken.Claims.FirstOrDefault(c => c.Type == "scope")?.Value,
                    ClientId = jwtToken.Claims.FirstOrDefault(c => c.Type == "aud")?.Value,
                    Username = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value,
                    TokenType = "Bearer",
                    Exp = (long?)jwtToken.ValidTo.ToUniversalTime().Subtract(
                        new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds,
                    Iat = (long?)jwtToken.IssuedAt.ToUniversalTime().Subtract(
                        new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds,
                    Sub = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Token introspection failed for malformed or invalid token");
        }

        // Return inactive status for any invalid tokens
        return new IntrospectionResponse { Active = false };
    }
}

/// <summary>
/// Response model for token introspection endpoint (RFC 7662).
/// </summary>
public class IntrospectionResponse
{
    public bool Active { get; set; }
    public string? Scope { get; set; }
    public string? ClientId { get; set; }
    public string? Username { get; set; }
    public string? TokenType { get; set; }
    public long? Exp { get; set; }
    public long? Iat { get; set; }
    public string? Sub { get; set; }
}
