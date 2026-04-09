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
public class JwtTokenFormatter
{
    private readonly ILogger<JwtTokenFormatter> _logger;

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
        if (string.IsNullOrWhiteSpace(token))
            return null;

        try
        {
            var handler = new JwtSecurityTokenHandler();

            // This does NOT validate signature or expiration
            if (!handler.CanReadToken(token))
            {
                _logger.LogWarning("Token cannot be read as JWT");
                return null;
            }

            var jwtToken = handler.ReadJwtToken(token);

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
            _logger.LogDebug(ex, "Failed to inspect JWT token");
            return null;
        }
    }

    /// <summary>
    /// Formats a token inspection for human-readable logging.
    /// Shows only public information, not sensitive claims.
    /// </summary>
    public string FormatForLogging(TokenInspection inspection)
    {
        if (inspection == null)
            return "(null token inspection)";

        return $"JWT{{ " +
            $"iss={inspection.Payload.Issuer}, " +
            $"sub={inspection.Payload.Subject}, " +
            $"aud={inspection.Payload.Audience}, " +
            $"exp={inspection.Payload.ExpiresAt:O} " +
            $"}}";
    }
}

/// <summary>
/// Inspected JWT token structure (without cryptographic validation).
/// </summary>
public class TokenInspection
{
    public TokenHeader Header { get; set; } = new();
    public TokenPayload Payload { get; set; } = new();
    public string Raw { get; set; } = string.Empty;
}

/// <summary>
/// JWT header information.
/// </summary>
public class TokenHeader
{
    public string? Alg { get; set; }
    public string? Typ { get; set; }
    public string? Kid { get; set; }
}

/// <summary>
/// JWT payload (claims).
/// </summary>
public class TokenPayload
{
    public string? Subject { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? NotBefore { get; set; }
    public Dictionary<string, List<string>> Claims { get; set; } = new();
}
