// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Handlers;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetAuthServer.Caching;
using DotnetAuthServer.Configuration;

/// <summary>
/// Handler for JSON Web Key Set (JWKS) endpoint per OIDC specification.
/// Returns the public keys used to validate JWTs issued by this authorization server.
/// Essential for clients that need to validate tokens independently.
/// </summary>
public class JwksHandler
{
    private readonly AuthServerOptions _options;
    private readonly ICacheService _cacheService;
    private readonly ILogger<JwksHandler> _logger;

    private const string JwksKey = "jwks";

    public JwksHandler(
        AuthServerOptions options,
        ICacheService cacheService,
        ILogger<JwksHandler> logger)
    {
        _options = options;
        _cacheService = cacheService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current JWKS (JSON Web Key Set).
    /// Returns cached result if available, otherwise generates from current signing key.
    /// </summary>
    public async Task<JwksResponse> GetJwksAsync(CancellationToken cancellationToken = default)
    {
        var cached = await _cacheService.GetAsync<JwksResponse>(JwksKey, cancellationToken);
        if (cached != null)
        {
            _logger.LogDebug("Returning cached JWKS");
            return cached;
        }

        var jwks = GenerateJwks();

        // Cache for 24 hours
        await _cacheService.SetAsync(JwksKey, jwks, TimeSpan.FromHours(24), cancellationToken);

        return jwks;
    }

    /// <summary>
    /// Generates JWKS from current signing key.
    /// In production, supports multiple keys for rotation.
    /// </summary>
    private JwksResponse GenerateJwks()
    {
        var keys = new List<JwkKey>();

        // Generate JWK for current signing key
        var jwk = GenerateJwkFromSigningKey();
        if (jwk != null)
        {
            keys.Add(jwk);
        }

        _logger.LogInformation("Generated JWKS with {KeyCount} keys", keys.Count);

        return new JwksResponse { Keys = keys };
    }

    /// <summary>
    /// Generates a JWK from the current HMAC signing key.
    /// For production, should support RSA keys too.
    /// </summary>
    private JwkKey? GenerateJwkFromSigningKey()
    {
        try
        {
            // Extract key components from base64-encoded signing key
            var keyBytes = Convert.FromBase64String(_options.JwtSigningKey);

            // Create thumbprint of the key
            var thumbprint = GenerateSha256Thumbprint(keyBytes);

            return new JwkKey
            {
                Kty = "oct",
                K = _options.JwtSigningKey,
                Kid = thumbprint.Substring(0, 16), // Use first 16 chars as key ID
                Use = "sig",
                Alg = _options.JwtAlgorithm ?? "HS256"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating JWK from signing key");
            return null;
        }
    }

    /// <summary>
    /// Generates SHA256 thumbprint of key material.
    /// Useful for key identification without exposing full key.
    /// </summary>
    private static string GenerateSha256Thumbprint(byte[] keyData)
    {
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(keyData);
            return Convert.ToBase64String(hash)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }

    /// <summary>
    /// Validates that a key ID exists in current JWKS.
    /// </summary>
    public async Task<bool> IsValidKeyIdAsync(
        string keyId,
        CancellationToken cancellationToken = default)
    {
        var jwks = await GetJwksAsync(cancellationToken);
        return jwks.Keys.Any(k => k.Kid == keyId);
    }
}

/// <summary>
/// JSON Web Key Set response per RFC 7517.
/// </summary>
public class JwksResponse
{
    [JsonPropertyName("keys")]
    public List<JwkKey> Keys { get; set; } = new();
}

/// <summary>
/// Single JSON Web Key (JWK) per RFC 7517.
/// </summary>
public class JwkKey
{
    [JsonPropertyName("kty")]
    public string? Kty { get; set; } // Key type (RSA, oct, EC, etc.)

    [JsonPropertyName("kid")]
    public string? Kid { get; set; } // Key ID

    [JsonPropertyName("use")]
    public string? Use { get; set; } // Key use (sig, enc)

    [JsonPropertyName("alg")]
    public string? Alg { get; set; } // Algorithm

    [JsonPropertyName("k")]
    public string? K { get; set; } // Symmetric key material (for oct keys)

    [JsonPropertyName("n")]
    public string? N { get; set; } // RSA modulus

    [JsonPropertyName("e")]
    public string? E { get; set; } // RSA exponent

    // Additional fields for different key types can be added as needed
}
