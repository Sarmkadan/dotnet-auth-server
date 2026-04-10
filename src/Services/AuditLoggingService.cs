// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Services;

using System.Collections.Concurrent;
using DotnetAuthServer.Middleware;

/// <summary>
/// Service for audit logging of security-relevant events.
/// Records authentication attempts, authorization decisions, token issuance, etc.
/// Critical for compliance (GDPR, SOC 2, etc.) and security incident investigation.
/// </summary>
public class AuditLoggingService
{
    private readonly ILogger<AuditLoggingService> _logger;
    private readonly ConcurrentQueue<AuditLogEntry> _auditLog = new();
    private readonly int _maxLogEntries = 10000; // Prevent unbounded memory growth

    public AuditLoggingService(ILogger<AuditLoggingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Logs a successful token issuance event with all relevant details.
    /// </summary>
    public void LogTokenIssuance(
        string userId,
        string clientId,
        string grantType,
        string scopes,
        string? ipAddress = null)
    {
        LogAuditEvent(new AuditLogEntry
        {
            EventType = "TOKEN_ISSUED",
            UserId = userId,
            ClientId = clientId,
            Details = new Dictionary<string, string>
            {
                { "grant_type", grantType },
                { "scopes", scopes },
                { "ip_address", ipAddress ?? "unknown" }
            },
            Timestamp = DateTime.UtcNow,
            RequestId = LogicalContext.RequestId
        });
    }

    /// <summary>
    /// Logs a successful user authentication.
    /// </summary>
    public void LogAuthentication(
        string userId,
        string? username,
        string? ipAddress = null,
        bool success = true)
    {
        LogAuditEvent(new AuditLogEntry
        {
            EventType = success ? "AUTH_SUCCESS" : "AUTH_FAILURE",
            UserId = userId,
            Details = new Dictionary<string, string>
            {
                { "username", username ?? "unknown" },
                { "ip_address", ipAddress ?? "unknown" }
            },
            Timestamp = DateTime.UtcNow,
            RequestId = LogicalContext.RequestId
        });
    }

    /// <summary>
    /// Logs an authorization decision (grant or deny).
    /// </summary>
    public void LogAuthorizationDecision(
        string userId,
        string clientId,
        bool granted,
        string reason = "")
    {
        LogAuditEvent(new AuditLogEntry
        {
            EventType = granted ? "AUTHORIZATION_GRANTED" : "AUTHORIZATION_DENIED",
            UserId = userId,
            ClientId = clientId,
            Details = new Dictionary<string, string>
            {
                { "reason", reason }
            },
            Timestamp = DateTime.UtcNow,
            RequestId = LogicalContext.RequestId
        });
    }

    /// <summary>
    /// Logs suspicious activity (rate limit exceeded, invalid credentials repeated, etc.)
    /// </summary>
    public void LogSuspiciousActivity(
        string activityType,
        string? userId = null,
        string? clientId = null,
        string? ipAddress = null)
    {
        LogAuditEvent(new AuditLogEntry
        {
            EventType = "SUSPICIOUS_ACTIVITY",
            UserId = userId,
            ClientId = clientId,
            Details = new Dictionary<string, string>
            {
                { "activity_type", activityType },
                { "ip_address", ipAddress ?? "unknown" }
            },
            Timestamp = DateTime.UtcNow,
            RequestId = LogicalContext.RequestId,
            Severity = AuditSeverity.Warning
        });
    }

    /// <summary>
    /// Logs a configuration change or administrative action.
    /// </summary>
    public void LogAdministrativeAction(
        string action,
        string? targetClientId = null,
        string? targetUserId = null,
        Dictionary<string, string>? changes = null)
    {
        var details = changes ?? new Dictionary<string, string>();
        details["action"] = action;

        LogAuditEvent(new AuditLogEntry
        {
            EventType = "ADMIN_ACTION",
            ClientId = targetClientId,
            UserId = targetUserId,
            Details = details,
            Timestamp = DateTime.UtcNow,
            RequestId = LogicalContext.RequestId,
            Severity = AuditSeverity.Critical
        });
    }

    /// <summary>
    /// Retrieves recent audit log entries (limited to prevent memory issues).
    /// </summary>
    public IEnumerable<AuditLogEntry> GetRecentEntries(int count = 100)
    {
        return _auditLog.TakeLast(count);
    }

    /// <summary>
    /// Clears the in-memory audit log.
    /// In production, audit logs should be persisted to a database or external system.
    /// </summary>
    public void Clear()
    {
        while (_auditLog.Count > 0)
        {
            _auditLog.TryDequeue(out _);
        }
    }

    private void LogAuditEvent(AuditLogEntry entry)
    {
        _auditLog.Enqueue(entry);

        // Prevent unbounded memory growth
        if (_auditLog.Count > _maxLogEntries)
        {
            _auditLog.TryDequeue(out _);
        }

        var logLevel = entry.Severity switch
        {
            AuditSeverity.Critical => LogLevel.Critical,
            AuditSeverity.Warning => LogLevel.Warning,
            _ => LogLevel.Information
        };

        _logger.Log(
            logLevel,
            "AUDIT: {EventType} | User: {UserId} | Client: {ClientId} | RequestId: {RequestId}",
            entry.EventType,
            entry.UserId ?? "unknown",
            entry.ClientId ?? "unknown",
            entry.RequestId ?? "unknown");
    }
}

/// <summary>
/// Single audit log entry with timestamp and event details.
/// </summary>
public class AuditLogEntry
{
    public string EventType { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string? ClientId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string? RequestId { get; set; }
    public Dictionary<string, string> Details { get; set; } = new();
    public AuditSeverity Severity { get; set; } = AuditSeverity.Information;
}

/// <summary>
/// Severity level for audit events.
/// </summary>
public enum AuditSeverity
{
    Information = 0,
    Warning = 1,
    Critical = 2
}
