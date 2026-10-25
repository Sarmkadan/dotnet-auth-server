# ScopeValidationService

`ScopeValidationService` validates, checks, merges, and filters OAuth 2.0 / OpenID Connect scope strings. It is a sealed class in `src/Services/ScopeValidationService.cs`, namespace `DotnetAuthServer.Services`.

It handles four concerns:

- **Validation** of a requested, space-delimited scope string (`ValidateScopesAsync`).
- **Required-scope checks** for OIDC requests (`GetRequiredScopes`, `ContainsRequiredScopes`).
- **Merging** several scope lists into one canonical string (`MergeScopes`).
- **Filtering** granted scopes against a requested subset, used by refresh-token grants (`FilterScopes`).

Scope syntax (space-delimited, RFC 6749 §3.3) is parsed with the `ParseScopes` and `JoinScopes` string extensions in `src/Extensions/StringExtensions.cs`. Duplicates are removed during parsing.

## Dependencies

| Dependency | Purpose |
|---|---|
| `ScopeService` | Scope definitions. Declared as a dependency, but the current validation path does not query it (see [Known limitations](#known-limitations)). |
| `ICacheService` | Caches per-scope validity results under the key `scope:valid:{scope}` for 24 hours. |
| `ILogger<ScopeValidationService>` | Logs invalid scopes and errors. |

Standard OIDC scopes are always valid and never reach the cache: `openid`, `profile`, `email`, `address`, `phone`, `offline_access`.

## Registration

The class is a concrete type. Per the project's DI conventions it is registered without an interface:

```csharp
builder.Services.AddScoped<ScopeValidationService>();
```

Callers depend on `ScopeValidationService` directly.

## Constructor

### `public ScopeValidationService(ScopeService scopeService, ICacheService cacheService, ILogger<ScopeValidationService> logger)`

Creates the service from its collaborators.

- **Parameters**:
  - `scopeService`: The scope service.
  - `cacheService`: The cache used for per-scope validity results.
  - `logger`: The logger.
- **Throws**:
  - `ArgumentNullException`: If any parameter is `null`.

## Methods

### `public async Task<IEnumerable<string>> ValidateScopesAsync(string? scopeString, CancellationToken cancellationToken = default)`

Parses a space-delimited scope string and returns the scopes that are valid.

- **Parameters**:
  - `scopeString`: The scope string, for example `"openid profile api:read"`. `null`, empty, or whitespace-only input is allowed.
  - `cancellationToken`: Passed to the cache calls. Cancellation is not wrapped.
- **Return value**: A `Task<IEnumerable<string>>` containing the valid scopes in request order, with duplicates removed. Invalid scopes are omitted. The result is an empty sequence when the input is `null` or whitespace. The method does not return `null`.
- **Side effects**: If any scope is invalid, a warning listing the invalid scopes is logged.
- **Throws**:
  - `AuthServerException` with `ErrorCode` `server_error` and `StatusCode` 500 when an unexpected error occurs while parsing or validating. The original exception is attached as the inner exception.
  - `OperationCanceledException` when `cancellationToken` is cancelled. It is not wrapped.

Example:

```csharp
var valid = (await scopeValidationService.ValidateScopesAsync("openid profile openid"))
    .ToList();
// valid: ["openid", "profile"]: duplicates are removed
```

Per-scope behaviour, implemented by the private `IsScopeValidAsync`:

1. Standard OIDC scopes return `true` immediately.
2. Otherwise the cache is read at `scope:valid:{scope}`. A cached value that parses as a `bool` is returned.
3. On a cache miss the result is written back to the cache for 24 hours and returned.
4. Any exception other than cancellation is logged and treated as `false`. The scope is then reported as invalid. This path does not throw.

### `public IEnumerable<string> GetRequiredScopes(bool isOidc = true)`

Returns the minimum scopes a request must include.

- **Parameters**:
  - `isOidc`: `true` for an OpenID Connect request. Defaults to `true`.
- **Return value**: A lazily evaluated sequence. It yields `"openid"` when `isOidc` is `true`, and is empty otherwise.
- **Throws**: None.

### `public bool ContainsRequiredScopes(IEnumerable<string> requestedScopes, bool isOidc = true)`

Checks whether the requested scopes include every scope returned by `GetRequiredScopes(isOidc)`.

- **Parameters**:
  - `requestedScopes`: The scopes to check, already parsed. Comparison is case-sensitive (`HashSet<string>` default comparer).
  - `isOidc`: Passed to `GetRequiredScopes`. Defaults to `true`.
- **Return value**: `true` when all required scopes are present. `false` otherwise.
- **Throws**: None. A `null` `requestedScopes` argument or any other internal error is logged and returns `false`.

Note that this method takes a sequence, not a space-delimited string. Parse the string first with `ParseScopes()` if needed.

### `public string MergeScopes(params IEnumerable<string>[] scopeLists)`

Combines any number of scope sequences into one space-delimited string.

- **Parameters**:
  - `scopeLists`: Zero or more scope sequences.
- **Return value**: A string containing the union of all scopes, with duplicates removed, sorted with the default string comparer, and joined by single spaces. Returns an empty string when there are no scopes.
- **Throws**:
  - `AuthServerException` with `ErrorCode` `server_error` and `StatusCode` 500 when an error occurs. This includes a `null` sequence inside `scopeLists`, which causes a `NullReferenceException` that is wrapped. The original exception is attached as the inner exception.

Example:

```csharp
var merged = scopeValidationService.MergeScopes(
    new[] { "profile", "openid" },
    new[] { "openid", "email" });
// merged: "email openid profile"
```

### `public IEnumerable<string> FilterScopes(IEnumerable<string> grantedScopes, IEnumerable<string> requestedScopes)`

Returns the scopes that are both granted and requested. This is used when a refresh-token request asks for a subset of the original scopes.

- **Parameters**:
  - `grantedScopes`: Scopes originally granted to the token or grant.
  - `requestedScopes`: Scopes the client now asks for.
- **Return value**: The intersection, in the order of `grantedScopes`, with duplicates removed. Scopes requested but not granted are dropped silently. The result is empty when nothing overlaps.
- **Throws**:
  - None. Any internal error is logged and an empty sequence is returned. A caller that receives an empty result cannot tell a real empty intersection from an error, so check the logs when that matters.

Note that this method does not widen scope. A requested scope that was never granted never appears in the result.

## Usage

### Validating a token request

```csharp
var validScopes = await scopeValidationService.ValidateScopesAsync(request.Scope, cancellationToken);
if (!scopeValidationService.ContainsRequiredScopes(validScopes))
{
    throw new InvalidScopeException("The 'openid' scope is required.");
}

var scopeString = scopeValidationService.MergeScopes(validScopes);
```

### Refreshing with a narrower scope set

```csharp
// grantedScopes: the scopes stored with the original grant (source of your choice)
var narrowed = scopeValidationService.FilterScopes(
    grantedScopes,
    request.Scope.ParseScopes());

var tokenScope = narrowed.JoinScopes();
```

## Known limitations

- **Existence check is a stub.** `IsScopeValidAsync` sets `exists = true` for every non-standard scope and notes that a database query belongs there. As written, any syntactically present scope is accepted, and only the standard OIDC scopes and cache entries affect the result. `ScopeService` is injected but not used by this path. Treat `ValidateScopesAsync` as a syntax and caching seam until the lookup is implemented.
- **Cache entries never change.** Because the stored value is always `true` today, a scope rejected later would stay cached as valid for up to 24 hours.
- **Case-sensitive comparisons.** Scopes are compared with ordinal `HashSet` semantics. `Profile` and `profile` are different scopes.
- **Errors in `ContainsRequiredScopes` and `FilterScopes` are silent.** They log and return `false` or an empty sequence rather than throwing. Only `ValidateScopesAsync` and `MergeScopes` throw `AuthServerException`.
- **`GetRequiredScopes` is not a validation step.** It only reports what is required. Callers must call `ContainsRequiredScopes` to enforce it.

## See also

- [ClientValidationService](ClientValidationService.md) and [PkceValidationService](PkceValidationService.md): sibling validation services.
- [AuthServerException](AuthServerException.md): the exception type thrown on failure.
