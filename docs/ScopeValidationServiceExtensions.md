# ScopeValidationServiceExtensions

The `ScopeValidationServiceExtensions` class provides a set of static helper methods designed to streamline the validation, comparison, and analysis of OAuth 2.0 or OpenID Connect scope strings within the authentication flow. These extensions facilitate common operations such as verifying required scopes, filtering standard scopes, and determining scope deltas, ensuring consistent scope handling across the authentication service.

## API

### ContainsAnyRequiredScope
Checks if a provided collection of scopes contains at least one scope from a required set.

*   **Signature:** `public static bool ContainsAnyRequiredScope(IEnumerable<string> scopes, IEnumerable<string> requiredScopes)`
*   **Parameters:**
    *   `scopes` (`IEnumerable<string>`): The collection of scopes currently associated with the request or token.
    *   `requiredScopes` (`IEnumerable<string>`): The collection of scopes required for the requested operation.
*   **Returns:** `bool` - `true` if at least one scope from `requiredScopes` is present in `scopes`; otherwise, `false`.

### ValidateAsync
Asynchronously validates a collection of scopes against defined service constraints or policies.

*   **Signature:** `public static async Task<bool> ValidateAsync(IEnumerable<string> scopes)`
*   **Parameters:**
    *   `scopes` (`IEnumerable<string>`): The collection of scopes to validate.
*   **Returns:** `Task<bool>` - A task that completes with `true` if all scopes are valid; otherwise, `false`.

### IsStandardScopesOnly
Determines if a provided collection of scopes consists exclusively of standard, pre-defined scopes.

*   **Signature:** `public static bool IsStandardScopesOnly(IEnumerable<string> scopes)`
*   **Parameters:**
    *   `scopes` (`IEnumerable<string>`): The collection of scopes to examine.
*   **Returns:** `bool` - `true` if all scopes are identified as standard; otherwise, `false`.

### GetAddedScopes
Computes the set of new scopes added by comparing a modified scope collection against an original collection.

*   **Signature:** `public static IEnumerable<string> GetAddedScopes(IEnumerable<string> originalScopes, IEnumerable<string> newScopes)`
*   **Parameters:**
    *   `originalScopes` (`IEnumerable<string>`): The base collection of scopes.
    *   `newScopes` (`IEnumerable<string>`): The modified collection of scopes.
*   **Returns:** `IEnumerable<string>` - A collection containing only the scopes present in `newScopes` that were not in `originalScopes`.

## Usage

### Example 1: Verifying Required Scopes
```csharp
using var scopes = new List<string> { "openid", "profile", "email" };
var required = new List<string> { "admin", "write" };

if (!ScopeValidationServiceExtensions.ContainsAnyRequiredScope(scopes, required))
{
    throw new UnauthorizedAccessException("Missing required administrative scopes.");
}
```

### Example 2: Determining Scope Delta
```csharp
var originalScopes = new List<string> { "openid", "profile" };
var updatedScopes = new List<string> { "openid", "profile", "email", "offline_access" };

var added = ScopeValidationServiceExtensions.GetAddedScopes(originalScopes, updatedScopes);
// 'added' will contain { "email", "offline_access" }
```

## Notes

*   **Thread Safety:** The methods within `ScopeValidationServiceExtensions` are implemented as stateless static operations and are safe for concurrent use in multi-threaded environments, provided that the collections passed as arguments are not concurrently modified by other threads.
*   **Null Handling:** Consumers should ensure that the `IEnumerable<string>` arguments are not null before calling these methods to avoid `ArgumentNullException`.
*   **Case Sensitivity:** Scope string comparisons are case-sensitive. Ensure consistency in scope casing (typically lowercase) before passing collections to these extension methods.
