// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DotnetAuthServer.Examples;

/// <summary>
/// Machine-to-Machine (M2M) Client Credentials Flow Example
/// Used for service-to-service authentication without user involvement
/// </summary>
public class ClientCredentialsFlowExample
{
    private readonly HttpClient _httpClient;
    private readonly string _authServerUrl;
    private const string ServiceId = "data-processor-service";
    private const string ServiceSecret = "super-secret-key-store-in-vault";

    public ClientCredentialsFlowExample(string authServerUrl)
    {
        _authServerUrl = authServerUrl;
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Request access token using client credentials (no user needed)
    /// </summary>
    public async Task<TokenResponse?> GetServiceAccessTokenAsync(string scope = "api:read api:write")
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_authServerUrl}/oauth/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "grant_type", "client_credentials" },
                    { "client_id", ServiceId },
                    { "client_secret", ServiceSecret },
                    { "scope", scope }
                })
            };

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Token request failed: {response.StatusCode}");
                Console.WriteLine($"Error: {errorContent}");
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return tokenResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error requesting token: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Use access token to call downstream API
    /// </summary>
    public async Task CallDownstreamApiAsync(string accessToken, string apiUrl)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            request.Headers.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine("API Response:");
                Console.WriteLine(content);
            }
            else
            {
                Console.WriteLine($"API call failed: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error calling API: {ex.Message}");
        }
    }

    /// <summary>
    /// Validate token is still active via introspection endpoint
    /// </summary>
    public async Task<bool> ValidateTokenAsync(string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_authServerUrl}/oauth/token/introspect")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "token", accessToken },
                    { "token_type_hint", "access_token" }
                })
            };

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                return false;

            var json = await response.Content.ReadAsStringAsync();
            var introspectResponse = JsonSerializer.Deserialize<IntrospectResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return introspectResponse?.Active ?? false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error validating token: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Revoke token when no longer needed
    /// </summary>
    public async Task RevokeTokenAsync(string accessToken)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{_authServerUrl}/oauth/token/revoke")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    { "token", accessToken },
                    { "token_type_hint", "access_token" }
                })
            };

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
                Console.WriteLine("Token revoked successfully");
            else
                Console.WriteLine($"Token revocation failed: {response.StatusCode}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error revoking token: {ex.Message}");
        }
    }
}

/// <summary>
/// Example: Background job that processes data using service credentials
/// </summary>
public class BackgroundDataProcessorService
{
    private readonly ClientCredentialsFlowExample _authFlow;
    private string? _currentAccessToken;
    private DateTime _tokenExpiresAt;

    public BackgroundDataProcessorService(string authServerUrl)
    {
        _authFlow = new ClientCredentialsFlowExample(authServerUrl);
    }

    /// <summary>
    /// Execute background job with automatic token refresh
    /// </summary>
    public async Task ExecuteJobAsync(string jobId)
    {
        try
        {
            Console.WriteLine($"Starting job: {jobId}");

            // Get fresh token if needed
            if (string.IsNullOrEmpty(_currentAccessToken) || DateTime.UtcNow >= _tokenExpiresAt)
            {
                await RefreshAccessTokenAsync();
            }

            // Process data
            await ProcessDataAsync(_currentAccessToken);

            Console.WriteLine($"Job {jobId} completed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Job {jobId} failed: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Get service token (with caching)
    /// </summary>
    private async Task RefreshAccessTokenAsync()
    {
        var tokenResponse = await _authFlow.GetServiceAccessTokenAsync("api:read api:write");

        if (tokenResponse == null)
            throw new InvalidOperationException("Failed to obtain access token");

        _currentAccessToken = tokenResponse.AccessToken;
        _tokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        Console.WriteLine($"Token obtained, expires in {tokenResponse.ExpiresIn} seconds");
    }

    /// <summary>
    /// Simulate processing data
    /// </summary>
    private async Task ProcessDataAsync(string? accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
            throw new InvalidOperationException("No access token");

        // Validate token before use
        var isValid = await _authFlow.ValidateTokenAsync(accessToken);
        if (!isValid)
        {
            Console.WriteLine("Token is invalid, obtaining new one...");
            await RefreshAccessTokenAsync();
            return;
        }

        // Call downstream API
        await _authFlow.CallDownstreamApiAsync(accessToken,
            "https://api.example.com/data/process");

        // Simulate processing time
        await Task.Delay(1000);

        Console.WriteLine("Data processed");
    }

    /// <summary>
    /// Cleanup - revoke token on shutdown
    /// </summary>
    public async Task ShutdownAsync()
    {
        if (!string.IsNullOrEmpty(_currentAccessToken))
        {
            await _authFlow.RevokeTokenAsync(_currentAccessToken);
            _currentAccessToken = null;
        }
    }
}

/// <summary>
/// Example: Multiple services with different permission scopes
/// </summary>
public class MultiServiceAuthenticationExample
{
    private readonly string _authServerUrl;

    public MultiServiceAuthenticationExample(string authServerUrl)
    {
        _authServerUrl = authServerUrl;
    }

    /// <summary>
    /// Different services request different scopes
    /// </summary>
    public async Task DemonstrateMultiServiceAsync()
    {
        // Log reading service (read-only)
        var logReaderFlow = new ClientCredentialsFlowExample(_authServerUrl);
        var logReaderToken = await logReaderFlow.GetServiceAccessTokenAsync("logs:read");

        if (logReaderToken != null)
        {
            Console.WriteLine($"Log Reader Token: {logReaderToken.AccessToken[..50]}...");
            Console.WriteLine($"Scopes: {logReaderToken.Scope}");
            Console.WriteLine($"Expires in: {logReaderToken.ExpiresIn} seconds");
        }

        // Data writer service (write access)
        var dataWriterFlow = new ClientCredentialsFlowExample(_authServerUrl);
        var dataWriterToken = await dataWriterFlow.GetServiceAccessTokenAsync(
            "api:read api:write database:write");

        if (dataWriterToken != null)
        {
            Console.WriteLine($"\nData Writer Token: {dataWriterToken.AccessToken[..50]}...");
            Console.WriteLine($"Scopes: {dataWriterToken.Scope}");
            Console.WriteLine($"Expires in: {dataWriterToken.ExpiresIn} seconds");
        }

        // Admin service (full access)
        var adminFlow = new ClientCredentialsFlowExample(_authServerUrl);
        var adminToken = await adminFlow.GetServiceAccessTokenAsync("admin:*");

        if (adminToken != null)
        {
            Console.WriteLine($"\nAdmin Token: {adminToken.AccessToken[..50]}...");
            Console.WriteLine($"Scopes: {adminToken.Scope}");
            Console.WriteLine($"Expires in: {adminToken.ExpiresIn} seconds");
        }
    }
}

/// <summary>
/// DTO for token response
/// </summary>
public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;
}

/// <summary>
/// DTO for token introspection response
/// </summary>
public class IntrospectResponse
{
    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    [JsonPropertyName("sub")]
    public string? Subject { get; set; }

    [JsonPropertyName("exp")]
    public long? ExpiresAt { get; set; }
}

/// <summary>
/// Main example execution
/// </summary>
internal class Program
{
    static async Task Main(string[] args)
    {
        const string authServerUrl = "https://localhost:7001";

        Console.WriteLine("=== Client Credentials Flow Example ===\n");

        // Example 1: Simple token request
        Console.WriteLine("1. Request access token:");
        var flow = new ClientCredentialsFlowExample(authServerUrl);
        var token = await flow.GetServiceAccessTokenAsync();

        if (token != null)
        {
            Console.WriteLine($"✓ Access Token (first 50 chars): {token.AccessToken[..50]}...");
            Console.WriteLine($"✓ Token Type: {token.TokenType}");
            Console.WriteLine($"✓ Expires In: {token.ExpiresIn} seconds");
            Console.WriteLine($"✓ Scope: {token.Scope}\n");

            // Example 2: Validate token
            Console.WriteLine("2. Validate token:");
            var isValid = await flow.ValidateTokenAsync(token.AccessToken);
            Console.WriteLine($"✓ Token is valid: {isValid}\n");

            // Example 3: Revoke token
            Console.WriteLine("3. Revoke token:");
            await flow.RevokeTokenAsync(token.AccessToken);
            Console.WriteLine();
        }

        // Example 4: Background job with token refresh
        Console.WriteLine("4. Background job with automatic token refresh:");
        var processor = new BackgroundDataProcessorService(authServerUrl);
        try
        {
            await processor.ExecuteJobAsync("job-001");
        }
        finally
        {
            await processor.ShutdownAsync();
        }

        // Example 5: Multiple services with different scopes
        Console.WriteLine("\n5. Multiple services with different scopes:");
        var multiService = new MultiServiceAuthenticationExample(authServerUrl);
        await multiService.DemonstrateMultiServiceAsync();
    }
}
