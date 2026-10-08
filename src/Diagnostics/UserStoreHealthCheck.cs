#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Diagnostics;

using DotnetAuthServer.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

/// <summary>
/// Readiness check that confirms user storage answers a bounded probe read.
/// Failures are reported without exception details so storage internals are not exposed over HTTP.
/// </summary>
public sealed class UserStoreHealthCheck : IHealthCheck
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(2);

    private readonly UserStore _userStore;

    /// <summary>
    /// Initializes a new instance of <see cref="UserStoreHealthCheck"/>.
    /// </summary>
    /// <param name="userStore">The user store to probe.</param>
    /// <exception cref="ArgumentNullException"><paramref name="userStore"/> is null.</exception>
    public UserStoreHealthCheck(UserStore userStore)
    {
        _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(ProbeTimeout);

        try
        {
            await _userStore.FindByIdAsync(UserStoreStartupCheck.ProbeUserId, timeoutSource.Token);
            return HealthCheckResult.Healthy("User store responded to probe.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy($"User store did not respond within {ProbeTimeout.TotalSeconds} seconds.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return HealthCheckResult.Unhealthy("User store probe failed.", exception);
        }
    }
}
