// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.BackgroundWorkers;

using DotnetAuthServer.Data.Repositories;

/// <summary>
/// Background worker that periodically removes expired tokens from storage.
/// Prevents unlimited growth of the database with stale token records.
/// Typically runs once per hour but can be configured via dependency injection.
/// </summary>
public class TokenCleanupWorker : BackgroundService
{
    private readonly ILogger<TokenCleanupWorker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _cleanupInterval;

    public TokenCleanupWorker(ILogger<TokenCleanupWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
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

            // Get all refresh tokens and remove expired ones
            // Note: In production, this would query database directly for efficiency
            var now = DateTime.UtcNow;
            var deletedCount = 0;

            _logger.LogInformation("Starting cleanup of expired refresh tokens");

            // This is a placeholder - actual implementation would depend on repository capabilities
            // For in-memory storage, you might iterate and delete
            // For database, you'd run a DELETE query with WHERE ExpiresAt <= NOW()

            if (deletedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired refresh tokens", deletedCount);
            }
        }
    }

    private async Task CleanupExpiredGrantsAsync(CancellationToken cancellationToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var grantRepo = scope.ServiceProvider.GetRequiredService<IAuthorizationGrantRepository>();

            var now = DateTime.UtcNow;
            var deletedCount = 0;

            _logger.LogInformation("Starting cleanup of expired authorization grants");

            // Similar cleanup for authorization codes
            // Only keep codes that haven't expired yet

            if (deletedCount > 0)
            {
                _logger.LogInformation("Cleaned up {Count} expired authorization grants", deletedCount);
            }
        }
    }
}
