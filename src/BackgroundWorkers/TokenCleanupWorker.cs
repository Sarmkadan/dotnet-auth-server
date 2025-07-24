#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.BackgroundWorkers;

using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Security;

/// <summary>
/// Background worker that periodically removes expired tokens from storage.
/// Prevents unlimited growth of the database with stale token records.
/// Typically runs once per hour but can be configured via dependency injection.
/// </summary>
public sealed class TokenCleanupWorker : BackgroundService
{
    private readonly ILogger<TokenCleanupWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly RevokedTokenStore _revokedTokenStore;
    private readonly TimeSpan _cleanupInterval;

    public TokenCleanupWorker(
        ILogger<TokenCleanupWorker> logger,
        IServiceProvider serviceProvider,
        RevokedTokenStore revokedTokenStore)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _revokedTokenStore = revokedTokenStore;
        _cleanupInterval = TimeSpan.FromHours(1); // Run cleanup hourly
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Token cleanup worker started");

        // Initial delay to allow server startup
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredTokensAsync(stoppingToken);
                await CleanupExpiredGrantsAsync(stoppingToken);
                _revokedTokenStore.PurgeExpired();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token cleanup");
            }

            try
            {
                await Task.Delay(_cleanupInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Server is shutting down
                break;
            }
        }

        _logger.LogInformation("Token cleanup worker stopped");
    }

    private async Task CleanupExpiredTokensAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var refreshTokenRepo = scope.ServiceProvider.GetRequiredService<IRefreshTokenRepository>();

            _logger.LogInformation("Starting cleanup of expired refresh tokens");

            await refreshTokenRepo.DeleteExpiredAsync(cancellationToken);

            _logger.LogInformation("Completed cleanup of expired refresh tokens");
        }
    }

    private async Task CleanupExpiredGrantsAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var grantRepo = scope.ServiceProvider.GetRequiredService<IAuthorizationGrantRepository>();

            _logger.LogInformation("Starting cleanup of expired authorization grants");

            await grantRepo.DeleteExpiredAsync(cancellationToken);

            _logger.LogInformation("Completed cleanup of expired authorization grants");
        }
    }
}
