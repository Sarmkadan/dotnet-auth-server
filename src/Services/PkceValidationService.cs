// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using System.Security.Cryptography;
using System.Text;
using DotnetAuthServer.Configuration;

/// <summary>
/// Service for PKCE (Proof Key for Public Clients) validation.
/// Implements RFC 7636 to protect against authorization code interception attacks.
/// Particularly important for mobile and single-page applications that cannot securely store secrets.
/// </summary>
public class PkceValidationService
{
    private readonly AuthServerOptions _options;
    private readonly ILogger<PkceValidationService> _logger;

    private const int MinCodeVerifierLength = 43;
    private const int MaxCodeVerifierLength = 128;

    public PkceValidationService(AuthServerOptions options, ILogger<PkceValidationService> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Generates a cryptographically secure code verifier for PKCE.
    /// </summary>
    public string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }

        // URL-safe base64 encoding without padding
        var verifier = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        if (verifier.Length < MinCodeVerifierLength || verifier.Length > MaxCodeVerifierLength)
        {
            _logger.LogError("Generated code verifier has invalid length: {Length}", verifier.Length);
            throw new InvalidOperationException("Failed to generate valid code verifier");
        }

        return verifier;
    }

    /// <summary>
    /// Generates a code challenge from a code verifier.
    /// Supports both S256 (SHA256) and plain methods.
    /// S256 is recommended and more secure.
    /// </summary>
    public string GenerateCodeChallenge(string codeVerifier, string method = "S256")
    {
        if (string.IsNullOrWhiteSpace(codeVerifier))
            throw new ArgumentException("Code verifier is required");

        if (!IsValidCodeVerifier(codeVerifier))
        {
            _logger.LogWarning("Invalid code verifier format");
            throw new ArgumentException("Code verifier must contain only unreserved characters [A-Z][a-z][0-9]_-.");
        }

        return method switch
        {
            "S256" => GenerateS256Challenge(codeVerifier),
            "plain" => codeVerifier, // Plain method returns verifier itself
            _ => throw new NotSupportedException($"Method '{method}' is not supported")
        };
    }

    /// <summary>
    /// Validates that a code verifier matches the stored code challenge.
    /// </summary>
    public bool ValidateCodeVerifier(string? codeVerifier, string? codeChallenge, string method = "S256")
    {
        if (string.IsNullOrWhiteSpace(codeVerifier) || string.IsNullOrWhiteSpace(codeChallenge))
        {
            _logger.LogWarning("Code verifier or challenge is missing");
            return false;
        }

        if (!IsValidCodeVerifier(codeVerifier))
        {
            _logger.LogWarning("Code verifier has invalid format");
            return false;
        }

        try
        {
            var computedChallenge = GenerateCodeChallenge(codeVerifier, method);
            var isValid = computedChallenge == codeChallenge;

            if (!isValid)
            {
                _logger.LogWarning("Code verifier does not match challenge");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating code verifier");
            return false;
        }
    }

    /// <summary>
    /// Checks if PKCE is required for a specific client or flow.
    /// Public clients should always use PKCE; confidential clients may optionally use it.
    /// </summary>
    public bool IsPkceRequired(bool isConfidentialClient)
    {
        if (_options.RequirePkceForAllClients)
            return true;

        // PKCE is always recommended for public clients (mobile apps, SPAs)
        return !isConfidentialClient;
    }

    /// <summary>
    /// Validates code challenge format and method.
    /// </summary>
    public bool IsValidChallenge(string? codeChallenge, string method)
    {
        if (string.IsNullOrWhiteSpace(codeChallenge))
            return false;

        return method switch
        {
            "S256" => codeChallenge.Length == 43, // S256 produces 43-char base64url
            "plain" => IsValidCodeVerifier(codeChallenge),
            _ => false
        };
    }

    /// <summary>
    /// Checks if code verifier contains only allowed characters.
    /// Per RFC 7636: [A-Z][a-z][0-9]_-.
    /// </summary>
    private bool IsValidCodeVerifier(string verifier)
    {
        if (string.IsNullOrWhiteSpace(verifier))
            return false;

        if (verifier.Length < MinCodeVerifierLength || verifier.Length > MaxCodeVerifierLength)
            return false;

        // Check for only unreserved characters
        return verifier.All(c =>
            (c >= 'A' && c <= 'Z') ||
            (c >= 'a' && c <= 'z') ||
            (c >= '0' && c <= '9') ||
            c == '-' || c == '.' || c == '_' || c == '~');
    }

    /// <summary>
    /// Generates S256 (SHA256) code challenge.
    /// Hashes the verifier and encodes as base64url.
    /// </summary>
    private string GenerateS256Challenge(string codeVerifier)
    {
        using (var sha256 = SHA256.Create())
        {
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
            var challenge = Convert.ToBase64String(hash)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');

            return challenge;
        }
    }
}
