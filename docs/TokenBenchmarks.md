# TokenBenchmarks

The `TokenBenchmarks` class provides standardized performance measurement and load testing capabilities for critical OAuth 2.0 and OpenID Connect operations within the `dotnet-auth-server` infrastructure. It allows developers to isolate and benchmark token processing tasks—including introspection, PKCE validation, and grant handling—to identify potential performance bottlenecks and validate optimizations under controlled conditions.

## API

### Setup()
Initializes the benchmarking environment. This method prepares necessary data structures, mock configurations, and service dependencies required for subsequent benchmark operations.
*   **Parameters:** None
*   **Returns:** `void`
*   **Throws:** None

### IntrospectToken()
Benchmarks the token introspection process. This measures the latency and throughput of evaluating the validity and metadata of access tokens.
*   **Parameters:** None (Expects environment to be configured via `Setup`)
*   **Returns:** `IntrospectionResponse`
*   **Throws:** `InvalidOperationException` if called before `Setup`.

### ValidatePkce()
Measures the performance of Proof Key for Code Exchange (PKCE) validation logic.
*   **Parameters:** None
*   **Returns:** `bool` indicating whether the PKCE challenge was successfully validated.
*   **Throws:** None

### ValidateClientCredentials()
Benchmarks the authentication logic for client credentials. This task evaluates the performance of client lookup and credential verification.
*   **Parameters:** None
*   **Returns:** `Task<Client>` representing the validated client.
*   **Throws:** `AuthenticationException` if credentials are invalid.

### HandleClientCredentialsGrant()
Evaluates the total performance of handling a client credentials grant flow, from validation to token issuance.
*   **Parameters:** None
*   **Returns:** `Task<TokenResponse>` containing the issued token information.
*   **Throws:** `AuthenticationException` if the grant flow cannot be completed.

## Usage

### Example 1: Basic Benchmark Setup
```csharp
var benchmarks = new TokenBenchmarks();

// Initialize required environment/mocks
benchmarks.Setup();

// Perform a benchmarked introspection operation
var result = benchmarks.IntrospectToken();
Console.WriteLine($"Introspection status: {result.Active}");
```

### Example 2: Integration with BenchmarkDotNet
```csharp
[MemoryDiagnoser]
public class OAuthBenchmarks
{
    private TokenBenchmarks _benchmarks;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _benchmarks = new TokenBenchmarks();
        _benchmarks.Setup();
    }

    [Benchmark]
    public async Task<TokenResponse> BenchmarkGrantHandling()
    {
        return await _benchmarks.HandleClientCredentialsGrant();
    }
}
```

## Notes

*   **State Management:** The `Setup()` method is not idempotent and should be called exactly once per benchmark iteration or session. Subsequent calls may reset initialized state or throw exceptions depending on the specific implementation.
*   **Thread Safety:** This class is generally not thread-safe. When running concurrent benchmarks, instantiate a unique `TokenBenchmarks` instance for each thread to avoid race conditions on the internal mock state.
*   **Environment Dependency:** Benchmarks must be run in an environment that simulates realistic production load; ensure any external dependencies are mocked consistently to avoid variance in results.
