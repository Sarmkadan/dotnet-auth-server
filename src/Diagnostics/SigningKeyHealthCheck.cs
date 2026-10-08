#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Diagnostics;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DotnetAuthServer.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;

/// <summary>
/// Readiness check that confirms the JWT signing key is loaded and usable for token issuance.
/// The check signs a throwaway token with the configured key and algorithm; the key itself is never reported.
/// </summary>
public sealed class SigningKeyHealthCheck : IHealthCheck
{
    // Matches the minimum enforced by AuthServerOptionsValidation for JwtSigningKey.
    private const int MinSigningKeyLength = 32;

    private readonly AuthServerOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="SigningKeyHealthCheck"/>.
    /// </summary>
    /// <param name="options">Server options containing the signing key and algorithm.</param>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    public SigningKeyHealthCheck(AuthServerOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var key = _options.JwtSigningKey;

        if (string.IsNullOrWhiteSpace(key))
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("No JWT signing key is loaded."));
        }

        if (key.Length < MinSigningKeyLength)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"JWT signing key is shorter than the required {MinSigningKeyLength} characters."));
        }

        try
        {
            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                _options.JwtAlgorithm);

            var probe = new JwtSecurityToken(
                claims: [new Claim("probe", "1")],
                expires: DateTime.UtcNow.AddMinutes(1),
                signingCredentials: credentials);

            _ = new JwtSecurityTokenHandler().WriteToken(probe);

            return Task.FromResult(HealthCheckResult.Healthy($"Signing key loaded; algorithm {_options.JwtAlgorithm}."));
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or SecurityTokenException)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"JWT signing key cannot be used with algorithm {_options.JwtAlgorithm}.", exception));
        }
    }
}
