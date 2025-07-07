# TokenIntrospectionBenchmarks

`TokenIntrospectionBenchmarks` is a benchmarking class designed to measure the performance characteristics of token introspection operations within the `dotnet-auth-server` framework. It provides standardized test scenarios for evaluating the computational overhead and latency associated with validating various token states, including valid, invalid, and expired tokens, to ensure optimal performance in high-throughput authentication environments.

## API

### Setup
`public void Setup()`
Initializes the necessary authentication components, configuration mocks, and token stores required for subsequent benchmark execution. This method must be invoked before executing any benchmarking methods to ensure the environment is correctly prepared.

### IntrospectValidToken
`public bool IntrospectValidToken()`
Executes an introspection request for a cryptographically valid and active token. Returns `true` if the introspection operation completed successfully, indicating the token is valid; otherwise, returns `false`.

### IntrospectInvalidToken
`public bool IntrospectInvalidToken()`
Executes an introspection request for a cryptographically or structurally invalid token. Returns `true` if the introspection operation completed without error, confirming the rejection of the invalid token; otherwise, returns `false`.

### IntrospectExpiredToken
`public bool IntrospectExpiredToken()`
Executes an introspection request for a token that has passed its expiration timestamp. Returns `true` if the introspection operation completed, confirming the identification of the expired token; otherwise, returns `false`.

## Usage

### Running benchmarks via BenchmarkDotNet
To execute these benchmarks using the BenchmarkDotNet framework, define a program entry point to run the suite:

```csharp
using BenchmarkDotNet.Running;

public class Program
{
    public static void Main(string[] args)
    {
        // Executes all methods marked with [Benchmark] in TokenIntrospectionBenchmarks
        var summary = BenchmarkRunner.Run<TokenIntrospectionBenchmarks>();
    }
}
```

### Manual execution for performance profiling
For manual performance profiling or integration into custom diagnostic tools, the components can be invoked directly:

```csharp
public void ProfileIntrospection()
{
    var benchmarks = new TokenIntrospectionBenchmarks();
    
    // Prepare the environment
    benchmarks.Setup();
    
    // Measure latency for valid token introspection
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    bool isValid = benchmarks.IntrospectValidToken();
    stopwatch.Stop();
    
    Console.WriteLine($"Introspection result: {isValid}, Took: {stopwatch.ElapsedMilliseconds}ms");
}
```

## Notes

*   **BenchmarkDotNet Integration**: These methods are designed to be decorated with BenchmarkDotNet attributes (e.g., `[Benchmark]`) for automated execution. The `Setup` method should typically be attributed with `[GlobalSetup]` to ensure it runs once per benchmark iteration.
*   **Performance Variance**: Results for these benchmarks are heavily dependent on the underlying implementation of the token store (e.g., in-memory versus database-backed providers) and the complexity of the token validation logic.
*   **Thread Safety**: While the individual introspection methods are designed to be read-only regarding state after `Setup` completes, the class is not intended for concurrent modification. When running benchmarks in parallel, ensure the BenchmarkDotNet configuration accounts for thread isolation to prevent inconsistent results.
*   **Edge Cases**: The `IntrospectExpiredToken` benchmark assumes a fixed clock state; significant deviations in system time or improper configuration of clock skew settings within the environment may impact the accuracy of the results.
