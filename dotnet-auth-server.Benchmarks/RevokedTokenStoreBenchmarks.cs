using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using DotnetAuthServer.Security;

namespace dotnet_auth_server.Benchmarks;

/// <summary>
/// Benchmarks for the <see cref="RevokedTokenStore"/> class.
/// Covers the most frequently used public operations:
/// registration, revocation, family revocation and revocation checks.
/// </summary>
[MemoryDiagnoser]
public class RevokedTokenStoreBenchmarks
{
    private RevokedTokenStore _store = null!;

    // Arrays holding pre‑generated data for the benchmarks.
    private string[] _jtis = null!;
    private DateTime[] _expiries = null!;
    private string[] _familyIds = null!;

    // A family identifier that will be used for the RevokeFamily benchmark.
    private string _targetFamilyId = null!;

    // Input size for the benchmarks – the number of tokens to work with.
    [Params(10, 100, 1000)]
    public int TokenCount { get; set; }

    /// <summary>
    /// Generates test data and registers the tokens in the store.
    /// Half of the tokens are revoked so that <c>IsRevoked</c> has both
    /// true and false paths to measure.
    /// </summary>
    [GlobalSetup]
    public void GlobalSetup()
    {
        _store = new RevokedTokenStore();

        _jtis = new string[TokenCount];
        _expiries = new DateTime[TokenCount];
        _familyIds = new string[TokenCount];

        var rnd = new Random(42);
        for (int i = 0; i < TokenCount; i++)
        {
            _jtis[i] = Guid.NewGuid().ToString("N");
            // Tokens expire between 5 and 60 minutes from now.
            _expiries[i] = DateTime.UtcNow.AddMinutes(5 + rnd.NextDouble() * 55);
            // Create a few families – reuse the same family id for groups of 10 tokens.
            _familyIds[i] = $"family-{i / 10}";
        }

        // Register all tokens.
        for (int i = 0; i < TokenCount; i++)
        {
            _store.RegisterToken(_jtis[i], _expiries[i], _familyIds[i]);
        }

        // Revoke the first half of the tokens.
        for (int i = 0; i < TokenCount / 2; i++)
        {
            _store.Revoke(_jtis[i], _expiries[i], _familyIds[i]);
        }

        // Choose a family that definitely exists for the family‑revocation benchmark.
        _targetFamilyId = _familyIds[0];
    }

    /// <summary>
    /// Benchmarks the cost of registering a new token.
    /// </summary>
    [Benchmark]
    public void RegisterToken()
    {
        // Register a fresh token each iteration to avoid duplicate‑key overhead.
        var jti = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddMinutes(30);
        var familyId = $"family-{TokenCount}";

        _store.RegisterToken(jti, expiresAt, familyId);
    }

    /// <summary>
    /// Benchmarks revoking a token that is already known to the store.
    /// </summary>
    [Benchmark]
    public void RevokeToken()
    {
        // Revoke the second half of the tokens (they are not revoked yet).
        for (int i = TokenCount / 2; i < TokenCount; i++)
        {
            _store.Revoke(_jtis[i], _expiries[i], _familyIds[i]);
        }
    }

    /// <summary>
    /// Benchmarks revoking an entire family of tokens.
    /// </summary>
    [Benchmark]
    public void RevokeFamily()
    {
        _store.RevokeFamily(_targetFamilyId);
    }

    /// <summary>
    /// Benchmarks checking whether a token is revoked.
    /// </summary>
    [Benchmark]
    public void IsRevoked()
    {
        // Alternate between revoked and non‑revoked tokens.
        for (int i = 0; i < TokenCount; i++)
        {
            _store.IsRevoked(_jtis[i]);
        }
    }
}
