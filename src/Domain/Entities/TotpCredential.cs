#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Entities;

/// <summary>
/// Stores the TOTP (RFC 6238) secret and state for a single user's MFA enrollment.
/// The secret is a 20-byte random value encoded as Base32 (as expected by authenticator apps).
/// Each user may have at most one active TOTP credential.
/// </summary>
public sealed class TotpCredential
{
    /// <summary>Internal record identifier.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>User that owns this credential.</summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Base32-encoded TOTP shared secret (RFC 4648 alphabet, no padding).
    /// Store this value encrypted at rest in production.
    /// </summary>
    public string SecretKey { get; set; } = null!;

    /// <summary>
    /// Whether MFA has been confirmed and is actively enforced for this user.
    /// Setup is two-phase: the secret is generated first, then confirmed with a valid code.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>Timestamp when the credential record was first created (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Timestamp when MFA was confirmed and enabled (UTC).</summary>
    public DateTime? EnabledAt { get; set; }

    /// <summary>Timestamp of the last successful TOTP verification (UTC).</summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// One-time backup codes that can each be used once in place of a TOTP code.
    /// Stored as hashes in production; plain for this in-memory demo.
    /// </summary>
    public IList<string> BackupCodes { get; set; } = [];

    /// <summary>
    /// Marks the credential as enabled after a successful code verification.
    /// </summary>
    public void Enable()
    {
        IsEnabled = true;
        EnabledAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a successful TOTP verification.
    /// </summary>
    public void RecordVerification() => LastUsedAt = DateTime.UtcNow;
}
