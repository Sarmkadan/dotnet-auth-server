# ScopeValidationBenchmarks

The `ScopeValidationBenchmarks` class provides a suite of BenchmarkDotNet tests designed to evaluate the performance and correctness of scope validation logic within the `dotnet-auth-server` system. By simulating various scenarios—such as valid, invalid, empty, single, and multiple scope configurations—it enables developers to quantify the computational overhead of scope parsing and verification, ensuring that authentication and authorization workflows maintain optimal responsiveness under load.

## API

*   **`Setup()`**
    Prepares the necessary environment, dependencies, and test data required for subsequent benchmark execution. This method must be called prior to running any performance tests.

*   **`ValidateScopes_Valid()`**
    Benchmarks the performance of validating a set of scopes that strictly conform to the defined authorization policy.

*   **`ValidateScopes_InvalidScope()`**
    Benchmarks the performance of detecting unauthorized, malformed, or unrecognized scopes within an authentication request.

*   **`ValidateScopes_EmptyScope()`**
    Benchmarks the system's handling of requests where no scopes are provided, ensuring efficient management of empty input strings or collections.

*   **`ValidateScopes_SingleScope()`**
    Benchmarks the validation process for authentication requests containing exactly one requested scope.

*   **`ValidateScopes_MultipleScopes()`**
    Benchmarks the validation process for requests containing a complex set of multiple distinct, authorized scopes.

## Usage

**Example 1: Executing via BenchmarkDotNet**

Typically, these benchmarks are executed using the BenchmarkDotNet harness in a dedicated test or console project.

```csharp
using BenchmarkDotNet.Running;

public class Program
{
    public static void Main(string[] args)
    {
        // Execute all benchmarks within the class
        BenchmarkRunner.Run<ScopeValidationBenchmarks>();
    }
}
```

**Example 2: Manual invocation for verification**

While primarily intended for BenchmarkDotNet, the methods can be invoked directly if the environment has been prepared.

```csharp
var benchmarks = new ScopeValidationBenchmarks();

// Must initialize state before running
benchmarks.Setup();

// Execute a specific validation benchmark
bool result = benchmarks.ValidateScopes_Valid();

Console.WriteLine($"Validation result: {result}");
```

## Notes

*   **Performance Variability:** Benchmark results are highly dependent on the size of the allowed scope set configured in the underlying `dotnet-auth-server` policy engine. Larger policies will naturally increase the computational cost of validation.
*   **Thread Safety:** This class is designed specifically for use by the BenchmarkDotNet infrastructure and is not inherently thread-safe. The `Setup` method modifies internal state and is not designed for concurrent execution.
*   **Test Data:** The benchmarks utilize predefined datasets to ensure consistency across runs. They do not reflect dynamic runtime traffic conditions and should be interpreted as a baseline for algorithmic performance.
