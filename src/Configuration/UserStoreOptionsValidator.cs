#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Configuration;

using Microsoft.Extensions.Options;

/// <summary>
/// Validates <see cref="UserStoreOptions"/> together with the <see cref="AuthServerOptions"/>
/// lockout and storage settings the user store depends on. Every error message names the
/// configuration key that must be corrected.
/// </summary>
public sealed class UserStoreOptionsValidator : IValidateOptions<UserStoreOptions>
{
    /// <summary>
    /// The only password hashing algorithm supported: PBKDF2-HMAC-SHA256.
    /// </summary>
    public const string Pbkdf2Algorithm = "Pbkdf2";

    /// <summary>
    /// Minimum PBKDF2 iteration count (OWASP guidance for PBKDF2-HMAC-SHA256).
    /// </summary>
    public const int MinPbkdf2Iterations = 100_000;

    /// <summary>
    /// Maximum PBKDF2 iteration count, bounding per-login hashing cost.
    /// </summary>
    public const int MaxPbkdf2Iterations = 10_000_000;

    private const string AuthServerKeyPrefix = "DotnetAuthServer:AuthServer";
    private const string PasswordHashingKeyPrefix = UserStoreOptions.SectionKey + ":PasswordHashing";

    private readonly AuthServerOptions _authServerOptions;

    /// <summary>
    /// Initializes a new instance of <see cref="UserStoreOptionsValidator"/>.
    /// </summary>
    /// <param name="authServerOptions">The auth server options holding lockout and storage settings.</param>
    /// <exception cref="ArgumentNullException"><paramref name="authServerOptions"/> is null.</exception>
    public UserStoreOptionsValidator(AuthServerOptions authServerOptions)
    {
        _authServerOptions = authServerOptions ?? throw new ArgumentNullException(nameof(authServerOptions));
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, UserStoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();
        errors.AddRange(ValidatePasswordHashing(options.PasswordHashing));
        errors.AddRange(ValidateLockout(_authServerOptions));
        errors.AddRange(ValidateStorage(_authServerOptions));

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }

    /// <summary>
    /// Validates the password hashing parameters.
    /// </summary>
    /// <param name="hashing">The hashing options to validate.</param>
    /// <returns>A list of human-readable errors, or an empty list if valid.</returns>
    public static IReadOnlyList<string> ValidatePasswordHashing(PasswordHashingOptions? hashing)
    {
        var errors = new List<string>();

        if (hashing is null)
        {
            errors.Add($"{UserStoreOptions.SectionKey}:PasswordHashing must be configured.");
            return errors.AsReadOnly();
        }

        if (string.IsNullOrWhiteSpace(hashing.Algorithm))
        {
            errors.Add($"{PasswordHashingKeyPrefix}:Algorithm must be a non-empty string. Supported value: {Pbkdf2Algorithm}.");
        }
        else if (!string.Equals(hashing.Algorithm, Pbkdf2Algorithm, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add($"{PasswordHashingKeyPrefix}:Algorithm '{hashing.Algorithm}' is not supported. Supported value: {Pbkdf2Algorithm} (PBKDF2-HMAC-SHA256). Argon2id is not available in the .NET base class library.");
        }

        if (hashing.Iterations < MinPbkdf2Iterations || hashing.Iterations > MaxPbkdf2Iterations)
        {
            errors.Add($"{PasswordHashingKeyPrefix}:Iterations must be between {MinPbkdf2Iterations} and {MaxPbkdf2Iterations} (inclusive), but was {hashing.Iterations}.");
        }

        return errors.AsReadOnly();
    }

    private static IReadOnlyList<string> ValidateLockout(AuthServerOptions authServer)
    {
        var errors = new List<string>();

        if (authServer.FailedLoginAttemptThreshold <= 0)
        {
            errors.Add($"{AuthServerKeyPrefix}:FailedLoginAttemptThreshold (max failed login attempts) must be greater than 0, but was {authServer.FailedLoginAttemptThreshold}.");
        }

        if (authServer.AccountLockoutDurationMinutes <= 0)
        {
            errors.Add($"{AuthServerKeyPrefix}:AccountLockoutDurationMinutes (lockout duration) must be greater than 0, but was {authServer.AccountLockoutDurationMinutes}.");
        }

        return errors.AsReadOnly();
    }

    private static IReadOnlyList<string> ValidateStorage(AuthServerOptions authServer)
    {
        var errors = new List<string>();

        // The only user storage implementation is in-memory. Refuse to start when a persistent
        // backend is requested, rather than silently ignoring DatabaseConnectionString.
        if (!authServer.UseInMemoryDatabase)
        {
            errors.Add($"{AuthServerKeyPrefix}:UseInMemoryDatabase is false, but no persistent user storage provider is registered, so {AuthServerKeyPrefix}:DatabaseConnectionString cannot be used. Set {AuthServerKeyPrefix}:UseInMemoryDatabase to true.");
        }

        return errors.AsReadOnly();
    }
}
