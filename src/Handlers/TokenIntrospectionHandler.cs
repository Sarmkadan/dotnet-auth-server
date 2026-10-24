#nullable enable
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
using DotnetAuthServer.Security;

/// <summary>
/// Handler for OAuth2 token introspection (RFC 7662).
/// Allows authenticated clients to query information about tokens
/// without needing to parse JWTs themselves. Used by resource servers
/// to validate access tokens received from clients.
/// </summary>
public sealed class TokenIntrospectionHandler
{
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly SymmetricSecurityKey _signingKey;
    private readonly TokenValidationParameters _validationParameters;
    private readonly RevokedTokenStore _revokedTokenStore;
    private readonly ILogger<TokenIntrospectionHandler> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenIntrospectionHandler"/> class.
    /// </summary>
    /// <param name="options">The authentication server options.</param>
    /// <param name="revokedTokenStore">The store used to check for revoked tokens.</param>
    /// <param name="logger">The logger used by the handler.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/>, <paramref name="revokedTokenStore"/>, or <paramref name="logger"/> is <see langword="null"/>.
    /// </exception>
    public TokenIntrospectionHandler(
        AuthServerOptions options,
        RevokedTokenStore revokedTokenStore,
        ILogger<TokenIntrospectionHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(revokedTokenStore);
        ArgumentNullException.ThrowIfNull(logger);

        _tokenHandler = new JwtSecurityTokenHandler();
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.JwtSigningKey));
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _signingKey,
            ValidateIssuer = true,
            ValidIssuer = options.IssuerUrl,
            ValidateAudience = false, // Audience varies by client
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(5)
        };
        _revokedTokenStore = revokedTokenStore;
        _logger = logger;
    }

    /// <summary>
    /// Introspects a token and returns its active status and claims.
    /// Returns minimal information (active=false) for invalid tokens to prevent information disclosure.
    /// </summary>
    public IntrospectionResponse IntrospectToken(string? token)
    {
        _logger.LogDebug("IntrospectToken called with token length: {Length}", token?.Length ?? 0);
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Introspection request with missing token");
            return new IntrospectionResponse { Active = false };
        }

        try
        {
            var principal = _tokenHandler.ValidateToken(token, _validationParameters, out var validatedToken);

            if (validatedToken is JwtSecurityToken jwtToken)
            {
                // Reject tokens that have been explicitly revoked via their jti
                var jti = jwtToken.Id;
                if (!string.IsNullOrWhiteSpace(jti) && _revokedTokenStore.IsRevoked(jti))
                {
                    _logger.LogInformation("Introspection rejected revoked token jti={Jti}", jti);
                    return new IntrospectionResponse { Active = false };
                }

                var response = new IntrospectionResponse
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

                _logger.LogInformation("IntrospectToken finished successfully for token");
                return response;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token introspection failed for malformed or invalid token");
        }

        _logger.LogInformation("IntrospectToken finished with inactive response");
        return new IntrospectionResponse { Active = false };
    }
}

/// <summary>
/// Response model for token introspection endpoint (RFC 7662).
/// </summary>
public sealed class IntrospectionResponse
{
    /// <summary>
    /// Whether the token is active (valid, unexpired, and not revoked).
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// Space-separated list of scopes associated with the token.
    /// </summary>
    public string? Scope { get; set; }

    /// <summary>
    /// Identifier of the client the token was issued to.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Human-readable identifier of the resource owner, if any.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Type of the token, for example <c>Bearer</c>.
    /// </summary>
    public string? TokenType { get; set; }

    /// <summary>
    /// Expiration time of the token, as seconds since the Unix epoch.
    /// </summary>
    public long? Exp { get; set; }

    /// <summary>
    /// Time at which the token was issued, as seconds since the Unix epoch.
    /// </summary>
    public long? Iat { get; set; }

    /// <summary>
    /// Subject identifier of the token.
    /// </summary>
    public string? Sub { get; set; }

    /// <summary>
    /// Returns a string representation of this introspection response.
    /// </summary>
    /// <returns>A string representation of the response.</returns>
    public override string ToString() =>
        $"IntrospectionResponse {{ Active = {Active}, Scope = {Scope}, ClientId = {ClientId}, Username = {Username}, TokenType = {TokenType}, Exp = {Exp}, Iat = {Iat}, Sub = {Sub} }}";
}
