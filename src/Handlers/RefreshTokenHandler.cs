#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Handlers;

using System.Security.Cryptography;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Diagnostics;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Exceptions;
using Microsoft.Extensions.Options;

/// <summary>
/// Handles refresh token lifecycle: issuance, validation, rotation and revocation.
/// Implements token rotation with reuse detection to mitigate token theft scenarios.
/// </summary>
public sealed class RefreshTokenHandler
{
    private readonly IRefreshTokenRepository _tokenRepository;
    private readonly ILogger<RefreshTokenHandler> _logger;
    private readonly AuthServerOptions _options;

    /// <summary>
    /// Initializes a new instance of <see cref="RefreshTokenHandler"/>.
    /// </summary>
    /// <param name="tokenRepository">Repository for refresh token persistence.</param>
    /// <param name="options">Server configuration options.</param>
    /// <param name="logger">Logger instance.</param>
    public RefreshTokenHandler(
        IRefreshTokenRepository tokenRepository,
        IOptions<AuthServerOptions> options,
        ILogger<RefreshTokenHandler> logger)
    {
        _tokenRepository = tokenRepository ?? throw new ArgumentNullException(nameof(tokenRepository));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Issues a new refresh token for the given user and client, persisting it in the store.
    /// </summary>
    /// <param name="userId">The subject identifier.</param>
    /// <param name="clientId">The OAuth2 client identifier.</param>
    /// <param name="grantedScopes">Space-separated scope string.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The raw (unhashed) refresh token value for delivery to the client.</returns>
    public async Task<string> IssueAsync(
        string userId,
        string clientId,
        string grantedScopes,
        CancellationToken cancellationToken = default)
    {
        var rawToken = GenerateTokenValue();
        var tokenHash = HashToken(rawToken);

        var entity = new RefreshToken
        {
            TokenId = Guid.NewGuid().ToString(),
            TokenHash = tokenHash,
            ClientId = clientId,
            UserId = userId,
            GrantedScopes = grantedScopes,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddSeconds(_options.RefreshTokenLifetimeSeconds)
        };

        await _tokenRepository.CreateAsync(entity, cancellationToken);
        _logger.LogInformation("Issued refresh token {TokenId} for user {UserId}, client {ClientId}",
            entity.TokenId, userId, clientId);

        return rawToken;
    }

    /// <summary>
    /// Validates a refresh token and returns its metadata. Throws if expired, revoked or unknown.
    /// </summary>
    /// <param name="rawToken">The raw token value presented by the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The stored <see cref="RefreshToken"/> entity.</returns>
    public async Task<RefreshToken> ValidateAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        var tokenHash = HashToken(rawToken);
        var stored = await _tokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);

        if (stored is null)
        {
            _logger.LogWarning("Refresh token not found (possible reuse of rotated token)");
            throw new InvalidGrantException("invalid_grant", "Refresh token is invalid or has been revoked.");
        }

        if (stored.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning("Expired refresh token {TokenId} presented", stored.TokenId);
            throw new InvalidGrantException("invalid_grant", "Refresh token has expired.");
        }

        if (stored.IsRevoked)
        {
            SecurityEventLog.RefreshTokenReuseDetected(
                _logger, stored.TokenId, stored.UserId, stored.ClientId, stored.FamilyId);
            await RevokeChainAsync(stored.UserId, stored.ClientId, cancellationToken);
            throw new InvalidGrantException("invalid_grant", "Refresh token has been revoked. All tokens for this grant have been invalidated.");
        }

        return stored;
    }

    /// <summary>
    /// Rotates a refresh token: revokes the current one and issues a replacement,
    /// preserving the rotation chain for reuse detection.
    /// </summary>
    /// <param name="currentRawToken">The current raw token value being exchanged.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The new raw (unhashed) refresh token value.</returns>
    public async Task<string> RotateAsync(string currentRawToken, CancellationToken cancellationToken = default)
    {
        var current = await ValidateAsync(currentRawToken, cancellationToken);

        current.Revoke(RefreshToken.RotationRevocationReason);
        await _tokenRepository.UpdateAsync(current, cancellationToken);

        var newRawToken = GenerateTokenValue();
        var newHash = HashToken(newRawToken);

        var replacement = current.CreateRotatedToken(
            Guid.NewGuid().ToString(),
            newHash,
            DateTime.UtcNow.AddSeconds(_options.RefreshTokenLifetimeSeconds));

        await _tokenRepository.CreateAsync(replacement, cancellationToken);
        _logger.LogInformation("Rotated refresh token {OldId} -> {NewId} (v{Version})",
            current.TokenId, replacement.TokenId, replacement.Version);

        return newRawToken;
    }

    /// <summary>
    /// Revokes all refresh tokens for a given user and client combination.
    /// Used when token reuse is detected to invalidate the entire grant chain.
    /// </summary>
    /// <param name="userId">The subject identifier.</param>
    /// <param name="clientId">The OAuth2 client identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task RevokeChainAsync(string userId, string clientId, CancellationToken cancellationToken = default)
    {
        var userTokens = await _tokenRepository.GetByUserIdAsync(userId, cancellationToken);
        var clientTokens = userTokens.Where(t => t.ClientId == clientId && !t.IsRevoked);
        var count = 0;

        foreach (var token in clientTokens)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
            await _tokenRepository.UpdateAsync(token, cancellationToken);
            count++;
        }

        _logger.LogWarning("Revoked {Count} refresh tokens for user {UserId}, client {ClientId} (chain revocation)",
            count, userId, clientId);
    }

    private static string GenerateTokenValue()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static string HashToken(string rawToken)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }
}
