#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Configuration;

/// <summary>
/// TOTP multi-factor authentication settings, bound from the <c>DotnetAuthServer:Mfa</c> section.
/// Validated at host start by <see cref="MfaOptionsValidator"/>; out-of-range values stop the host.
/// </summary>
public sealed class MfaOptions
{
    /// <summary>
    /// Configuration key prefix for this section, used in validation messages.
    /// </summary>
    public const string SectionKey = "DotnetAuthServer:Mfa";

    /// <summary>
    /// Issuer name shown in authenticator apps. Must be non-empty.
    /// </summary>
    public string Issuer { get; set; } = "DotnetAuthServer";

    /// <summary>
    /// Number of digits in a TOTP code. Must be 6 or 8.
    /// </summary>
    public int Digits { get; set; } = 6;

    /// <summary>
    /// Length of one TOTP time step in seconds. Must be between 15 and 120 (inclusive).
    /// </summary>
    public int TimeStepSeconds { get; set; } = 30;

    /// <summary>
    /// Number of time steps accepted on either side of the current step. Must be between 0 and 2 (inclusive).
    /// </summary>
    public int AllowedDriftSteps { get; set; } = 1;

    /// <summary>
    /// Number of single-use recovery codes issued at enrollment. Must be between 1 and 20 (inclusive).
    /// </summary>
    public int RecoveryCodeCount { get; set; } = 8;

    /// <summary>
    /// Length of each recovery code in characters. Must be at least 8.
    /// </summary>
    public int RecoveryCodeLength { get; set; } = 10;
}
