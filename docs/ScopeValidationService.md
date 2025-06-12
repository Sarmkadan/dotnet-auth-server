# ScopeValidationService

Central service for validating and managing OAuth scopes within the authentication pipeline. It ensures that requested scopes are valid, required scopes are present, and provides utilities for scope manipulation and comparison.

## API

### `public ScopeValidationService()`

Initializes a new instance of the `ScopeValidationService`.

### `public async Task<IEnumerable<string>> ValidateScopesAsync(IEnumerable<string> requestedScopes, IEnumerable<string> requiredScopes)`

Validates that all required scopes are present within the requested scopes.

- **Parameters**
  - `requestedScopes`: The scopes requested by the client.
  - `requiredScopes`: The scopes required for the operation.
- **Return value**: A `Task` resolving to an `IEnumerable<string>` of missing scopes. If no scopes are missing, the result is empty.
- **Exceptions**: Throws `ArgumentNullException` if either `requestedScopes` or `requiredScopes` is `null`.

### `public IEnumerable<string> GetRequiredScopes()`

Retrieves the set of scopes required for the current operation context.

- **Return value**: An `IEnumerable<string>` of required scopes.

### `public bool ContainsRequiredScopes(IEnumerable<string> requestedScopes)`

Determines whether the requested scopes contain all required scopes.

- **Parameters**
  - `requestedScopes`: The scopes requested by the client.
- **Return value**: `true` if all required scopes are present; otherwise, `false`.
- **Exceptions**: Throws `ArgumentNullException` if `requestedScopes` is `null`.

### `public string MergeScopes(IEnumerable<string> scopes)`

Merges a collection of scopes into a single space-separated string.

- **Parameters**
  - `scopes`: The scopes to merge.
- **Return value**: A space-separated string of scopes. Returns `null` if `scopes` is `null` or empty.
- **Exceptions**: None.

### `public IEnumerable<string> FilterScopes(IEnumerable<string> scopes, Func<string, bool> predicate)`

Filters a collection of scopes based on a predicate.

- **Parameters**
  - `scopes`: The scopes to filter.
  - `predicate`: The predicate used to determine inclusion.
- **Return value**: An `IEnumerable<string>` of scopes that satisfy the predicate. Returns an empty sequence if `scopes` is `null` or `predicate` is `null`.
- **Exceptions**: None.

## Usage

### Validating requested scopes against required scopes
