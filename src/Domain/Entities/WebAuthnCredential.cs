// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Entities;

/// <summary>
/// Represents a stored WebAuthn/FIDO2 public-key credential associated with a user account,
/// enabling passwordless and phishing-resistant authentication via platform authenticators
/// (e.g., Touch ID, Windows Hello) or roaming hardware security keys (e.g., YubiKey).
/// </summary>
public class WebAuthnCredential
{
    /// <summary>Gets or sets the internal unique identifier for this credential record.</summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets or sets the credential ID issued by the authenticator, stored as a Base64URL-encoded string.
    /// This identifier is presented by the client during each authentication ceremony.
    /// </summary>
    public string CredentialId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the packed public key bytes extracted from the attestation response.
    /// For ES256, contains the P-256 x and y coordinates; for RS256, the modulus and exponent.
    /// See <see cref="Algorithm"/> to determine the encoding.
    /// </summary>
    public byte[] PublicKey { get; set; } = [];

    /// <summary>
    /// Gets or sets the COSE algorithm identifier for the public key.
    /// Common values: <c>-7</c> (ES256/P-256), <c>-257</c> (RS256).
    /// </summary>
    public int Algorithm { get; set; }

    /// <summary>
    /// Gets or sets the signature counter value from the most recent successful authentication.
    /// A received counter value lower than the stored value signals a potentially cloned authenticator.
    /// </summary>
    public uint SignatureCounter { get; set; }

    /// <summary>Gets or sets the subject identifier (user ID) that owns this credential.</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a user-facing label for the credential (e.g., "MacBook Touch ID" or "YubiKey 5C").
    /// </summary>
    public string FriendlyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the AAGUID of the authenticator model as reported during attestation,
    /// formatted as a hyphenated GUID string (e.g., "adce0002-35bc-c60a-648b-0b25f1f05503").
    /// </summary>
    public string AaGuid { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this credential is eligible for cloud backup (the BE flag in authenticatorData).
    /// When <see langword="true"/>, the credential may be a synced passkey.
    /// </summary>
    public bool BackupEligible { get; set; }

    /// <summary>
    /// Gets or sets whether the credential is currently backed up to a cloud service
    /// (the BS flag in authenticatorData). Meaningful only when <see cref="BackupEligible"/> is <see langword="true"/>.
    /// </summary>
    public bool BackedUp { get; set; }

    /// <summary>Gets or sets the UTC timestamp at which this credential was registered.</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Gets or sets the UTC timestamp of the last successful authentication using this credential.</summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Gets or sets whether this credential is currently active.
    /// Setting to <see langword="false"/> disables the credential without deleting the record.
    /// </summary>
    public bool IsActive { get; set; } = true;
}
