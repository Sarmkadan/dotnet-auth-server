// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DotnetAuthServer.Examples;

/// <summary>
/// Refresh Token Rotation Example
/// Demonstrates the security benefit of automatic token rotation:
/// - Old token is invalidated on each refresh
/// - Limits damage from token leaks
/// - Detects token reuse attacks
/// </summary>
public class TokenRefreshRotationExample
{
    private readonly HttpClient _httpClient;
    private readonly string _authServerUrl;
    private const string ClientId = "mobile-app";
    private const string RedirectUri = "myapp://callback";

    public TokenRefreshRotationExample(string authServerUrl)
    {
        _authServerUrl = authServerUrl;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Refresh access token using refresh token
    /// Note: Old refresh token is automatically invalidated by server (rotation)
    /// </summary>
    public async Task<TokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_authServerUrl}/oauth/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "refresh_token" },
                    { "client_id", ClientId },
                    { "refresh_token", refreshToken }
                })
            };

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Token refresh failed: {error}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<TokenResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing token: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Example: Demonstrate token rotation security
    /// Shows what happens when you try to reuse an old refresh token
    /// </summary>
    public async Task DemonstrateTokenRotationAsync(string initialRefreshToken)
    {
        Console.WriteLine("=== Token Rotation Demonstration ===\n");

        var currentRefreshToken = initialRefreshToken;
        var refreshCount = 0;

        Console.WriteLine($"Initial Refresh Token: {currentRefreshToken[..30]}...\n");

        // Refresh token 3 times
        for (int i = 1; i <= 3; i++)
        {
            Console.WriteLine($"--- Refresh #{i} ---");

            var newToken = await RefreshTokenAsync(currentRefreshToken);

            if (newToken == null)
            {
                Console.WriteLine("✗ Token refresh failed\n");
                break;
            }

            Console.WriteLine($"✓ New Access Token: {newToken.AccessToken[..30]}...");
            Console.WriteLine($"✓ New Refresh Token: {newToken.RefreshToken[..30]}...");
            Console.WriteLine($"✓ Old Refresh Token: INVALIDATED (rotated)\n");

            // Old refresh token is now invalid - attempting to use it would fail
            currentRefreshToken = newToken.RefreshToken;
            refreshCount++;
        }

        Console.WriteLine($"Total successful refreshes: {refreshCount}\n");
    }

    /// <summary>
    /// Example: Refresh token with automatic expiration handling
    /// </summary>
    public async Task<TokenResponse?> RefreshWithExpirationAsync(string refreshToken,
        int expiresInSeconds)
    {
        var expirationTime = DateTime.UtcNow.AddSeconds(expiresInSeconds);

        while (DateTime.UtcNow < expirationTime.AddSeconds(-300)) // Refresh 5 min before expiry
        {
            // Wait until near expiration
            var timeUntilRefresh = expirationTime.AddSeconds(-300) - DateTime.UtcNow;

            if (timeUntilRefresh.TotalSeconds > 0)
            {
                Console.WriteLine($"Token expires in {(expirationTime - DateTime.UtcNow).TotalSeconds:F0} seconds");
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            // Refresh token
            var newToken = await RefreshTokenAsync(refreshToken);

            if (newToken == null)
                return null;

            refreshToken = newToken.RefreshToken;
            expirationTime = DateTime.UtcNow.AddSeconds(newToken.ExpiresIn);

            Console.WriteLine($"✓ Token refreshed at {DateTime.Now:HH:mm:ss}");
        }

        return new TokenResponse
        {
            RefreshToken = refreshToken,
            ExpiresIn = (int)(expirationTime - DateTime.UtcNow).TotalSeconds
        };
    }
}

/// <summary>
/// Example: Token lifecycle management in mobile app
/// </summary>
public class MobileAppTokenManager
{
    private TokenResponse? _currentToken;
    private readonly string _authServerUrl;
    private DateTime _tokenRefreshTime;
    private readonly SemaphoreSlim _tokenLock = new SemaphoreSlim(1, 1);

    public MobileAppTokenManager(string authServerUrl)
    {
        _authServerUrl = authServerUrl;
    }

    /// <summary>
    /// Set initial token after authorization
    /// </summary>
    public void SetToken(TokenResponse token)
    {
        _currentToken = token;
        _tokenRefreshTime = DateTime.UtcNow.AddSeconds(token.ExpiresIn - 300);
        Console.WriteLine($"Token set, will refresh at {_tokenRefreshTime:HH:mm:ss}");
    }

    /// <summary>
    /// Get valid access token, refreshing if necessary
    /// </summary>
    public async Task<string?> GetValidAccessTokenAsync()
    {
        // Check if refresh needed
        if (DateTime.UtcNow >= _tokenRefreshTime && _currentToken != null)
        {
            await _tokenLock.WaitAsync();
            try
            {
                // Double-check after acquiring lock
                if (DateTime.UtcNow >= _tokenRefreshTime)
                {
                    await RefreshTokenInternalAsync();
                }
            }
            finally
            {
                _tokenLock.Release();
            }
        }

        return _currentToken?.AccessToken;
    }

    /// <summary>
    /// Internal token refresh logic
    /// </summary>
    private async Task RefreshTokenInternalAsync()
    {
        if (_currentToken?.RefreshToken == null)
            return;

        try
        {
            var flow = new TokenRefreshRotationExample(_authServerUrl);
            var newToken = await flow.RefreshTokenAsync(_currentToken.RefreshToken);

            if (newToken != null)
            {
                _currentToken = newToken;
                _tokenRefreshTime = DateTime.UtcNow.AddSeconds(newToken.ExpiresIn - 300);
                Console.WriteLine($"✓ Token refreshed at {DateTime.Now:HH:mm:ss}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Token refresh failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Make API call with automatic token refresh
    /// </summary>
    public async Task<bool> CallApiAsync(string apiUrl)
    {
        var token = await GetValidAccessTokenAsync();

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("No valid token available");
            return false;
        }

        using var client = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
        request.Headers.Add("Authorization", $"Bearer {token}");

        try
        {
            var response = await client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"API call failed: {ex.Message}");
            return false;
        }
    }
}

/// <summary>
/// Example: Handling token refresh failures and fallback
/// </summary>
public class ResilientTokenRefreshExample
{
    private readonly string _authServerUrl;
    private int _refreshAttempts;
    private const int MaxRefreshAttempts = 3;

    public ResilientTokenRefreshExample(string authServerUrl)
    {
        _authServerUrl = authServerUrl;
    }

    /// <summary>
    /// Refresh with exponential backoff on failure
    /// </summary>
    public async Task<TokenResponse?> RefreshWithRetryAsync(string refreshToken)
    {
        _refreshAttempts = 0;

        while (_refreshAttempts < MaxRefreshAttempts)
        {
            _refreshAttempts++;

            try
            {
                var flow = new TokenRefreshRotationExample(_authServerUrl);
                var result = await flow.RefreshTokenAsync(refreshToken);

                if (result != null)
                {
                    Console.WriteLine($"✓ Token refreshed on attempt {_refreshAttempts}");
                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Attempt {_refreshAttempts} failed: {ex.Message}");
            }

            if (_refreshAttempts < MaxRefreshAttempts)
            {
                // Exponential backoff: 2^attempt seconds
                var delayMs = (int)Math.Pow(2, _refreshAttempts) * 1000;
                Console.WriteLine($"Retrying in {delayMs / 1000} seconds...");
                await Task.Delay(delayMs);
            }
        }

        Console.WriteLine($"✗ Token refresh failed after {MaxRefreshAttempts} attempts");
        return null;
    }

    /// <summary>
    /// Handle different refresh failure scenarios
    /// </summary>
    public async Task<TokenResponse?> RefreshWithFallbackAsync(
        string refreshToken,
        Func<Task<string?>> fallbackAuthAsync)
    {
        var flow = new TokenRefreshRotationExample(_authServerUrl);
        var newToken = await flow.RefreshTokenAsync(refreshToken);

        if (newToken != null)
        {
            Console.WriteLine("✓ Token refreshed successfully");
            return newToken;
        }

        Console.WriteLine("✗ Refresh failed, attempting re-authentication...");

        // Fallback: Use fallback authentication method
        var authCode = await fallbackAuthAsync();

        if (!string.IsNullOrEmpty(authCode))
        {
            Console.WriteLine("✓ Re-authentication successful");
            // Convert auth code to token (not shown here)
            return new TokenResponse { RefreshToken = refreshToken };
        }

        Console.WriteLine("✗ Re-authentication failed");
        return null;
    }
}

/// <summary>
/// DTO for token response
/// </summary>
public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = "Bearer";
}

/// <summary>
/// Main example execution
/// </summary>
internal class Program
{
    static async Task Main(string[] args)
    {
        const string authServerUrl = "https://localhost:7001";

        Console.WriteLine("=== Token Refresh & Rotation Example ===\n");

        // Example 1: Token rotation demonstration
        Console.WriteLine("Example 1: Token Rotation Security\n");
        var rotation = new TokenRefreshRotationExample(authServerUrl);
        // Note: Replace with actual refresh token from authorization flow
        var initialRefreshToken = "refresh_token_placeholder";

        // Example 2: Mobile app token manager
        Console.WriteLine("\nExample 2: Mobile App Token Lifecycle\n");
        var tokenManager = new MobileAppTokenManager(authServerUrl);

        // Simulate setting initial token
        tokenManager.SetToken(new TokenResponse
        {
            AccessToken = "initial_access_token",
            RefreshToken = "initial_refresh_token",
            ExpiresIn = 3600
        });

        // Make API calls (token auto-refreshes if needed)
        Console.WriteLine("Making API call...");
        await tokenManager.CallApiAsync("https://api.example.com/user/profile");

        // Example 3: Resilient token refresh
        Console.WriteLine("\nExample 3: Resilient Token Refresh with Retry\n");
        var resilient = new ResilientTokenRefreshExample(authServerUrl);
        var result = await resilient.RefreshWithRetryAsync(initialRefreshToken);

        if (result != null)
        {
            Console.WriteLine($"New token obtained: {result.AccessToken[..30]}...");
        }

        Console.WriteLine("\n✓ Examples completed");
    }
}
