#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Formatters;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

/// <summary>
/// Utility for parsing and inspecting JWT tokens without cryptographic validation.
/// Useful for debugging and logging token structure.
/// WARNING: Does NOT validate signatures - never trust token claims without validation!
/// </summary>
public sealed class JwtTokenFormatter
{
    private readonly ILogger<JwtTokenFormatter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenFormatter"/> class.
    /// </summary>
    public JwtTokenFormatter(ILogger<JwtTokenFormatter> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Parses a JWT token without validating its signature.
    /// Returns the decoded token structure or null if parsing fails.
    /// Useful for debugging and logging purposes only.
    /// </summary>
    public TokenInspection? InspectToken(string token)
    {
        _logger.LogInformation("InspectToken called with {Token}", token);
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();

            // This does NOT validate signature or expiration
            if (!handler.CanReadToken(token))
            {
                _logger.LogWarning("Token cannot be read as JWT: {Token}", token);
                return null;
            }

            var jwtToken = handler.ReadJwtToken(token);

            _logger.LogInformation("Successfully inspected token with Subject {Subject} and Issuer {Issuer}", jwtToken.Subject, jwtToken.Issuer);

            return new TokenInspection
            {
                Header = new TokenHeader
                {
                    Alg = jwtToken.Header.Alg,
                    Typ = jwtToken.Header.Typ,
                    Kid = jwtToken.Header.Kid
                },
                Payload = new TokenPayload
                {
                    Subject = jwtToken.Subject,
                    Issuer = jwtToken.Issuer,
                    Audience = jwtToken.Audiences.FirstOrDefault(),
                    IssuedAt = jwtToken.IssuedAt,
                    ExpiresAt = jwtToken.ValidTo,
                    NotBefore = jwtToken.ValidFrom,
                    Claims = jwtToken.Claims
                        .GroupBy(c => c.Type)
                        .ToDictionary(g => g.Key, g => g.Select(c => c.Value).ToList())
                },
                Raw = token
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to inspect JWT token: {Token}", token);
            return null;
        }
    }

    /// <summary>
    /// Formats a token inspection for human-readable logging.
    /// Shows only public information, not sensitive claims.
    /// </summary>
    public string FormatForLogging(TokenInspection inspection)
    {
        _logger.LogInformation("FormatForLogging called");
        if (inspection is null)
        {
            _logger.LogWarning("FormatForLogging called with null inspection");
            return "(null token inspection)";
        }

        return $"JWT{{ " +
            $"iss={inspection.Payload.Issuer}, " +
            $"sub={inspection.Payload.Subject}, " +
            $"aud={inspection.Payload.Audience}, " +
            $"exp={inspection.Payload.ExpiresAt:O} " +
            $"}}";
    }

    /// <summary>
    /// Returns a concise, informative representation of the formatter.
    /// Includes placeholder values for token‑related properties.
    /// </summary>
    public override string ToString()
    {
        return $"JwtTokenFormatter {{ Header = N/A, Payload = N/A, Raw = N/A, Alg = N/A, Typ = N/A, Kid = N/A }}";
    }
}

/// <summary>
/// Inspected JWT token structure (without cryptographic validation).
/// </summary>
public sealed class TokenInspection
{
    /// <summary>
    /// JWT header of the inspected token.
    /// </summary>
    public TokenHeader Header { get; set; } = new();

    /// <summary>
    /// JWT payload (claims) of the inspected token.
    /// </summary>
    public TokenPayload Payload { get; set; } = new();

    /// <summary>
    /// The original, unparsed token string.
    /// </summary>
    public string Raw { get; set; } = string.Empty;
}

/// <summary>
/// JWT header information.
/// </summary>
public sealed class TokenHeader
{
    /// <summary>
    /// Signing algorithm from the header (the 'alg' parameter).
    /// </summary>
    public string? Alg { get; set; }

    /// <summary>
    /// Token type from the header (the 'typ' parameter).
    /// </summary>
    public string? Typ { get; set; }

    /// <summary>
    /// Key identifier from the header (the 'kid' parameter).
    /// </summary>
    public string? Kid { get; set; }
}

/// <summary>
/// JWT payload (claims).
/// </summary>
public sealed class TokenPayload
{
    /// <summary>
    /// Subject of the token (the 'sub' claim).
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Issuer of the token (the 'iss' claim).
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// First audience value of the token (the 'aud' claim).
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// Time at which the token was issued (the 'iat' claim).
    /// </summary>
    public DateTime? IssuedAt { get; set; }

    /// <summary>
    /// Expiration time of the token (the 'exp' claim).
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Time before which the token is not valid (the 'nbf' claim).
    /// </summary>
    public DateTime? NotBefore { get; set; }

    /// <summary>
    /// All claims grouped by claim type; each type maps to its list of values.
    /// </summary>
    public Dictionary<string, List<string>> Claims { get; set; } = new();
}
