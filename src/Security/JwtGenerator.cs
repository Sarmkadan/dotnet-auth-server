#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Security;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Generates signed JWT access tokens and ID tokens for authenticated subjects.
/// Supports configurable signing algorithms and custom claim injection.
/// </summary>
public sealed class JwtGenerator
{
    private readonly AuthServerOptions _options;
    private readonly ILogger<JwtGenerator> _logger;
    private readonly SigningCredentials _signingCredentials;

    /// <summary>
    /// Initializes a new instance of <see cref="JwtGenerator"/>.
    /// </summary>
    /// <param name="options">Server configuration containing issuer URL and signing key.</param>
    /// <param name="logger">Logger instance.</param>
    public JwtGenerator(IOptions<AuthServerOptions> options, ILogger<JwtGenerator> logger)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var keyBytes = Encoding.UTF8.GetBytes(_options.JwtSigningKey);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        _signingCredentials = new SigningCredentials(securityKey, _options.JwtAlgorithm);
    }

    /// <summary>
    /// Generates a signed JWT access token for the given user and scopes.
    /// </summary>
    /// <param name="user">The authenticated user entity.</param>
    /// <param name="clientId">The requesting OAuth2 client identifier.</param>
    /// <param name="scopes">Space-separated scope string.</param>
    /// <param name="additionalClaims">Optional extra claims to embed in the token.</param>
    /// <returns>The serialized JWT string.</returns>
    public string GenerateAccessToken(
        User user,
        string clientId,
        string scopes,
        IEnumerable<Claim>? additionalClaims = null)
    {
        var claims = BuildBaseClaims(user, clientId, scopes);
        if (additionalClaims is not null)
        {
            claims.AddRange(additionalClaims);
        }

        var token = CreateToken(claims, TimeSpan.FromSeconds(_options.AccessTokenLifetimeSeconds));
        _logger.LogDebug("Generated access token for user {UserId}, client {ClientId}, scopes [{Scopes}]",
            user.UserId, clientId, scopes);

        return token;
    }

    /// <summary>
    /// Generates a signed OpenID Connect ID token for the given user.
    /// </summary>
    /// <param name="user">The authenticated user entity.</param>
    /// <param name="clientId">The audience (client) identifier.</param>
    /// <param name="nonce">The nonce value from the authorization request, if any.</param>
    /// <returns>The serialized JWT ID token string.</returns>
    public string GenerateIdToken(User user, string clientId, string? nonce = null)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("email_verified", user.EmailVerified.ToString().ToLowerInvariant()),
            new(JwtRegisteredClaimNames.Aud, clientId)
        };

        if (!string.IsNullOrEmpty(user.FullName))
        {
            claims.Add(new Claim("name", user.FullName));
        }

        if (!string.IsNullOrEmpty(nonce))
        {
            claims.Add(new Claim(JwtRegisteredClaimNames.Nonce, nonce));
        }

        var token = CreateToken(claims, TimeSpan.FromSeconds(_options.AccessTokenLifetimeSeconds));
        _logger.LogDebug("Generated ID token for user {UserId}, client {ClientId}", user.UserId, clientId);

        return token;
    }

    /// <summary>
    /// Validates the signing key length against the configured algorithm requirements.
    /// </summary>
    /// <returns><c>true</c> if the key meets minimum length requirements; otherwise <c>false</c>.</returns>
    public bool ValidateKeyStrength()
    {
        var keyLengthBits = Encoding.UTF8.GetByteCount(_options.JwtSigningKey) * 8;
        var requiredBits = _options.JwtAlgorithm switch
        {
            "HS256" => 256,
            "HS384" => 384,
            "HS512" => 512,
            _ => 256
        };

        if (keyLengthBits < requiredBits)
        {
            _logger.LogWarning("JWT signing key is {Actual} bits but {Algorithm} requires at least {Required} bits",
                keyLengthBits, _options.JwtAlgorithm, requiredBits);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Computes the thumbprint of the current signing key for JWK Set (JWKS) endpoint use.
    /// </summary>
    /// <returns>Base64url-encoded SHA-256 thumbprint of the signing key.</returns>
    public string GetKeyThumbprint()
    {
        var keyBytes = Encoding.UTF8.GetBytes(_options.JwtSigningKey);
        var hash = System.Security.Cryptography.SHA256.HashData(keyBytes);
        return Convert.ToBase64String(hash).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private List<Claim> BuildBaseClaims(User user, string clientId, string scopes)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("client_id", clientId),
            new("scope", scopes)
        };

        if (user.Roles is { Count: > 0 })
        {
            foreach (var role in user.Roles)
            {
                claims.Add(new Claim("role", role));
            }
        }

        return claims;
    }

    private string CreateToken(List<Claim> claims, TimeSpan lifetime)
    {
        var now = DateTime.UtcNow;
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.IssuerUrl,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.Add(lifetime),
            SigningCredentials = _signingCredentials
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        return handler.WriteToken(token);
    }
}
