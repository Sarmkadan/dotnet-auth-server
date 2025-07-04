# ClientValidationBenchmarks

The `ClientValidationBenchmarks` class provides a specialized benchmarking suite designed to evaluate the performance and efficacy of client validation processes within the `dotnet-auth-server` ecosystem. By simulating various client scenarios—including confidential, public, and inactive clients—this component allows developers to measure latency, throughput, and correctness under load, while also offering direct access to underlying caching mechanisms for state management during test execution.

## API

### Setup
Initializes the benchmarking environment, ensuring all necessary state stores or dependencies are prepared for subsequent validation tests.
- **Parameters**: None.
- **Return Value**: `void`.
- **Throws**: `InvalidOperationException` if the setup fails due to misconfiguration or resource unavailability.

### ValidateConfidentialClient
Executes a benchmark for the validation logic applied to confidential clients.
- **Parameters**: None.
- **Return Value**: `Task<bool>` representing whether the client was successfully validated.
- **Throws**: None.

### ValidatePublicClient
Executes a benchmark for the validation logic applied to public clients.
- **Parameters**: None.
- **Return Value**: `Task<bool>` representing whether the client was successfully validated.
- **Throws**: None.

### ValidateInactiveClient
Executes a benchmark for the validation logic applied to inactive clients.
- **Parameters**: None.
- **Return Value**: `Task<bool>` representing whether the client was successfully validated.
- **Throws**: None.

### GetAsync\<T>
Retrieves a cached value of the specified type `T`.
- **Parameters**: `string key` (The identifier for the item to retrieve).
- **Return Value**: `Task<T?>` the retrieved object, or `default` if the key is not found.
- **Throws**: `ArgumentNullException` if the key is null.

### SetAsync\<T>
Stores a value of type `T` in the underlying cache.
- **Parameters**: `string key` (The identifier for the item), `T value` (The object to store).
- **Return Value**: `Task`.
- **Throws**: `ArgumentNullException` if the key is null.

### RemoveAsync
Removes an entry from the cache.
- **Parameters**: `string key` (The identifier for the item to remove).
- **Return Value**: `Task`.
- **Throws**: `ArgumentNullException` if the key is null.

### RemoveByPatternAsync
Removes entries from the cache matching a specific pattern.
- **Parameters**: `string pattern` (The regex or wildcard pattern to match keys against).
- **Return Value**: `Task`.
- **Throws**: `ArgumentNullException` if the pattern is null.

### ClearAsync
Removes all entries from the cache.
- **Parameters**: None.
- **Return Value**: `Task`.
- **Throws**: None.

### GetOrSetAsync\<T>
Attempts to retrieve a value; if the value does not exist, it executes a factory function to retrieve/generate it, stores it, and returns the result.
- **Parameters**: `string key` (The identifier), `Func<Task<T>> factory` (The function to call if the key is missing).
- **Return Value**: `Task<T?>`.
- **Throws**: `ArgumentNullException` if key or factory is null.

## Usage

### Basic Benchmarking
```csharp
var benchmark = new ClientValidationBenchmarks();
benchmark.Setup();

// Run validation benchmark for a confidential client
bool isValid = await benchmark.ValidateConfidentialClient();
Console.WriteLine($"Confidential client validation successful: {isValid}");
```

### State Store Interaction
```csharp
var benchmark = new ClientValidationBenchmarks();
await benchmark.Setup();

// Manually manage state for complex benchmark scenarios
await benchmark.SetAsync<string>("test-client-id", "active-status");
var status = await benchmark.GetAsync<string>("test-client-id");

// Clean up cache
await benchmark.RemoveAsync("test-client-id");
```

## Notes

- **Thread-Safety**: While the `ClientValidationBenchmarks` methods are asynchronous and generally thread-safe, the underlying storage mechanism (cache) must be evaluated independently for concurrency guarantees. When running benchmarks in parallel, ensure the cache implementation supports concurrent access to prevent data corruption or race conditions.
- **GetOrSetAsync Atomicity**: The atomicity of `GetOrSetAsync` depends on the implementation of the underlying store. If the store does not support atomic "get-if-not-exists" operations, this method might lead to multiple factory calls under high concurrent contention.
- **Pattern Matching**: `RemoveByPatternAsync` efficiency is dictated by the underlying storage provider's capability to perform pattern-based lookups. Large-scale cache clears via pattern matching should be used judiciously in performance-sensitive environments.
