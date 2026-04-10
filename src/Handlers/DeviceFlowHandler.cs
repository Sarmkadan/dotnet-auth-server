// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Handlers;

using System.Collections.Concurrent;

/// <summary>
/// Handler for OAuth2 Device Flow (RFC 8628).
/// Enables authorization for devices with limited input capabilities (smart TVs, printers, etc.)
/// or devices that lack a suitable browser.
/// </summary>
public class DeviceFlowHandler
{
    private readonly ConcurrentDictionary<string, DeviceFlowSession> _sessions = new();
    private readonly ILogger<DeviceFlowHandler> _logger;

    private const int DeviceCodeLength = 8;
    private const int UserCodeLength = 6;
    private const int ExpirationSeconds = 600; // 10 minutes

    public DeviceFlowHandler(ILogger<DeviceFlowHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initiates a device flow authorization request.
    /// Returns device code and user code to display to user.
    /// </summary>
    public DeviceFlowInitiation InitiateFlow(
        string clientId,
        string? scope = null)
    {
        var deviceCode = GenerateCode(DeviceCodeLength);
        var userCode = GenerateCode(UserCodeLength);

        var session = new DeviceFlowSession
        {
            DeviceCode = deviceCode,
            UserCode = userCode,
            ClientId = clientId,
            Scope = scope,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(ExpirationSeconds),
            Status = DeviceFlowStatus.Pending
        };

        _sessions.TryAdd(deviceCode, session);

        _logger.LogInformation(
            "Device flow initiated: device_code={DeviceCode} user_code={UserCode} client={ClientId}",
            deviceCode,
            userCode,
            clientId);

        return new DeviceFlowInitiation
        {
            DeviceCode = deviceCode,
            UserCode = userCode,
            ExpiresIn = ExpirationSeconds,
            Interval = 5 // Poll interval in seconds
        };
    }

    /// <summary>
    /// Completes a device flow by authorizing it after user verification.
    /// </summary>
    public bool ApproveDeviceFlow(string userCode, string userId)
    {
        var session = _sessions.Values.FirstOrDefault(s => s.UserCode == userCode);
        if (session == null)
        {
            _logger.LogWarning("Device flow approval failed: user code not found");
            return false;
        }

        if (session.Status != DeviceFlowStatus.Pending)
        {
            _logger.LogWarning("Cannot approve device flow with status: {Status}", session.Status);
            return false;
        }

        session.Status = DeviceFlowStatus.Approved;
        session.UserId = userId;
        session.ApprovedAt = DateTime.UtcNow;

        _logger.LogInformation(
            "Device flow approved: user_code={UserCode} user={UserId}",
            userCode,
            userId);

        return true;
    }

    /// <summary>
    /// Denies a device flow authorization.
    /// </summary>
    public bool DenyDeviceFlow(string userCode)
    {
        var session = _sessions.Values.FirstOrDefault(s => s.UserCode == userCode);
        if (session == null)
            return false;

        session.Status = DeviceFlowStatus.Denied;

        _logger.LogInformation("Device flow denied: user_code={UserCode}", userCode);

        return true;
    }

    /// <summary>
    /// Polls the status of a device flow authorization.
    /// Used by the device to check if user has authorized it.
    /// </summary>
    public DeviceFlowPollResult PollDeviceFlow(string deviceCode)
    {
        if (!_sessions.TryGetValue(deviceCode, out var session))
        {
            return new DeviceFlowPollResult
            {
                Status = DeviceFlowStatus.Unknown,
                Error = "invalid_device_code"
            };
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            _sessions.TryRemove(deviceCode, out _);
            return new DeviceFlowPollResult
            {
                Status = DeviceFlowStatus.Expired,
                Error = "expired_token"
            };
        }

        return new DeviceFlowPollResult
        {
            Status = session.Status,
            UserId = session.UserId,
            Scope = session.Scope
        };
    }

    /// <summary>
    /// Removes a completed device flow session.
    /// </summary>
    public void CompleteDeviceFlow(string deviceCode)
    {
        _sessions.TryRemove(deviceCode, out _);
    }

    private static string GenerateCode(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, length)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }
}

/// <summary>
/// Device flow initiation response.
/// </summary>
public class DeviceFlowInitiation
{
    public string DeviceCode { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public string VerificationUri { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public int Interval { get; set; }
}

/// <summary>
/// Result of polling a device flow.
/// </summary>
public class DeviceFlowPollResult
{
    public DeviceFlowStatus Status { get; set; }
    public string? UserId { get; set; }
    public string? Scope { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Device flow authorization session.
/// </summary>
public class DeviceFlowSession
{
    public string DeviceCode { get; set; } = string.Empty;
    public string UserCode { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string? Scope { get; set; }
    public string? UserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DeviceFlowStatus Status { get; set; }
}

/// <summary>
/// Status of a device flow authorization.
/// </summary>
public enum DeviceFlowStatus
{
    Unknown = 0,
    Pending = 1,
    Approved = 2,
    Denied = 3,
    Expired = 4
}
