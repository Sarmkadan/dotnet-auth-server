#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Entities;

using System.Diagnostics;
using DotnetAuthServer.Diagnostics;

/// <summary>
/// Represents a refresh token for obtaining new access tokens
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
public sealed class RefreshToken
{
    /// <summary>
    /// Revocation reason recorded on the token that a rotation replaces. Presenting it again is reuse.
    /// </summary>
    public const string RotationRevocationReason = "Refresh token rotation";

    /// <summary>
    /// Unique refresh token identifier
    /// </summary>
    public string TokenId { get; set; } = null!;

    /// <summary>
    /// Identifies the rotation chain this token belongs to. Inherited by every token rotated from it,
    /// so all tokens issued from one authorization share a family.
    /// </summary>
    public string FamilyId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The refresh token value (hashed)
    /// </summary>
    public string TokenHash { get; set; } = null!;

    /// <summary>
    /// Client ID that owns this refresh token
    /// </summary>
    public string ClientId { get; set; } = null!;

    /// <summary>
    /// User ID associated with this refresh token
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Scopes granted with this refresh token
    /// </summary>
    public string GrantedScopes { get; set; } = null!;

    /// <summary>
    /// Current refresh token version (for rotation tracking)
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// The previous refresh token hash (for rotation chain)
    /// </summary>
    public string? PreviousTokenHash { get; set; }

    /// <summary>
    /// Refresh token expiration timestamp
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Whether this refresh token is revoked
    /// </summary>
    public bool IsRevoked { get; set; }

    /// <summary>
    /// Timestamp when the token was revoked (null if not revoked)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// Reason for revocation
    /// </summary>
    public string? RevocationReason { get; set; }

    /// <summary>
    /// Number of times this refresh token has been used
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Timestamp of last usage
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// IP address or device identifier for audit trail
    /// </summary>
    public string? IssuedToDeviceId { get; set; }

    /// <summary>
    /// Token creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Checks if the refresh token is valid and can be used
    /// </summary>
    public bool IsValid()
    {
        return !IsRevoked && DateTime.UtcNow < ExpiresAt;
    }

    /// <summary>
    /// Checks if the token has expired
    /// </summary>
    public bool IsExpired()
    {
        return DateTime.UtcNow >= ExpiresAt;
    }

    /// <summary>
    /// Records usage of this refresh token
    /// </summary>
    public void RecordUsage()
    {
        if (IsRevoked)
            throw new InvalidOperationException("Cannot use a revoked refresh token");

        if (IsExpired())
            throw new InvalidOperationException("Refresh token has expired");

        UsageCount++;
        LastUsedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Revokes the refresh token
    /// </summary>
    public void Revoke(string? reason = null)
    {
        IsRevoked = true;
        RevokedAt = DateTime.UtcNow;
        RevocationReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates the replacement entity for a rotation. The replacement keeps this token's family.
    /// </summary>
    /// <param name="tokenId">Identifier of the replacement token.</param>
    /// <param name="tokenHash">Hash of the replacement token value.</param>
    /// <param name="expiresAt">Expiry of the replacement token.</param>
    public RefreshToken CreateRotatedToken(string tokenId, string tokenHash, DateTime expiresAt)
    {
        return new RefreshToken
        {
            TokenId = tokenId,
            TokenHash = tokenHash,
            ClientId = ClientId,
            UserId = UserId,
            GrantedScopes = GrantedScopes,
            Version = Version + 1,
            PreviousTokenHash = TokenHash,
            FamilyId = FamilyId,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt
        };
    }

    /// <summary>
    /// Rotates the refresh token (creates a new version)
    /// </summary>
    public void Rotate()
    {
        PreviousTokenHash = TokenHash;
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if this token potentially indicates a replay attack
    /// based on multiple uses within a short timeframe
    /// </summary>
    public bool SuspiciousUsagePattern(TimeSpan timeWindow)
    {
        if (LastUsedAt is null) return false;

        var timeSinceLastUse = DateTime.UtcNow - LastUsedAt;
        return timeSinceLastUse < timeWindow && UsageCount > 1;
    }

    /// <summary>
    /// Returns a diagnostic representation: identifiers, expiry and revocation state.
    /// The token hash and scopes are deliberately omitted.
    /// </summary>
    public override string ToString() =>
        $"RefreshToken {{ TokenId = {TokenId}, FamilyId = {FamilyId}, UserId = {UserId}, ExpiresAt = {ExpiresAt:O}, IsRevoked = {IsRevoked} }}";
}
