# ScopeService

`ScopeService` manages OAuth 2.0 / OpenID Connect scope definitions: creating scopes, attaching ID-token and access-token claims, assigning roles that may request a scope, listing active scopes with their claims, and filtering requested scopes down to those a user may access.

The class is `public sealed` and lives in `src/Services/ScopeService.cs`, namespace `DotnetAuthServer.Services`. It has no interface; register and inject the concrete type. It depends only on `IScopeRepository` and adds no NuGet dependencies.

## Role in the system

- All persistence goes through `IScopeRepository` (see [IScopeRepository](IScopeRepository.md)). The in-memory `ScopeRepository` in the same file is case-insensitive on scope IDs.
- Scope lookups are case-insensitive, so `profile` and `PROFILE` resolve to the same scope.
- Scope entities are `Scope` objects (see [Scope](Scope.md)). Failures are reported as `AuthServerException` (see [AuthServerException](AuthServerException.md)), which the error middleware converts to OAuth error JSON.
- Scope validation during authorization requests is handled by `ScopeValidationService`, which uses this service (see [ScopeValidationService](ScopeValidationService.md)).

## Constructor

### `public ScopeService(IScopeRepository scopeRepository)`

Creates the service.

**Parameters:**
- `scopeRepository` – store for `Scope` records.

**Exceptions:** `ArgumentNullException` if `scopeRepository` is `null`.

## Creating scopes

### `public async Task<Scope> CreateScopeAsync(string scopeId, string displayName, string description, bool isOpenIdScope = false, bool requiresConsent = true, CancellationToken cancellationToken = default)`

Creates a new, active scope.

**Side effects:** Persists a new `Scope` with `IsActive = true`, the given flags, and empty claim and role collections. `IsRequired` is left at its default (`false`).

**Parameters:**
- `scopeId` – unique scope identifier, such as `read:profile`. Must not already exist, compared case-insensitively.
- `displayName` – human-readable name for consent screens.
- `description` – explanation of what the scope grants.
- `isOpenIdScope` – marks the scope as an OpenID Connect scope (for example `openid` or `profile`). Default `false`.
- `requiresConsent` – whether the user must consent to the scope. Default `true`.
- `cancellationToken` – cancellation token.

**Return value:** the persisted `Scope` returned by `IScopeRepository.CreateAsync`.

**Exceptions:**
- `AuthServerException` with `invalid_request` and status `400` if `scopeId`, `displayName`, or `description` is `null`, empty, or whitespace.
- `AuthServerException` with `invalid_request` and status `400` if a scope with the same ID (case-insensitive) already exists.

## Claims

Claims listed on a scope are added to the ID token, the access token, or both, depending on the target collection. Scope claims are a set: adding a claim that is already present is a no-op. The check is case-sensitive, so `email` and `Email` are stored separately.

### `public async Task AddClaimToScopeAsync(string scopeId, string claim, bool isIdTokenClaim = true, CancellationToken cancellationToken = default)`

Adds a claim to a scope's `IdTokenClaims` (when `isIdTokenClaim` is `true`) or `AccessTokenClaims` (when `false`).

**Side effects:** Calls `IScopeRepository.UpdateAsync`, which sets the scope's `UpdatedAt` timestamp. No update is persisted when the claim already exists in the target collection, but the repository update is still called.

**Parameters:**
- `scopeId` – identifier of the existing scope.
- `claim` – claim name, such as `email`.
- `isIdTokenClaim` – target collection. `true` (default) adds to the ID token; `false` adds to the access token.
- `cancellationToken` – cancellation token.

**Exceptions:**
- `ArgumentException` if `scopeId` or `claim` is `null` or empty.
- `AuthServerException` with `invalid_request` and status `404` if the scope does not exist.

## Roles

### `public async Task AssignRoleAsync(string scopeId, string role, CancellationToken cancellationToken = default)`

Adds a role to the scope's `AllowedRoles`. Once any role is present, only users holding at least one of the listed roles can be granted the scope (see [Access filtering](#access-filtering)).

**Side effects:** Calls `IScopeRepository.UpdateAsync`. Role matching is case-insensitive, so a role already listed in a different case is not added again.

**Parameters:**
- `scopeId` – identifier of the existing scope.
- `role` – role name, such as `admin`.
- `cancellationToken` – cancellation token.

**Exceptions:**
- `ArgumentException` if `scopeId` or `role` is `null` or empty.
- `AuthServerException` with `invalid_request` and status `404` if the scope does not exist.

This method only adds roles. The service has no method to remove a role or claim.

## Listing scopes

### `public async Task<IEnumerable<ScopeSummary>> GetScopesWithClaimsAsync(CancellationToken cancellationToken = default)`

Returns a summary of every active scope, including its combined claims.

**Behavior:**
- Only scopes with `IsActive = true` are included. Inactive scopes are omitted.
- Each `ScopeSummary.Claims` is the union of `IdTokenClaims` and `AccessTokenClaims`, de-duplicated case-insensitively. The order is not guaranteed.
- Scopes are fetched from the repository before the method returns. The projection to `ScopeSummary` is lazy, so enumerating the result runs the mapping.

**Parameters:**
- `cancellationToken` – cancellation token.

**Return value:** one `ScopeSummary` per active scope. The list is empty if there are none.

## Access filtering

### `public async Task<IEnumerable<string>> ValidateAndFilterScopesAsync(IEnumerable<string> requestedScopes, IEnumerable<string> userRoles, CancellationToken cancellationToken = default)`

Filters the requested scope IDs down to those the user may receive. A requested scope is kept when it exists, is active, and `Scope.CanUserAccessScope(userRoles)` returns `true`. A scope with no allowed roles is open to every user.

**Behavior:**
- Unknown, inactive, and role-restricted scopes are dropped silently. No exception is thrown for them, so callers that need to report rejected scopes must compare the input with the result.
- Kept scopes are returned as they appear in `requestedScopes`, with the caller's casing, in the original order. Duplicates are not removed.
- If `requestedScopes` is empty, the result is empty.

**Parameters:**
- `requestedScopes` – scope IDs requested by the client.
- `userRoles` – roles of the authenticated user. Matching is case-insensitive.
- `cancellationToken` – cancellation token.

**Return value:** the accepted scope IDs as a `List<string>`.

**Exceptions:**
- `ArgumentNullException` if `requestedScopes` or `userRoles` is `null`.
- `ArgumentException` if an entry in `requestedScopes` is `null` or empty, because the lookup rejects it.

## Types returned

### `public sealed class ScopeSummary`

Read-only view of a scope for listing endpoints. It is a plain DTO with settable properties.

| Property | Type | Description |
|---|---|---|
| `ScopeId` | `string` | Scope identifier. |
| `DisplayName` | `string` | Human-readable name. |
| `Description` | `string` | Description of the scope. |
| `IsOpenIdScope` | `bool` | Whether the scope is an OpenID Connect scope. |
| `RequiresConsent` | `bool` | Whether the user must consent to the scope. |
| `Claims` | `IEnumerable<string>` | Combined ID-token and access-token claims. Defaults to an empty collection. |

## Example

```csharp
var scopes = new ScopeService(scopeRepository);

await scopes.CreateScopeAsync(
    "profile",
    "Profile",
    "Basic profile information",
    isOpenIdScope: true);

await scopes.AddClaimToScopeAsync("profile", "name");
await scopes.AddClaimToScopeAsync("profile", "email", isIdTokenClaim: false);
await scopes.AssignRoleAsync("profile", "member");

var granted = await scopes.ValidateAndFilterScopesAsync(
    ["openid", "profile", "admin:all"],
    userRoles: ["member"]);
// granted contains "profile" (and "openid" if it exists and is open to all users)
```
