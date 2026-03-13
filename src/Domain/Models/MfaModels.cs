#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Models;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Returned when TOTP enrollment is initiated, containing the information needed
/// for the user to configure an authenticator app.
/// </summary>
public sealed class MfaSetupResponse
{
    /// <summary>
    /// Base32-encoded TOTP secret that must be entered into the authenticator app.
    /// Display this only once; do not store it in the browser.
    /// </summary>
    public string SecretKey { get; set; } = null!;

    /// <summary>
    /// <c>otpauth://</c> URI suitable for rendering as a QR code.
    /// Most authenticator apps can scan this URI directly.
    /// </summary>
    public string ProvisioningUri { get; set; } = null!;

    /// <summary>
    /// Eight single-use backup codes the user can store offline.
    /// Each code is usable exactly once if the authenticator device is unavailable.
    /// </summary>
    public IList<string> BackupCodes { get; set; } = [];
}

/// <summary>
/// Request model for verifying a TOTP or backup code.
/// </summary>
public sealed class MfaVerifyRequest
{
    /// <summary>
    /// Six-digit TOTP code from the authenticator app,
    /// or an 8-character backup code (alphanumeric).
    /// </summary>
    [Required]
    public string Code { get; set; } = null!;
}

/// <summary>
/// Read-only MFA status for a user account.
/// </summary>
public sealed class MfaStatusResponse
{
    /// <summary>Whether TOTP MFA has been enrolled and confirmed.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Timestamp when MFA was first enabled (UTC), or null if never enabled.</summary>
    public DateTime? EnabledAt { get; set; }

    /// <summary>Timestamp of the most recent successful TOTP verification (UTC).</summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>Number of unused backup codes remaining.</summary>
    public int BackupCodesRemaining { get; set; }
}
