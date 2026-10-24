#nullable enable
using System.ComponentModel.DataAnnotations;
using DotnetAuthServer.Diagnostics;
using DotnetAuthServer.Services;

namespace DotnetAuthServer.Configuration;

/// <summary>
/// Configuration options for the authorization server
/// </summary>
public sealed class AuthServerOptions
{
    /// <summary>
    /// Issuer identifier (URL) of this authorization server, used as the token issuer.
    /// </summary>
    [Required, Url]
    public string IssuerUrl { get; set; } = null!;

    /// <summary>
    /// Secret key used to sign JWTs. Must be at least 32 characters; should come from the environment outside local development.
    /// </summary>
    [Required, MinLength(32)]
    public string JwtSigningKey { get; set; } = null!;

    /// <summary>
    /// JWT signing algorithm. Defaults to HS256.
    /// </summary>
    [Required]
    public string JwtAlgorithm { get; set; } = "HS256";

    /// <summary>
    /// Lifetime of issued access tokens, in seconds. Defaults to 3600.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int AccessTokenLifetimeSeconds { get; set; } = 3600;

    /// <summary>
    /// Lifetime of issued refresh tokens, in seconds. Defaults to 2592000 (30 days).
    /// </summary>
    [Range(1, int.MaxValue)]
    public int RefreshTokenLifetimeSeconds { get; set; } = 2592000;

    /// <summary>
    /// Lifetime of authorization codes, in seconds. Defaults to 300.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int AuthorizationCodeLifetimeSeconds { get; set; } = 300;

    /// <summary>
    /// Whether PKCE is required for all clients. Defaults to true.
    /// </summary>
    public bool RequirePkceForAllClients { get; set; } = true;

    /// <summary>
    /// Whether refresh tokens are automatically rotated when used. Defaults to true.
    /// </summary>
    public bool AutoRefreshTokenRotation { get; set; } = true;

    /// <summary>
    /// Maximum number of refresh token generations allowed. Defaults to 10.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int MaxRefreshTokenGenerations { get; set; } = 10;

    /// <summary>
    /// Clock skew tolerance, in seconds, applied when validating token times. Defaults to 300.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int ClockSkewToleranceSeconds { get; set; } = 300;

    /// <summary>
    /// Connection string for the backing database.
    /// </summary>
    [Required]
    public string DatabaseConnectionString { get; set; } = null!;

    /// <summary>
    /// Whether to use the in-memory store instead of a database. Defaults to false.
    /// </summary>
    public bool UseInMemoryDatabase { get; set; } = false;

    /// <summary>
    /// Number of failed login attempts before the account is locked. Defaults to 5.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int FailedLoginAttemptThreshold { get; set; } = 5;

    /// <summary>
    /// How long, in minutes, an account stays locked after too many failed logins. Defaults to 15.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int AccountLockoutDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Maximum number of TOTP verification attempts allowed per rate-limit window. Defaults to 5.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int TotpAttemptsPerWindow { get; set; } = 5;

    /// <summary>
    /// Length of the TOTP verification rate-limit window, in seconds. Defaults to 30.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int TotpRateLimitWindowSeconds { get; set; } = 30;

    /// <summary>
    /// Whether users must consent before a client is granted access. Defaults to true.
    /// </summary>
    public bool RequireUserConsent { get; set; } = true;

    /// <summary>
    /// Number of days a stored user consent remains valid. Defaults to 30.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int ConsentExpirationDays { get; set; } = 30;

    /// <summary>
    /// Number of hours a session-scoped consent remains valid. Defaults to 1.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int SessionConsentExpirationHours { get; set; } = 1;

    /// <summary>
    /// Password policy applied to user passwords.
    /// </summary>
    [Required]
    public PasswordPolicyOptions PasswordPolicy { get; set; } = new PasswordPolicyOptions();

    /// <summary>
    /// Scopes supported by the server. Defaults to openid, profile, email, phone, address and offline_access.
    /// </summary>
    public ICollection<string> SupportedScopes { get; set; } =
    [
        Constants.Scopes.OpenId,
        Constants.Scopes.Profile,
        Constants.Scopes.Email,
        Constants.Scopes.Phone,
        Constants.Scopes.Address,
        Constants.Scopes.OfflineAccess
    ];

    /// <summary>
    /// Grant types supported by the server. Defaults to authorization_code, refresh_token, client_credentials and password.
    /// </summary>
    public ICollection<string> SupportedGrantTypes { get; set; } =
    [
        Constants.GrantTypes.AuthorizationCode,
        Constants.GrantTypes.RefreshToken,
        Constants.GrantTypes.ClientCredentials,
        Constants.GrantTypes.Password
    ];
    /// <summary>
    /// Returns a string representation of these options with the signing key redacted.
    /// </summary>
    public override string ToString() => 
        $"AuthServerOptions {{ IssuerUrl = {IssuerUrl}, JwtSigningKey = {Redaction.Secret(JwtSigningKey)}, JwtAlgorithm = {JwtAlgorithm}, AccessTokenLifetimeSeconds = {AccessTokenLifetimeSeconds}, RefreshTokenLifetimeSeconds = {RefreshTokenLifetimeSeconds}, AuthorizationCodeLifetimeSeconds = {AuthorizationCodeLifetimeSeconds} }}";
}
