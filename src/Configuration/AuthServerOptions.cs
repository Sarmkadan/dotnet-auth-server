#nullable enable
using System.ComponentModel.DataAnnotations;
using DotnetAuthServer.Services;

namespace DotnetAuthServer.Configuration;

/// <summary>
/// Configuration options for the authorization server
/// </summary>
public sealed class AuthServerOptions
{
    [Required, Url]
    public string IssuerUrl { get; set; } = null!;

    [Required, MinLength(32)]
    public string JwtSigningKey { get; set; } = null!;

    [Required]
    public string JwtAlgorithm { get; set; } = "HS256";

    [Range(1, int.MaxValue)]
    public int AccessTokenLifetimeSeconds { get; set; } = 3600;

    [Range(1, int.MaxValue)]
    public int RefreshTokenLifetimeSeconds { get; set; } = 2592000;

    [Range(1, int.MaxValue)]
    public int AuthorizationCodeLifetimeSeconds { get; set; } = 300;

    public bool RequirePkceForAllClients { get; set; } = true;

    public bool AutoRefreshTokenRotation { get; set; } = true;

    [Range(1, int.MaxValue)]
    public int MaxRefreshTokenGenerations { get; set; } = 10;

    [Range(0, int.MaxValue)]
    public int ClockSkewToleranceSeconds { get; set; } = 300;

    [Required]
    public string DatabaseConnectionString { get; set; } = null!;

    public bool UseInMemoryDatabase { get; set; } = false;

    [Range(1, int.MaxValue)]
    public int FailedLoginAttemptThreshold { get; set; } = 5;

    [Range(1, int.MaxValue)]
    public int AccountLockoutDurationMinutes { get; set; } = 15;

    public bool RequireUserConsent { get; set; } = true;

[Range(1, int.MaxValue)]
public int ConsentExpirationDays { get; set; } = 30;

[Range(1, int.MaxValue)]
public int SessionConsentExpirationHours { get; set; } = 1;

[Required]
public PasswordPolicyOptions PasswordPolicy { get; set; } = new PasswordPolicyOptions();

    public ICollection<string> SupportedScopes { get; set; } =
    [
        Constants.Scopes.OpenId,
        Constants.Scopes.Profile,
        Constants.Scopes.Email,
        Constants.Scopes.Phone,
        Constants.Scopes.Address,
        Constants.Scopes.OfflineAccess
    ];

    public ICollection<string> SupportedGrantTypes { get; set; } =
    [
        Constants.GrantTypes.AuthorizationCode,
        Constants.GrantTypes.RefreshToken,
        Constants.GrantTypes.ClientCredentials,
        Constants.GrantTypes.Password
    ];
}
