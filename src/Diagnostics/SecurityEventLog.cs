#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Diagnostics;

using Microsoft.Extensions.Logging;

/// <summary>
/// Source-generated structured log events for security-relevant state changes.
/// Event IDs: 4001-4099 refresh-token reuse, 4101-4199 account lockout.
/// Token values are never logged; only identifiers are.
/// </summary>
internal static partial class SecurityEventLog
{
    /// <summary>
    /// A rotated (already-revoked) refresh token was presented again. Treated as possible token theft.
    /// </summary>
    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Warning,
        Message = "Refresh token reuse detected: rotated token {TokenId} presented for user {UserId}, client {ClientId}, family {FamilyId}")]
    public static partial void RefreshTokenReuseDetected(
        ILogger logger,
        string tokenId,
        string userId,
        string clientId,
        string familyId);

    /// <summary>
    /// A refresh token that was revoked for a reason other than rotation (e.g. logout, password change) was presented.
    /// </summary>
    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Warning,
        Message = "Revoked refresh token {TokenId} presented for user {UserId}, client {ClientId}, family {FamilyId}; revocation reason: {RevocationReason}")]
    public static partial void RevokedRefreshTokenPresented(
        ILogger logger,
        string tokenId,
        string userId,
        string clientId,
        string familyId,
        string revocationReason);

    /// <summary>
    /// A failed login reached the threshold and the account was locked.
    /// </summary>
    [LoggerMessage(
        EventId = 4101,
        Level = LogLevel.Warning,
        Message = "Account {UserId} locked until {LockedUntil:O} after {FailedLoginAttempts} failed login attempts")]
    public static partial void AccountLockedOut(
        ILogger logger,
        string userId,
        DateTime lockedUntil,
        int failedLoginAttempts);

    /// <summary>
    /// A login was rejected because the account is currently locked.
    /// </summary>
    [LoggerMessage(
        EventId = 4102,
        Level = LogLevel.Warning,
        Message = "Login rejected for locked account {UserId}, locked until {LockedUntil:O}")]
    public static partial void LoginRejectedForLockedAccount(
        ILogger logger,
        string userId,
        DateTime lockedUntil);

    /// <summary>
    /// An administrator locked an account for a fixed duration.
    /// </summary>
    [LoggerMessage(
        EventId = 4103,
        Level = LogLevel.Warning,
        Message = "Admin locked account {UserId} until {LockedUntil:O}")]
    public static partial void AccountLockedByAdmin(
        ILogger logger,
        string userId,
        DateTime lockedUntil);

    /// <summary>
    /// An administrator cleared an account lockout.
    /// </summary>
    [LoggerMessage(
        EventId = 4104,
        Level = LogLevel.Information,
        Message = "Admin unlocked account {UserId}")]
    public static partial void AccountUnlockedByAdmin(
        ILogger logger,
        string userId);
}
