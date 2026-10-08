#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Configuration;

using Microsoft.Extensions.Options;

/// <summary>
/// Validates <see cref="MfaOptions"/> at host start. Every error message names the
/// configuration key that must be corrected and the allowed range.
/// </summary>
public sealed class MfaOptionsValidator : IValidateOptions<MfaOptions>
{
    /// <summary>Allowed TOTP code lengths.</summary>
    public static readonly int[] AllowedDigits = [6, 8];

    /// <summary>Minimum TOTP time step in seconds.</summary>
    public const int MinTimeStepSeconds = 15;

    /// <summary>Maximum TOTP time step in seconds.</summary>
    public const int MaxTimeStepSeconds = 120;

    /// <summary>Minimum number of time steps of allowed drift.</summary>
    public const int MinAllowedDriftSteps = 0;

    /// <summary>Maximum number of time steps of allowed drift.</summary>
    public const int MaxAllowedDriftSteps = 2;

    /// <summary>Minimum number of recovery codes issued at enrollment.</summary>
    public const int MinRecoveryCodeCount = 1;

    /// <summary>Maximum number of recovery codes issued at enrollment.</summary>
    public const int MaxRecoveryCodeCount = 20;

    /// <summary>Minimum recovery code length in characters.</summary>
    public const int MinRecoveryCodeLength = 8;

    private const string KeyPrefix = MfaOptions.SectionKey;

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, MfaOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            errors.Add($"{KeyPrefix}:Issuer must be a non-empty, non-whitespace string.");
        }

        if (Array.IndexOf(AllowedDigits, options.Digits) < 0)
        {
            errors.Add($"{KeyPrefix}:Digits must be 6 or 8, but was {options.Digits}.");
        }

        if (options.TimeStepSeconds < MinTimeStepSeconds || options.TimeStepSeconds > MaxTimeStepSeconds)
        {
            errors.Add($"{KeyPrefix}:TimeStepSeconds must be between {MinTimeStepSeconds} and {MaxTimeStepSeconds} seconds (inclusive), but was {options.TimeStepSeconds}.");
        }

        if (options.AllowedDriftSteps < MinAllowedDriftSteps || options.AllowedDriftSteps > MaxAllowedDriftSteps)
        {
            errors.Add($"{KeyPrefix}:AllowedDriftSteps must be between {MinAllowedDriftSteps} and {MaxAllowedDriftSteps} (inclusive), but was {options.AllowedDriftSteps}.");
        }

        if (options.RecoveryCodeCount < MinRecoveryCodeCount || options.RecoveryCodeCount > MaxRecoveryCodeCount)
        {
            errors.Add($"{KeyPrefix}:RecoveryCodeCount must be between {MinRecoveryCodeCount} and {MaxRecoveryCodeCount} (inclusive), but was {options.RecoveryCodeCount}.");
        }

        if (options.RecoveryCodeLength < MinRecoveryCodeLength)
        {
            errors.Add($"{KeyPrefix}:RecoveryCodeLength must be at least {MinRecoveryCodeLength} characters, but was {options.RecoveryCodeLength}.");
        }

        return errors.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(errors);
    }
}
