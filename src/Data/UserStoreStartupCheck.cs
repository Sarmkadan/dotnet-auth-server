#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Data;

using Microsoft.Extensions.Hosting;

/// <summary>
/// Hosted service that proves user storage is usable before the server accepts traffic.
/// Resolving <see cref="UserStore"/> runs its configuration validation, and a bounded probe
/// read against the repository confirms the storage backend responds. Any failure aborts host
/// startup with a message that names the configuration keys to check.
/// </summary>
public sealed class UserStoreStartupCheck : IHostedService
{
    internal const string ProbeUserId = "__startup_probe__";
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(5);

    private readonly UserStore _userStore;
    private readonly ILogger<UserStoreStartupCheck> _logger;

    /// <summary>
    /// Initializes a new instance of <see cref="UserStoreStartupCheck"/>.
    /// </summary>
    /// <param name="userStore">The user store to validate and probe.</param>
    /// <param name="logger">Logger instance.</param>
    /// <exception cref="ArgumentNullException">Any argument is null.</exception>
    public UserStoreStartupCheck(UserStore userStore, ILogger<UserStoreStartupCheck> logger)
    {
        _userStore = userStore ?? throw new ArgumentNullException(nameof(userStore));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Probes user storage and throws if it is not reachable within the timeout.
    /// </summary>
    /// <param name="cancellationToken">Token signalling host shutdown.</param>
    /// <exception cref="InvalidOperationException">The storage backend failed or did not respond in time.</exception>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(ProbeTimeout);

        try
        {
            await _userStore.FindByIdAsync(ProbeUserId, timeoutSource.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new InvalidOperationException(
                $"User storage did not respond within {ProbeTimeout.TotalSeconds} seconds at startup. " +
                "Check DotnetAuthServer:AuthServer:UseInMemoryDatabase and DotnetAuthServer:AuthServer:DatabaseConnectionString.");
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                "User storage failed its startup reachability check. " +
                "Check DotnetAuthServer:AuthServer:UseInMemoryDatabase and DotnetAuthServer:AuthServer:DatabaseConnectionString.",
                exception);
        }

        _logger.LogInformation("User storage passed startup reachability check");
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
