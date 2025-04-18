using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using DotnetAuthServer.Services;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Domain.Models;

namespace DotnetAuthServer.Benchmarks;

[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ScopeValidationBenchmarks
{
    private AuthServerOptions _options;
    private ScopeValidationService _scopeValidationService;
    private Client _client;

    [GlobalSetup]
    public void Setup()
    {
        _options = new AuthServerOptions
        {
            JwtSigningKey = "super-secret-key-that-is-at-least-32-characters-long!",
            IssuerUrl = "https://auth.example.com"
        };

        _scopeValidationService = new ScopeValidationService(_options, NullLogger<ScopeValidationService>.Instance);

        // Setup client with allowed scopes
        _client = new Client
        {
            ClientId = "test-client",
            AllowedScopes = new List<string> { "openid", "profile", "email", "api:read", "api:write" }
        };
    }

    [Benchmark]
    public bool ValidateScopes_Valid()
    {
        var request = new TokenRequest
        {
            ClientId = "test-client",
            Scope = "openid profile email"
        };

        return _scopeValidationService.ValidateScopes(request, _client);
    }

    [Benchmark]
    public bool ValidateScopes_InvalidScope()
    {
        var request = new TokenRequest
        {
            ClientId = "test-client",
            Scope = "openid profile invalid:scope"
        };

        return _scopeValidationService.ValidateScopes(request, _client);
    }

    [Benchmark]
    public bool ValidateScopes_EmptyScope()
    {
        var request = new TokenRequest
        {
            ClientId = "test-client",
            Scope = ""
        };

        return _scopeValidationService.ValidateScopes(request, _client);
    }

    [Benchmark]
    public bool ValidateScopes_SingleScope()
    {
        var request = new TokenRequest
        {
            ClientId = "test-client",
            Scope = "openid"
        };

        return _scopeValidationService.ValidateScopes(request, _client);
    }

    [Benchmark]
    public bool ValidateScopes_MultipleScopes()
    {
        var request = new TokenRequest
        {
            ClientId = "test-client",
            Scope = "openid profile email api:read api:write"
        };

        return _scopeValidationService.ValidateScopes(request, _client);
    }
}
