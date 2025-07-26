using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using DotnetAuthServer.Services;
using DotnetAuthServer.Configuration;

namespace DotnetAuthServer.Benchmarks;

/// <summary>
/// Benchmark class for PKCE validation operations.
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class PkceBenchmarks
{
    private AuthServerOptions _options;
    private PkceValidationService _pkceValidationService;
    private string _codeVerifier;
    private string _codeChallenge;
    private string _invalidCodeVerifier;

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

        _pkceValidationService = new PkceValidationService(_options, NullLogger<PkceValidationService>.Instance);
        _codeVerifier = _pkceValidationService.GenerateCodeVerifier();
        _codeChallenge = _pkceValidationService.GenerateCodeChallenge(_codeVerifier);
        _invalidCodeVerifier = _pkceValidationService.GenerateCodeVerifier();
    }

    /// <summary>
    /// Generates a code verifier.
    /// </summary>
    /// <returns>A randomly generated code verifier.</returns>
    [Benchmark]
    public string GenerateCodeVerifier()
    {
        return _pkceValidationService.GenerateCodeVerifier();
    }

    /// <summary>
    /// Generates a code challenge from a given code verifier.
    /// </summary>
    /// <returns>A code challenge generated from the code verifier.</returns>
    [Benchmark]
    public string GenerateCodeChallenge()
    {
        return _pkceValidationService.GenerateCodeChallenge(_codeVerifier);
    }

    /// <summary>
    /// Validates a code verifier and challenge pair.
    /// </summary>
    /// <returns>True if the code verifier and challenge pair are valid, false otherwise.</returns>
    [Benchmark]
    public bool ValidatePkce()
    {
        return _pkceValidationService.ValidateCodeVerifier(_codeVerifier, _codeChallenge);
    }

    /// <summary>
    /// Validates a code verifier and challenge pair with an invalid code verifier.
    /// </summary>
    /// <returns>False, as the code verifier is invalid.</returns>
    [Benchmark]
    public bool ValidatePkce_Invalid()
    {
        return _pkceValidationService.ValidateCodeVerifier(_invalidCodeVerifier, _codeChallenge);
    }

    /// <summary>
    /// Validates a code verifier and challenge pair with a wrong challenge.
    /// </summary>
    /// <returns>False, as the challenge is invalid.</returns>
    [Benchmark]
    public bool ValidatePkce_WrongChallenge()
    {
        return _pkceValidationService.ValidateCodeVerifier(_codeVerifier, _invalidCodeVerifier);
    }
}
