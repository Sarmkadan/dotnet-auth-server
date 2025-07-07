# TokenRevocationBenchmarks

`TokenRevocationBenchmarks` provides a benchmarking suite designed to evaluate the performance and efficiency of token revocation mechanisms within the authentication server. This class allows developers to measure the latency and throughput of revoking different types of tokens under various conditions, ensuring the revocation flow maintains acceptable performance characteristics under load.

## API

### Setup
`public void Setup`

Initializes the benchmark environment. This method is responsible for preparing necessary services, mocking dependencies, and establishing the required state before performance measurements begin. This is typically decorated with `[GlobalSetup]` in a BenchmarkDotNet context.

### RevokeAccessToken
`public async Task<bool> RevokeAccessToken`

Measures the performance of the revocation process for a valid access token.

*   **Returns:** `Task<bool>` - Returns `true` if the access token was successfully revoked, `false` otherwise.

### RevokeRefreshToken
`public async Task<bool> RevokeRefreshToken`

Measures the performance of the revocation process for a valid refresh token.

*   **Returns:** `Task<bool>` - Returns `true` if the refresh token was successfully revoked, `false` otherwise.

### RevokeInvalidToken
`public async Task<bool> RevokeInvalidToken`

Measures the performance of the system when attempting to revoke an invalid, expired, or non-existent token, verifying that error handling paths are performant.

*   **Returns:** `Task<bool>` - Returns `false` as expected for a failed revocation attempt.

## Usage

### Running benchmarks via BenchmarkDotNet
The primary use case is to execute these benchmarks using the BenchmarkDotNet framework to gather performance statistics.

```csharp
using BenchmarkDotNet.Running;

// Run the benchmarks
var summary = BenchmarkRunner.Run<TokenRevocationBenchmarks>();
```

### Manual invocation in integration tests
These methods can also be utilized within integration tests to verify that revocation logic remains functional and performant after codebase changes.

```csharp
var benchmarks = new TokenRevocationBenchmarks();
benchmarks.Setup();

bool result = await benchmarks.RevokeAccessToken();

if (!result)
{
    throw new Exception("Access token revocation failed unexpectedly.");
}
```

## Notes

*   **Thread Safety:** The methods in this class are designed to be executed by a benchmarking runner. If extending these benchmarks to simulate high-concurrency, ensure that the underlying data stores and services used in `Setup` are thread-safe or properly isolated per iteration.
*   **Edge Cases:** Tests should account for scenarios including database connectivity issues, high-latency storage backends, and malformed token inputs. Ensure that `RevokeInvalidToken` accurately reflects expected production behavior, such as returning `false` without throwing an exception when provided with a non-existent token identifier.
*   **Environment:** Performance results are highly dependent on the environment configuration, including the type of token storage (e.g., Redis, SQL database) and the complexity of the token validation logic. Ensure the `Setup` method accurately mimics the production environment configuration.
