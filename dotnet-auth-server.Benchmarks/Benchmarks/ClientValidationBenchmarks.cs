using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using DotnetAuthServer.Services;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Data.Repositories;
using DotnetAuthServer.Caching;
using Moq;

namespace DotnetAuthServer.Benchmarks;

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

    [Benchmark]
    public async Task<bool> ValidateConfidentialClient()
    {
        var client = await _clientValidationService.ValidateClientAsync("confidential-client", "secret", "client_credentials");
        return client != null && client.IsActive;
    }

    [Benchmark]
    public async Task<bool> ValidatePublicClient()
    {
        var client = await _clientValidationService.ValidateClientAsync("public-client", null, "authorization_code");
        return client != null && client.IsActive;
    }

    [Benchmark]
    public async Task<bool> ValidateInactiveClient()
    {
        _clientRepositoryMock.Setup(x => x.GetActiveClientAsync("inactive-client", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Client)null);

        var client = await _clientValidationService.ValidateClientAsync("inactive-client", "secret", "client_credentials");
        return client == null;
    }
}

public class NullCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        return Task.FromResult<T?>(null);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task ClearAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<T?> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T?>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        return factory(cancellationToken);
    }
}
