#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Diagnostics;

using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Reads display-only metadata (jti, exp) from issued JWTs. Signatures are never validated here;
/// this must not be used for authorization decisions.
/// </summary>
internal static class JwtDiagnostics
{
    /// <summary>
    /// Parses a JWT without validating it.
    /// </summary>
    /// <param name="jwt">The serialized token. May be <c>null</c> or malformed.</param>
    /// <returns>The parsed token, or <c>null</c> when the input is empty or cannot be parsed.</returns>
    public static JwtSecurityToken? TryRead(string? jwt)
    {
        if (string.IsNullOrWhiteSpace(jwt))
        {
            return null;
        }

        try
        {
            return new JwtSecurityTokenHandler().ReadJwtToken(jwt);
        }
        catch (Exception exception) when (exception is ArgumentException or SecurityTokenException)
        {
            return null;
        }
    }
}
