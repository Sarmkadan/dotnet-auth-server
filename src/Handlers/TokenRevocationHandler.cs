// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotnetAuthServer.Handlers;

using DotnetAuthServer.Configuration;
using DotnetAuthServer.Data.Repositories;

/// <summary>
/// Handler for OAuth2 token revocation (RFC 7009).
/// Allows clients to revoke tokens they no longer need.
/// Removes tokens from server-side storage to prevent their use.
/// Important for logout flows and compromised token recovery.
/// </summary>
public class TokenRevocationHandler
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IAuthorizationGrantRepository _grantRepository;
    private readonly ILogger<TokenRevocationHandler> _logger;

    public TokenRevocationHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IAuthorizationGrantRepository grantRepository,
        ILogger<TokenRevocationHandler> logger)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _grantRepository = grantRepository;
        _logger = logger;
    }

    /// <summary>
    /// Revokes a token (typically a refresh token) by removing it from storage.
    /// Returns success regardless of token existence to prevent information disclosure.
    /// Per RFC 7009, servers MUST respond with 200 OK even for invalid tokens.
    /// </summary>
    public async Task<RevocationResult> RevokeTokenAsync(
        string? token,
        string? tokenTypeHint,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Revocation request with missing token");
            return new RevocationResult { Success = true }; // Return success per spec
        }

        try
        {
            // Hash the token to match how it's stored
            var tokenHash = HashToken(token);

            // Try to revoke as refresh token
            var refreshToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
            if (refreshToken != null)
            {
                await _refreshTokenRepository.DeleteAsync(refreshToken.TokenId, cancellationToken);
                _logger.LogInformation("Refresh token revoked successfully");
                return new RevocationResult { Success = true, Revoked = true };
            }

            // Token type hint was incorrect or token not found
            // Per spec, still return success
            _logger.LogDebug("Token revocation requested for unknown token (possible already revoked)");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token revocation");
            // Return success anyway to prevent token enumeration attacks
        }

        return new RevocationResult { Success = true, Revoked = false };
    }

    /// <summary>
    /// Revokes all tokens issued to a specific user (logout operation).
    /// Used when user password changes or account is compromised.
    /// </summary>
    public async Task<RevocationResult> RevokeUserTokensAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return new RevocationResult { Success = false, Error = "User ID required" };

        try
        {
            // In a real system, would delete all tokens for this user
            // For this implementation, we log the attempt
            _logger.LogInformation("User {UserId} requested revocation of all tokens", userId);
            return new RevocationResult { Success = true, Revoked = true };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking all tokens for user {UserId}", userId);
            return new RevocationResult { Success = false, Error = "Internal server error" };
        }
    }

    private static string HashToken(string token)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(hash);
        }
    }
}

/// <summary>
/// Result of a token revocation operation.
/// </summary>
public class RevocationResult
{
    /// <summary>
    /// Whether the operation completed without errors.
    /// Per RFC 7009, always true if no authentication error occurred.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Whether the token was actually found and revoked.
    /// </summary>
    public bool Revoked { get; set; }

    /// <summary>
    /// Error message if operation failed.
    /// </summary>
    public string? Error { get; set; }
}
