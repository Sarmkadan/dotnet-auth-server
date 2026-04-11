// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Data.Repositories;

using DotnetAuthServer.Domain.Entities;

/// <summary>
/// Repository interface for managing user consent records.
/// Tracks which clients users have authorized and what scopes they've granted.
/// </summary>
public interface IConsentRepository : IRepository<Consent>
{
    /// <summary>
    /// Gets all consents for a specific user.
    /// </summary>
    Task<IEnumerable<Consent>> GetByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets consent for a specific user and client combination.
    /// </summary>
    Task<Consent?> GetByUserAndClientAsync(
        string userId,
        string clientId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets consents for a specific client from any user.
    /// </summary>
    Task<IEnumerable<Consent>> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all consents for a user.
    /// Used during account deletion or user-initiated logout from all devices.
    /// </summary>
    Task<int> RevokeUserConsentsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a specific consent relationship.
    /// </summary>
    Task<bool> RevokeConsentAsync(
        string userId,
        string clientId,
        CancellationToken cancellationToken = default);
}
