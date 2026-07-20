#nullable enable

namespace DotnetAuthServer.Services;

/// <summary>
/// Service for audit logging of security-relevant events.
/// Records authentication attempts, authorization decisions, token issuance, etc.
/// Critical for compliance (GDPR, SOC 2, etc.) and security incident investigation.
/// </summary>
public interface IAuditLoggingService
{
    /// <summary>
    /// Logs a successful token issuance event with all relevant details.
    /// </summary>
    void LogTokenIssuance(
        string userId,
        string clientId,
        string grantType,
        string scopes,
        string? ipAddress = null);

    /// <summary>
    /// Logs a successful user authentication.
    /// </summary>
    void LogAuthentication(
        string userId,
        string? username,
        string? ipAddress = null,
        bool success = true);

    /// <summary>
    /// Logs an authorization decision (grant or deny).
    /// </summary>
    void LogAuthorizationDecision(
        string userId,
        string clientId,
        bool granted,
        string reason = "");

    /// <summary>
    /// Logs suspicious activity (rate limit exceeded, invalid credentials repeated, etc.)
    /// </summary>
    void LogSuspiciousActivity(
        string activityType,
        string? userId = null,
        string? clientId = null,
        string? ipAddress = null);

    /// <summary>
    /// Logs a configuration change or administrative action.
    /// </summary>
    void LogAdministrativeAction(
        string action,
        string? targetClientId = null,
        string? targetUserId = null,
        Dictionary<string, string>? changes = null);

    /// <summary>
    /// Retrieves recent audit log entries (limited to prevent memory issues).
    /// </summary>
    IEnumerable<AuditLogEntry> GetRecentEntries(int count = 100);

    /// <summary>
    /// Filters audit log entries by event type and time range.
    /// Returns entries matching the specified event type within the time range,
    /// ordered by timestamp (newest first), limited by max count.
    /// </summary>
    /// <param name="eventType">The event type to filter by (e.g., "TOKEN_ISSUED", "AUTH_SUCCESS")</param>
    /// <param name="startTime">The start of the time range (inclusive)</param>
    /// <param name="endTime">The end of the time range (inclusive)</param>
    /// <param name="maxCount">Maximum number of entries to return</param>
    /// <returns>Filtered and ordered audit log entries</returns>
    IEnumerable<AuditLogEntry> GetEntriesByEventTypeAndTimeRange(
        string eventType,
        DateTime startTime,
        DateTime endTime,
        int maxCount = 100);

    /// <summary>
    /// Clears the in-memory audit log.
    /// In production, audit logs should be persisted to a database or external system.
    /// </summary>
    void Clear();
}