# PkceBenchmarks

The `PkceBenchmarks` class is a specialized suite designed for the `dotnet-auth-server` project to measure and analyze the performance characteristics of Proof Key for Code Exchange (PKCE) operations. It provides methods to benchmark the generation of PKCE components (code verifiers and challenges) and the validation logic, ensuring that authentication workflows maintain acceptable latency and throughput under load.

## API

- `public void Setup()`
  Prepares the required services and benchmark state, ensuring all dependencies are initialized before execution. This method is typically invoked by the BenchmarkDotNet infrastructure.

- `public string GenerateCodeVerifier()`
  Generates a random, cryptographically secure code verifier string compliant with PKCE standards. Returns a string suitable for use in an OAuth 2.0 authorization request.

- `public string GenerateCodeChallenge()`
  Generates a code challenge by applying the S256 transform to a randomly generated code verifier. Returns a base64url-encoded string representing the challenge.

- `public bool ValidatePkce()`
  Executes a benchmark iteration of the PKCE validation process using a valid verifier and challenge pair. Returns `true` if the validation succeeds.

- `public bool ValidatePkce_Invalid()`
  Executes a benchmark iteration of the PKCE validation process with malformed input, measuring performance under failure scenarios. Returns `false` upon detecting an invalid format.

- `public bool ValidatePkce_WrongChallenge()`
  Executes a benchmark iteration of the PKCE validation process where the provided verifier does not match the challenge. Returns `false` when validation fails due to a mismatch.

## Usage

### Example 1: Running the Benchmark Suite

```csharp
using BenchmarkDotNet.Running;
using DotnetAuthServer.Benchmarks;

// Execute the benchmark suite from the main entry point
var summary = BenchmarkRunner.Run<PkceBenchmarks>();
```

### Example 2: Manual Utility Usage in Tests

```csharp
using DotnetAuthServer.Benchmarks;

var benchmarks = new PkceBenchmarks();
benchmarks.Setup();

// Verify correct PKCE flow performance
bool isValid = benchmarks.ValidatePkce();
Console.WriteLine($"PKCE validation result: {isValid}");
```

## Notes

- **Thread Safety**: This class is intended for single-threaded execution within a BenchmarkDotNet context. The internal state, including service instances, is not designed for concurrent access across multiple threads.
- **Performance**: Validation methods (`ValidatePkce`, `ValidatePkce_Invalid`, `ValidatePkce_WrongChallenge`) are sensitive to the underlying cryptographic provider's performance, which may vary based on the host environment's CPU capabilities.
- **Setup Dependency**: The `Setup()` method must be invoked before any of the validation or generation methods to ensure necessary services (e.g., cryptographic providers, cache) are correctly initialized. Failure to do so will likely result in a `NullReferenceException`.
