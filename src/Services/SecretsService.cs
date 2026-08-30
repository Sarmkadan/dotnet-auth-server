#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Service for handling secret operations: generation, hashing, and comparison.
/// Provides cryptographically secure methods to prevent common vulnerabilities
/// like timing attacks or weak random generation.
/// </summary>
public sealed class SecretsService
{
    // 32 bytes provides a 256-bit random secret by default.
    private const int SecretByteLength = 32;

    // 16 bytes provides a 128-bit unique salt for each secret hash.
    private const int SaltByteLength = 16;

    // 32 bytes produces a 256-bit derived key matching SHA-256.
    private const int Pbkdf2OutputByteLength = 32;

    // 10,000 iterations preserves the established PBKDF2 work factor.
    private const int Pbkdf2Iterations = 10000;

    // SHA-256 is the established digest used by stored secret hashes.
    private static readonly HashAlgorithmName Pbkdf2HashAlgorithm = HashAlgorithmName.SHA256;

    private readonly ILogger<SecretsService> _logger;

    public SecretsService(ILogger<SecretsService> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Generates a cryptographically secure random secret.
    /// Suitable for client secrets, API keys, and tokens.
    /// </summary>
    public string GenerateSecureSecret(int length = SecretByteLength)
    {
        if (length < 16 || length > 256)
        {
            _logger.LogWarning("Invalid secret length requested: {Length}", length);
            throw new ArgumentException("Secret length must be between 16 and 256");
        }

        var bytes = new byte[length];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        // Base64url encoding for URL-safe representation
        var secret = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        return secret;
    }

    /// <summary>
    /// Hashes a secret using PBKDF2 with SHA256.
    /// Suitable for storing secrets securely in database.
    /// Uses unique salt per secret to prevent rainbow tables.
    /// </summary>
    public SecretHash HashSecret(string secret, int iterations = Pbkdf2Iterations)
    {
        ArgumentException.ThrowIfNullOrEmpty(secret);

        // Generate unique salt
        var salt = new byte[SaltByteLength];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // Hash with PBKDF2
        using (var pbkdf2 = new Rfc2898DeriveBytes(secret, salt, iterations, Pbkdf2HashAlgorithm))
        {
            var hash = pbkdf2.GetBytes(Pbkdf2OutputByteLength);

            return new SecretHash
            {
                Hash = Convert.ToBase64String(hash),
                Salt = Convert.ToBase64String(salt),
                Iterations = iterations,
                Algorithm = "PBKDF2-SHA256"
            };
        }
    }

    /// <summary>
    /// Verifies a secret against its hash.
    /// Uses constant-time comparison to prevent timing attacks.
    /// </summary>
    public bool VerifySecret(string secret, SecretHash hash)
    {
        ArgumentException.ThrowIfNullOrEmpty(secret);
        ArgumentNullException.ThrowIfNull(hash);

        try
        {
            var salt = Convert.FromBase64String(hash.Salt);
            using (var pbkdf2 = new Rfc2898DeriveBytes(
                secret,
                salt,
                hash.Iterations,
                Pbkdf2HashAlgorithm))
            {
                var computedHash = pbkdf2.GetBytes(Pbkdf2OutputByteLength);
                var storedHash = Convert.FromBase64String(hash.Hash);

                // Constant-time comparison to prevent timing attacks
                return ConstantTimeCompare(computedHash, storedHash);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying secret");
            return false;
        }
    }

    /// <summary>
    /// Performs constant-time byte array comparison.
    /// Prevents timing attacks by always comparing all bytes regardless of match.
    /// </summary>
    private static bool ConstantTimeCompare(byte[] array1, byte[] array2)
    {
        if (array1.Length != array2.Length)
            return false;

        int result = 0;
        for (int i = 0; i < array1.Length; i++)
        {
            result |= array1[i] ^ array2[i];
        }

        return result == 0;
    }

    /// <summary>
    /// Generates a secure random token with specified length.
    /// Base64url encoded for safe transmission.
    /// </summary>
    public string GenerateToken(int length = SecretByteLength)
    {
        return GenerateSecureSecret(length);
    }

    /// <summary>
    /// Masks a secret for logging purposes.
    /// Shows only first and last few characters.
    /// </summary>
    public static string MaskSecret(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret) || secret.Length <= 6)
            return "***";

        return $"{secret.Substring(0, 3)}***{secret.Substring(secret.Length - 3)}";
    }
}

/// <summary>
/// Hashed secret with metadata for storage.
/// </summary>
public sealed class SecretHash
{
    public string Hash { get; set; } = string.Empty;
    public string Salt { get; set; } = string.Empty;
    public int Iterations { get; set; } = 10000;
    public string Algorithm { get; set; } = "PBKDF2-SHA256";

    public override string ToString()
    {
        // Return masked version for logging
        return $"SecretHash({Algorithm}, iterations={Iterations})";
    }
}
