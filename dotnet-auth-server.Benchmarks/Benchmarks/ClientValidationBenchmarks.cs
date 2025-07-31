using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using DotnetAuthServer.Services;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Caching;
using Moq;

namespace DotnetAuthServer.Benchmarks;

/// <summary>
/// Benchmark suite for client validation performance testing.
/// Measures the efficiency of validating confidential and public OAuth clients
/// against different client types and grant flows.
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ClientValidationBenchmarks
{
    private AuthServerOptions _options;
    private ClientValidationService _clientValidationService;
    private Mock<IClientRepository> _clientRepositoryMock;
    private Client _confidentialClient;
    private Client _publicClient;

    /// <summary>
    /// Initializes the benchmark environment with test clients and mock repository.
    /// Sets up confidential and public client configurations with appropriate secrets,
    /// grant types, and scopes for validation testing.
    /// </summary>
    [GlobalSetup]
    public void Setup()
    {
        _options = new AuthServerOptions
        {
            JwtSigningKey = "super-secret-key-that-is-at-least-32-characters-long!",
            IssuerUrl = "https://auth.example.com"
        };

        _clientRepositoryMock = new Mock<IClientRepository>();

        _clientValidationService = new ClientValidationService(
            _clientRepositoryMock.Object,
            new NullCacheService(),
            NullLogger<ClientValidationService>.Instance
        );

        // Setup confidential client (with secret)
        _confidentialClient = new Client
        {
            ClientId = "confidential-client",
            IsActive = true,
            IsConfidential = true,
            ClientSecretHash = "hashed-secret",
            AllowedGrantTypes = new List<string> { "client_credentials", "authorization_code" },
            AllowedScopes = new List<string> { "openid", "profile", "api:read" },
            RedirectUris = new List<string> { "https://client.example.com/callback" }
        };

        // Setup public client (no secret, PKCE required)
        _publicClient = new Client
        {
            ClientId = "public-client",
            IsActive = true,
            IsConfidential = false,
            ClientSecretHash = null,
            AllowedGrantTypes = new List<string> { "authorization_code" },
            AllowedScopes = new List<string> { "openid", "profile" },
            RequirePkce = true,
            RedirectUris = new List<string> { "https://spa.example.com/callback" }
        };

        _clientRepositoryMock.Setup(x => x.GetActiveClientAsync("confidential-client", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_confidentialClient);
        _clientRepositoryMock.Setup(x => x.GetByIdAsync("confidential-client", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_confidentialClient);

        _clientRepositoryMock.Setup(x => x.GetActiveClientAsync("public-client", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_publicClient);
        _clientRepositoryMock.Setup(x => x.GetByIdAsync("public-client", It.IsAny<CancellationToken>()))
            .ReturnsAsync(_publicClient);
    }

    /// <summary>
    /// Benchmarks validation of confidential clients with client credentials grant type.
    /// Tests the performance of validating a confidential client that has a secret
    /// against the client_credentials grant flow.
    /// </summary>
    /// <returns>True if the client is valid and active; otherwise false.</returns>
    [Benchmark]
    public async Task<bool> ValidateConfidentialClient()
    {
        var client = await _clientValidationService.ValidateClientAsync("confidential-client", "secret", "client_credentials");
        return client != null && client.IsActive;
    }

    /// <summary>
    /// Benchmarks validation of public clients with authorization code grant type.
    /// Tests the performance of validating a public client (without secret) that requires
    /// PKCE against the authorization_code grant flow.
    /// </summary>
    /// <returns>True if the client is valid and active; otherwise false.</returns>
    [Benchmark]
    public async Task<bool> ValidatePublicClient()
    {
        var client = await _clientValidationService.ValidateClientAsync("public-client", null, "authorization_code");
        return client != null && client.IsActive;
    }

    /// <summary>
    /// Benchmarks validation of inactive clients.
    /// Tests the performance of handling validation requests for clients that are marked as inactive,
    /// ensuring they are properly rejected during the validation process.
    /// </summary>
    /// <returns>True if the inactive client is correctly rejected (returns null); otherwise false.</returns>
    [Benchmark]
    public async Task<bool> ValidateInactiveClient()
    {
        _clientRepositoryMock.Setup(x => x.GetActiveClientAsync("inactive-client", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client)null);

        var client = await _clientValidationService.ValidateClientAsync("inactive-client", "secret", "client_credentials");
        return client == null;
    }
}

/// <summary>
/// Null implementation of ICacheService for benchmarking purposes.
/// Provides no-op implementations of all cache operations to eliminate caching overhead
/// from client validation benchmarks and measure pure validation performance.
/// </summary>
public class NullCacheService : ICacheService
{
    /// <summary>
    /// Retrieves a cached value by key.
    /// In this null implementation, always returns null to avoid caching overhead in benchmarks.
    /// </summary>
    /// <typeparam name="T">The type of value to retrieve.</typeparam>
    /// <param name="key">The cache key to look up.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the cached value or null.</returns>
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        return Task.FromResult<T?>(null);
    }

    /// <summary>
    /// Stores a value in cache with optional expiration.
    /// In this null implementation, performs no operation and returns completed task.
    /// </summary>
    /// <typeparam name="T">The type of value to store.</typeparam>
    /// <param name="key">The cache key to store under.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">Optional expiration time span for the cached value.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes a value from cache by key.
    /// In this null implementation, performs no operation and returns completed task.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Removes cached values matching a pattern.
    /// In this null implementation, performs no operation and returns completed task.
    /// </summary>
    /// <param name="pattern">The pattern to match cache keys against.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all cached values.
    /// In this null implementation, performs no operation and returns completed task.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Retrieves a value from cache or computes and stores it if not present.
    /// In this null implementation, delegates to the provided factory function without caching.
    /// </summary>
    /// <typeparam name="T">The type of value to retrieve or store.</typeparam>
    /// <param name="key">The cache key to look up.</param>
    /// <param name="factory">The function to compute the value if not found in cache.</param>
    /// <param name="expiration">Optional expiration time span for the cached value.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the retrieved or computed value.</returns>
    public Task<T?> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T?>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        return factory(cancellationToken);
    }
}
