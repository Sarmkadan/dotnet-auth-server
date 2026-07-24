#nullable enable
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
public sealed class DeviceFlowHandler
{
    private readonly ConcurrentDictionary<string, DeviceFlowSession> _sessions = new();
    private readonly ILogger<DeviceFlowHandler> _logger;

    private const int DeviceCodeLength = 16;

    /// <summary>
    /// Minimum length of generated user codes. RFC 8628 recommends enough entropy
    /// to resist guessing; 8 characters from a base-20 alphabet yields ~34.6 bits.
    /// </summary>
    private const int UserCodeLength = 8;

    private const int ExpirationSeconds = 600; // 10 minutes

    /// <summary>
    /// Minimum interval, in seconds, the client must wait between polls. Polling
    /// faster than this results in a <c>slow_down</c> error per RFC 8628 section 3.5.
    /// </summary>
    private const int MinPollIntervalSeconds = 5;

    /// <summary>
    /// Unambiguous base-20 alphabet used for user codes: excludes visually confusable
    /// characters (0/O, 1/I, and other easily-mistaken letters) so codes are easy to
    /// transcribe by hand.
    /// </summary>
    private const string UserCodeAlphabet = "ABCDEFGHJKLMNPQRTUVW";

    public DeviceFlowHandler(ILogger<DeviceFlowHandler> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        _logger = logger;
    }

    /// <summary>
    /// Initiates a device flow authorization request.
    /// Returns device code and user code to display to user.
    /// </summary>
    /// <param name="clientId">Identifier of the client initiating the flow.</param>
    /// <param name="scope">Optional space-delimited list of requested scopes.</param>
    /// <exception cref="ArgumentException"><paramref name="clientId"/> is null or empty.</exception>
    public DeviceFlowInitiation InitiateFlow(
        string clientId,
        string? scope = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(clientId);

        var deviceCode = GenerateDeviceCode();
        var userCode = GenerateUserCode();

        var session = new DeviceFlowSession
        {
            DeviceCode = deviceCode,
            UserCode = userCode,
            ClientId = clientId,
            Scope = scope,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(ExpirationSeconds),
            Status = DeviceFlowStatus.Pending,
            LastPolledAt = null
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
            Interval = MinPollIntervalSeconds
        };
    }

    /// <summary>
    /// Completes a device flow by authorizing it after user verification.
    /// </summary>
    /// <param name="userCode">The user-facing code the user entered.</param>
    /// <param name="userId">Identifier of the user granting authorization.</param>
    /// <exception cref="ArgumentException"><paramref name="userCode"/> or <paramref name="userId"/> is null or empty.</exception>
    public bool ApproveDeviceFlow(string userCode, string userId)
    {
        ArgumentException.ThrowIfNullOrEmpty(userCode);
        ArgumentException.ThrowIfNullOrEmpty(userId);

        var session = _sessions.Values.FirstOrDefault(s => s.UserCode == userCode);
        if (session is null)
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
    /// <param name="userCode">The user-facing code the user entered.</param>
    /// <exception cref="ArgumentException"><paramref name="userCode"/> is null or empty.</exception>
    public bool DenyDeviceFlow(string userCode)
    {
        ArgumentException.ThrowIfNullOrEmpty(userCode);

        var session = _sessions.Values.FirstOrDefault(s => s.UserCode == userCode);
        if (session is null)
            return false;

        session.Status = DeviceFlowStatus.Denied;

        _logger.LogInformation("Device flow denied: user_code={UserCode}", userCode);

        return true;
    }

    /// <summary>
    /// Polls the status of a device flow authorization.
    /// Used by the device to check if user has authorized it.
    /// </summary>
    /// <remarks>
    /// When the session has been approved, this call issues the result and atomically
    /// removes the session so the device code cannot be redeemed a second time by a
    /// concurrent poll (RFC 8628 single-use device code semantics).
    /// </remarks>
    /// <param name="deviceCode">The device code returned by <see cref="InitiateFlow"/>.</param>
    /// <exception cref="ArgumentException"><paramref name="deviceCode"/> is null or empty.</exception>
    public DeviceFlowPollResult PollDeviceFlow(string deviceCode)
    {
        ArgumentException.ThrowIfNullOrEmpty(deviceCode);

        if (!_sessions.TryGetValue(deviceCode, out var session))
        {
            return new DeviceFlowPollResult
            {
                Status = DeviceFlowStatus.Unknown,
                Error = "invalid_device_code"
            };
        }

        var now = DateTime.UtcNow;

        if (session.ExpiresAt < now)
        {
            _sessions.TryRemove(deviceCode, out _);
            return new DeviceFlowPollResult
            {
                Status = DeviceFlowStatus.Expired,
                Error = "expired_token"
            };
        }

        var previousPoll = Interlocked.Exchange(ref session.LastPolledAtTicks, now.Ticks);
        if (previousPoll != 0)
        {
            var elapsed = now - new DateTime(previousPoll, DateTimeKind.Utc);
            if (elapsed.TotalSeconds < MinPollIntervalSeconds)
            {
                return new DeviceFlowPollResult
                {
                    Status = session.Status,
                    Error = "slow_down"
                };
            }
        }

        if (session.Status == DeviceFlowStatus.Approved)
        {
            // Single-use redemption: only the poll that wins this race removes the
            // session and returns the tokens; any concurrent poll sees it as gone.
            if (!_sessions.TryRemove(deviceCode, out var removedSession))
            {
                return new DeviceFlowPollResult
                {
                    Status = DeviceFlowStatus.Unknown,
                    Error = "invalid_device_code"
                };
            }

            _logger.LogInformation("Device flow redeemed: device_code={DeviceCode}", deviceCode);

            return new DeviceFlowPollResult
            {
                Status = removedSession.Status,
                UserId = removedSession.UserId,
                Scope = removedSession.Scope
            };
        }

        if (session.Status is DeviceFlowStatus.Denied)
        {
            _sessions.TryRemove(deviceCode, out _);
        }

        return new DeviceFlowPollResult
        {
            Status = session.Status,
            UserId = session.UserId,
            Scope = session.Scope
        };
    }

    /// <summary>
    /// Removes a device flow session, e.g. after a client abandons the flow.
    /// </summary>
    /// <param name="deviceCode">The device code identifying the session to remove.</param>
    /// <exception cref="ArgumentException"><paramref name="deviceCode"/> is null or empty.</exception>
    public void CompleteDeviceFlow(string deviceCode)
    {
        ArgumentException.ThrowIfNullOrEmpty(deviceCode);
        _sessions.TryRemove(deviceCode, out _);
    }

    /// <summary>
    /// Generates a cryptographically random device code using an unreserved URL-safe
    /// alphabet, opaque to the end user.
    /// </summary>
    private static string GenerateDeviceCode() =>
        GenerateCode(DeviceCodeLength, "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789");

    /// <summary>
    /// Generates a user-facing code from the unambiguous alphabet, sized for manual
    /// transcription and sufficient entropy against brute-force guessing.
    /// </summary>
    private static string GenerateUserCode() =>
        GenerateCode(UserCodeLength, UserCodeAlphabet);

    private static string GenerateCode(int length, string alphabet)
    {
        Span<char> buffer = length <= 64 ? stackalloc char[length] : new char[length];
        for (var i = 0; i < length; i++)
        {
            buffer[i] = alphabet[System.Security.Cryptography.RandomNumberGenerator.GetInt32(alphabet.Length)];
        }

        return new string(buffer);
    }
}

/// <summary>
/// Device flow initiation response.
/// </summary>
public sealed class DeviceFlowInitiation
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
public sealed class DeviceFlowPollResult
{
    public DeviceFlowStatus Status { get; set; }
    public string? UserId { get; set; }
    public string? Scope { get; set; }
    public string? Error { get; set; }
}

/// <summary>
/// Device flow authorization session.
/// </summary>
public sealed class DeviceFlowSession
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

    /// <summary>
    /// Timestamp of the last poll for this session, as UTC ticks, used to enforce the
    /// minimum polling interval. Stored as ticks (rather than <see cref="DateTime"/>) so
    /// it can be updated atomically via <see cref="Interlocked.Exchange(ref long, long)"/>.
    /// </summary>
    public long LastPolledAtTicks;

    /// <summary>
    /// Convenience accessor for the last poll time, or <see langword="null"/> if the
    /// session has not yet been polled.
    /// </summary>
    public DateTime? LastPolledAt
    {
        get => LastPolledAtTicks == 0 ? null : new DateTime(LastPolledAtTicks, DateTimeKind.Utc);
        set => LastPolledAtTicks = value?.Ticks ?? 0;
    }
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
