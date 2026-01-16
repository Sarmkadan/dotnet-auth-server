// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Entities;

/// <summary>
/// Represents a refresh token for obtaining new access tokens
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Unique refresh token identifier
    /// </summary>
    public string TokenId { get; set; } = null!;

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
        if (LastUsedAt == null) return false;

        var timeSinceLastUse = DateTime.UtcNow - LastUsedAt;
        return timeSinceLastUse < timeWindow && UsageCount > 1;
    }
}
