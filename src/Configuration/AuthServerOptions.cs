// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Configuration;

/// <summary>
/// Configuration options for the authorization server
/// </summary>
public class AuthServerOptions
{
    /// <summary>
    /// The issuer URL (will be used in JWT tokens)
    /// </summary>
    public string IssuerUrl { get; set; } = null!;

    /// <summary>
    /// JWT signing key (minimum 256 bits/32 bytes for HS256)
    /// </summary>
    public string JwtSigningKey { get; set; } = null!;

    /// <summary>
    /// Algorithm for JWT signing (HS256, RS256, etc.)
    /// </summary>
    public string JwtAlgorithm { get; set; } = "HS256";

    /// <summary>
    /// Access token lifetime in seconds
    /// </summary>
    public int AccessTokenLifetimeSeconds { get; set; } = 3600;

    /// <summary>
    /// Refresh token lifetime in seconds
    /// </summary>
    public int RefreshTokenLifetimeSeconds { get; set; } = 2592000;

    /// <summary>
    /// Authorization code lifetime in seconds
    /// </summary>
    public int AuthorizationCodeLifetimeSeconds { get; set; } = 300;

    /// <summary>
    /// Whether to require PKCE for all clients
    /// </summary>
    public bool RequirePkceForAllClients { get; set; } = true;

    /// <summary>
    /// Whether to automatically rotate refresh tokens
    /// </summary>
    public bool AutoRefreshTokenRotation { get; set; } = true;

    /// <summary>
    /// Maximum number of refresh token generations to track (for replay detection)
    /// </summary>
    public int MaxRefreshTokenGenerations { get; set; } = 10;

    /// <summary>
    /// Database connection string
    /// </summary>
    public string DatabaseConnectionString { get; set; } = null!;

    /// <summary>
    /// Whether to use in-memory database (for testing)
    /// </summary>
    public bool UseInMemoryDatabase { get; set; } = false;

    /// <summary>
    /// Failed login attempt threshold before account lockout
    /// </summary>
    public int FailedLoginAttemptThreshold { get; set; } = 5;

    /// <summary>
    /// Account lockout duration in minutes
    /// </summary>
    public int AccountLockoutDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Whether to require user consent for scope access
    /// </summary>
    public bool RequireUserConsent { get; set; } = true;

    /// <summary>
    /// Supported scopes
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
    /// Supported grant types
    /// </summary>
    public ICollection<string> SupportedGrantTypes { get; set; } =
    [
        Constants.GrantTypes.AuthorizationCode,
        Constants.GrantTypes.RefreshToken,
        Constants.GrantTypes.ClientCredentials,
        Constants.GrantTypes.Password
    ];

    /// <summary>
    /// Validates the configuration is correct
    /// </summary>
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(IssuerUrl) &&
               !string.IsNullOrWhiteSpace(JwtSigningKey) &&
               JwtSigningKey.Length >= 32 &&
               !string.IsNullOrWhiteSpace(DatabaseConnectionString) &&
               AccessTokenLifetimeSeconds > 0 &&
               RefreshTokenLifetimeSeconds > 0 &&
               AuthorizationCodeLifetimeSeconds > 0;
    }
}
