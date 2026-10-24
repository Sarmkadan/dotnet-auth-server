#nullable enable
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
public sealed class JwksHandler
{
    /// <summary>
    /// Returns a string listing the cached JWKS keys, if any.
    /// </summary>
    /// <returns>A string representation of the handler's cached key set.</returns>
    public override string ToString() => $"JwksHandler {{ Keys = {string.Join(", ", (_cacheService.GetAsync<JwksResponse>(JwksKey, default).Result?.Keys ?? []).Select(k => $"Kty = {k.Kty}, Kid = {k.Kid}, Use = {k.Use}, Alg = {k.Alg}, K = {k.K}"))} }}";
    private readonly AuthServerOptions _options;
    private readonly ICacheService _cacheService;
    private readonly ILogger<JwksHandler> _logger;

    private const string JwksKey = "jwks";

    /// <summary>
    /// Initializes a new instance of the <see cref="JwksHandler"/> class.
    /// </summary>
    /// <param name="options">Server options containing the signing configuration.</param>
    /// <param name="cacheService">Cache used to store the generated key set.</param>
    /// <param name="logger">Logger used to record JWKS activity.</param>
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
        _logger.LogInformation("Getting JWKS");
        var cached = await _cacheService.GetAsync<JwksResponse>(JwksKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogInformation("Returning cached JWKS");
            return cached;
        }

        var jwks = GenerateJwks();

        // Cache for 24 hours
        await _cacheService.SetAsync(JwksKey, jwks, TimeSpan.FromHours(24), cancellationToken);

        _logger.LogInformation("Returning generated JWKS with {KeyCount} keys", jwks.Keys.Count);
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
        if (jwk is not null)
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

            _logger.LogInformation("Generating JWK from signing key with {Thumbprint}", thumbprint);

            // NOTE: for symmetric (oct) keys the key material MUST NOT be published -
            // JWKS is a public document. Only the key id / alg / use are exposed so
            // resource servers can match the kid; validation of HS256 tokens requires
            // the shared secret to be distributed out of band anyway.
            return new JwkKey
            {
                Kty = "oct",
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
        _logger.LogInformation("Checking if key ID {KeyId} is valid", keyId);
        var jwks = await GetJwksAsync(cancellationToken);
        return jwks.Keys.Any(k => k.Kid == keyId);
    }
}

/// <summary>
/// JSON Web Key Set response per RFC 7517.
/// </summary>
public sealed class JwksResponse
{
    /// <summary>
    /// The public keys in the set.
    /// </summary>
    [JsonPropertyName("keys")]
    public List<JwkKey> Keys { get; set; } = new();
}

/// <summary>
/// Single JSON Web Key (JWK) per RFC 7517.
/// </summary>
public sealed class JwkKey
{
    /// <summary>
    /// Key type (RSA, oct, EC, etc.).
    /// </summary>
    [JsonPropertyName("kty")]
    public string? Kty { get; set; } // Key type (RSA, oct, EC, etc.)

    /// <summary>
    /// Key identifier.
    /// </summary>
    [JsonPropertyName("kid")]
    public string? Kid { get; set; } // Key ID

    /// <summary>
    /// Intended key use (sig, enc).
    /// </summary>
    [JsonPropertyName("use")]
    public string? Use { get; set; } // Key use (sig, enc)

    /// <summary>
    /// Algorithm intended for use with the key.
    /// </summary>
    [JsonPropertyName("alg")]
    public string? Alg { get; set; } // Algorithm

    /// <summary>
    /// Symmetric key material, for oct keys.
    /// </summary>
    [JsonPropertyName("k")]
    public string? K { get; set; } // Symmetric key material (for oct keys)

    /// <summary>
    /// Returns a string representation of this key, excluding the modulus and exponent.
    /// </summary>
    /// <returns>A string representation of the key.</returns>
    public override string ToString() => $"JwkKey {{ Kty = {Kty}, Kid = {Kid}, Use = {Use}, Alg = {Alg}, K = {K} }}";

    /// <summary>
    /// RSA modulus.
    /// </summary>
    [JsonPropertyName("n")]
    public string? N { get; set; } // RSA modulus

    /// <summary>
    /// RSA public exponent.
    /// </summary>
    [JsonPropertyName("e")]
    public string? E { get; set; } // RSA exponent

    // Additional fields for different key types can be added as needed
}
