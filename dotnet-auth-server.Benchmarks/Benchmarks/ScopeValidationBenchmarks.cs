using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using DotnetAuthServer.Services;
using DotnetAuthServer.Configuration;
using DotnetAuthServer.Domain.Entities;
using DotnetAuthServer.Domain.Models;

namespace DotnetAuthServer.Benchmarks;

/// <summary>
/// Benchmark class for scope validation.
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ScopeValidationBenchmarks
{
    private AuthServerOptions _options;
    private ScopeValidationService _scopeValidationService;
    private Client _client;

    /// <summary>
    /// Initializes the benchmark setup.
    /// </summary>
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

    /// <summary>
    /// Validates scopes for a valid request.
    /// </summary>
    /// <returns>True if the scopes are valid, false otherwise.</returns>
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

    /// <summary>
    /// Validates scopes for a request with an invalid scope.
    /// </summary>
    /// <returns>True if the scopes are valid, false otherwise.</returns>
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

    /// <summary>
    /// Validates scopes for a request with an empty scope.
    /// </summary>
    /// <returns>True if the scopes are valid, false otherwise.</returns>
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

    /// <summary>
    /// Validates scopes for a request with a single scope.
    /// </summary>
    /// <returns>True if the scopes are valid, false otherwise.</returns>
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

    /// <summary>
    /// Validates scopes for a request with multiple scopes.
    /// </summary>
    /// <returns>True if the scopes are valid, false otherwise.</returns>
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
