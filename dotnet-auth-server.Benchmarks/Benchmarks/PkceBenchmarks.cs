using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging.Abstractions;
using DotnetAuthServer.Services;
using DotnetAuthServer.Configuration;

namespace DotnetAuthServer.Benchmarks;

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

    [Benchmark]
    public string GenerateCodeVerifier()
    {
        return _pkceValidationService.GenerateCodeVerifier();
    }

    [Benchmark]
    public string GenerateCodeChallenge()
    {
        return _pkceValidationService.GenerateCodeChallenge(_codeVerifier);
    }

    [Benchmark]
    public bool ValidatePkce()
    {
        return _pkceValidationService.ValidateCodeVerifier(_codeVerifier, _codeChallenge);
    }

    [Benchmark]
    public bool ValidatePkce_Invalid()
    {
        return _pkceValidationService.ValidateCodeVerifier(_invalidCodeVerifier, _codeChallenge);
    }

    [Benchmark]
    public bool ValidatePkce_WrongChallenge()
    {
        return _pkceValidationService.ValidateCodeVerifier(_codeVerifier, _invalidCodeVerifier);
    }
}
