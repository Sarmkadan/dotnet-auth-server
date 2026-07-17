#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;

namespace DotnetAuthServer.Examples;

/// <summary>
/// Extension methods for <see cref="TokenRefreshRotationExample"/> providing additional functionality
/// for token refresh and rotation scenarios
/// </summary>
public static class TokenRefreshRotationExampleExtensions
{
    /// <summary>
    /// Validates that the token response contains all required fields
    /// </summary>
    /// <param name="example">The <see cref="TokenRefreshRotationExample"/> instance</param>
    /// <param name="tokenResponse">Token response to validate</param>
    /// <returns>True if token is valid; otherwise false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tokenResponse"/> is null</exception>
    public static bool ValidateTokenResponse(this TokenRefreshRotationExample example, TokenResponse? tokenResponse)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentNullException.ThrowIfNull(tokenResponse);

        return !string.IsNullOrEmpty(tokenResponse.AccessToken)
            && !string.IsNullOrEmpty(tokenResponse.RefreshToken)
            && tokenResponse.ExpiresIn > 0
            && !string.IsNullOrEmpty(tokenResponse.TokenType);
    }

    /// <summary>
    /// Gets the token expiration time based on current time and expires_in value
    /// </summary>
    /// <param name="example">The <see cref="TokenRefreshRotationExample"/> instance</param>
    /// <param name="tokenResponse">Token response containing expires_in</param>
    /// <returns>DateTime when token expires</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tokenResponse"/> is null</exception>
    public static DateTime GetTokenExpirationTime(this TokenRefreshRotationExample example, TokenResponse tokenResponse)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentNullException.ThrowIfNull(tokenResponse);

        return DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
    }

    /// <summary>
    /// Calculates time remaining until token expiration in seconds
    /// </summary>
    /// <param name="example">The <see cref="TokenRefreshRotationExample"/> instance</param>
    /// <param name="tokenResponse">Token response containing expires_in</param>
    /// <returns>Time remaining in seconds, or 0 if token is expired</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tokenResponse"/> is null</exception>
    public static double GetTokenTimeRemaining(this TokenRefreshRotationExample example, TokenResponse tokenResponse)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentNullException.ThrowIfNull(tokenResponse);

        var expiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        var timeRemaining = (expiresAt - DateTime.UtcNow).TotalSeconds;
        return Math.Max(0, timeRemaining);
    }

    /// <summary>
    /// Safely extracts the access token with null/empty checks
    /// </summary>
    /// <param name="example">The <see cref="TokenRefreshRotationExample"/> instance</param>
    /// <param name="tokenResponse">Token response to extract from</param>
    /// <returns>Access token if valid; otherwise null</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tokenResponse"/> is null</exception>
    public static string? SafeGetAccessToken(this TokenRefreshRotationExample example, TokenResponse? tokenResponse)
    {
        ArgumentNullException.ThrowIfNull(example);

        return tokenResponse?.AccessToken is { Length: > 0 }
            ? tokenResponse.AccessToken
            : null;
    }

    /// <summary>
    /// Safely extracts the refresh token with null/empty checks
    /// </summary>
    /// <param name="example">The <see cref="TokenRefreshRotationExample"/> instance</param>
    /// <param name="tokenResponse">Token response to extract from</param>
    /// <returns>Refresh token if valid; otherwise null</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tokenResponse"/> is null</exception>
    public static string? SafeGetRefreshToken(this TokenRefreshRotationExample example, TokenResponse? tokenResponse)
    {
        ArgumentNullException.ThrowIfNull(example);

        return tokenResponse?.RefreshToken is { Length: > 0 }
            ? tokenResponse.RefreshToken
            : null;
    }

    /// <summary>
    /// Creates a formatted string representation of the token response
    /// </summary>
    /// <param name="example">The <see cref="TokenRefreshRotationExample"/> instance</param>
    /// <param name="tokenResponse">Token response to format</param>
    /// <returns>Formatted string with token information</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tokenResponse"/> is null</exception>
    public static string FormatTokenInfo(this TokenRefreshRotationExample example, TokenResponse tokenResponse)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentNullException.ThrowIfNull(tokenResponse);

        var timeRemaining = example.GetTokenTimeRemaining(tokenResponse);
        var expiresAt = example.GetTokenExpirationTime(tokenResponse);

        return $""""
Token Information:
  Access Token: {tokenResponse.AccessToken[..Math.Min(20, tokenResponse.AccessToken.Length)]}...
  Refresh Token: {tokenResponse.RefreshToken[..Math.Min(20, tokenResponse.RefreshToken.Length)]}...
  Expires In: {tokenResponse.ExpiresIn} seconds
  Token Type: {tokenResponse.TokenType}
  Expires At: {expiresAt:yyyy-MM-dd HH:mm:ss} UTC
  Time Remaining: {timeRemaining:F0} seconds
""";
    }

    /// <summary>
    /// Creates a batch of token refresh operations for multiple refresh tokens
    /// </summary>
    /// <param name="example">The <see cref="TokenRefreshRotationExample"/> instance</param>
    /// <param name="refreshTokens">Collection of refresh tokens to refresh</param>
    /// <returns>Collection of refresh results with success/failure information</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="example"/> or <paramref name="refreshTokens"/> is null
    /// </exception>
    public static async IAsyncEnumerable<TokenRefreshResult> BatchRefreshTokensAsync(
        this TokenRefreshRotationExample example,
        IEnumerable<string> refreshTokens)
    {
        ArgumentNullException.ThrowIfNull(example);
        ArgumentNullException.ThrowIfNull(refreshTokens);

        foreach (var refreshToken in refreshTokens)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                yield return new TokenRefreshResult
                {
                    RefreshToken = refreshToken,
                    Success = false,
                    Error = "Refresh token is null or empty"
                };
                continue;
            }

            var result = await example.RefreshTokenAsync(refreshToken);

            yield return new TokenRefreshResult
            {
                RefreshToken = refreshToken,
                Success = result is not null,
                NewToken = result,
                Error = result is null ? "Refresh failed" : null
            };
        }
    }

    /// <summary>
    /// Gets token statistics from a collection of token responses
    /// </summary>
    /// <param name="example">The <see cref="TokenRefreshRotationExample"/> instance</param>
    /// <param name="tokenResponses">Collection of token responses</param>
    /// <returns>Token statistics including average expiration, total count, etc.</returns>
/// <exception cref="ArgumentNullException">Thrown when <paramref name="example"/> or <paramref name="tokenResponses"/> is null</exception>
    public static TokenStatistics GetTokenStatistics(
        this TokenRefreshRotationExample example,
        IEnumerable<TokenResponse> tokenResponses)

    private static TokenStatistics CalculateTokenStatistics(IEnumerable<TokenResponse> tokenResponses)
    {
        var tokens = tokenResponses.ToList();
        var validTokens = tokens.Where(t => t.ExpiresIn > 0).ToList();

        return validTokens.Count == 0
            ? new TokenStatistics
            {
                TotalTokens = tokens.Count,
                ValidTokens = 0,
                AverageExpiresIn = 0,
                MinExpiresIn = 0,
                MaxExpiresIn = 0,
                TotalValidDuration = 0
            }
            : new TokenStatistics
            {
                TotalTokens = tokens.Count,
                ValidTokens = validTokens.Count,
                AverageExpiresIn = (int)validTokens.Average(t => t.ExpiresIn),
                MinExpiresIn = validTokens.Min(t => t.ExpiresIn),
                MaxExpiresIn = validTokens.Max(t => t.ExpiresIn),
                TotalValidDuration = (int)validTokens.Sum(t => t.ExpiresIn)
            };
    }
}

/// <summary>
/// Result of a token refresh operation
/// </summary>
public sealed class TokenRefreshResult
{
    /// <summary>Original refresh token used for this operation</summary>
    public required string RefreshToken { get; init; }

    /// <summary>Indicates whether the refresh operation succeeded</summary>
    public required bool Success { get; init; }

    /// <summary>New token response if refresh succeeded; otherwise null</summary>
    public TokenResponse? NewToken { get; init; }

    /// <summary>Error message if refresh failed; otherwise null</summary>
    public string? Error { get; init; }
}

/// <summary>
/// Statistics about a collection of tokens
/// </summary>
public sealed class TokenStatistics
{
    /// <summary>Total number of tokens in the collection</summary>
    public required int TotalTokens { get; init; }

    /// <summary>Number of tokens with valid expiration values</summary>
    public required int ValidTokens { get; init; }

    /// <summary>Average expires_in value in seconds</summary>
    public required int AverageExpiresIn { get; init; }

    /// <summary>Minimum expires_in value in seconds</summary>
    public required int MinExpiresIn { get; init; }

    /// <summary>Maximum expires_in value in seconds</summary>
    public required int MaxExpiresIn { get; init; }

    /// <summary>Total valid duration in seconds (sum of all valid expires_in values)</summary>
    public required int TotalValidDuration { get; init; }
}