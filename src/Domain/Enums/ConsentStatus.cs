// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Domain.Enums;

/// <summary>
/// User consent status for scope permissions
/// </summary>
public enum ConsentStatus
{
    /// <summary>
    /// User has not yet provided consent
    /// </summary>
    Pending,

    /// <summary>
    /// User has explicitly granted consent
    /// </summary>
    Approved,

    /// <summary>
    /// User has denied consent
    /// </summary>
    Rejected,

    /// <summary>
    /// Consent has expired
    /// </summary>
    Expired
}
