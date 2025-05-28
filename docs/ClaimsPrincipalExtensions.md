# ClaimsPrincipalExtensions

Provides a set of extension methods for `System.Security.Claims.ClaimsPrincipal` that simplify access to common identity and token claims such as subject, email, roles, scopes, and timestamps. The methods encapsulate claim lookup and type conversion, returning nullable values or empty collections when the requested claim is absent.

## API

### GetSubject
```csharp
public static string? GetSubject(this ClaimsPrincipal principal)
```
- **Purpose:** Retrieves the `sub` claim value.
- **Parameters:** `principal` – the `ClaimsPrincipal` to inspect.
- **Return value:** The claim value as a string, or `null` if the claim is missing.
- **Exceptions:** Throws `ArgumentNullException` if `principal` is `null`.

### GetEmail
```csharp
public static string? GetEmail(this ClaimsPrincipal principal)
```
- **Purpose:** Retrieves the `email` claim value.
- **Parameters:** `principal` – the `ClaimsPrincipal` to inspect.
- **Return value:** The claim value as a string, or `null` if the claim is missing.
- **Exceptions:** Throws `ArgumentNullException` if `principal` is `null`.

### IsEmailVerified
```csharp
public static bool IsEmailVerified(this ClaimsPrincipal principal)
```
- **Purpose:** Determines whether the `email_verified` claim is present and evaluates to `true`.
- **Parameters:** `principal` – the `ClaimsPrincipal` to inspect.
- **Return value:** `true` if the claim exists and its value is `true`; otherwise `false`.
- **Exceptions:** Throws `ArgumentNullException` if `principal` is `null`.

### GetRoles
```csharp
public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
```
- **Purpose:** Enumerates all `role` claim values.
- **Parameters:** `principal` – the `ClaimsPrincipal` to inspect.
- **Return value:** An `IEnumerable<string>` containing the role claim values; empty if no role claims exist.
- **Exceptions:** Throws `ArgumentNullException` if `principal` is `null`.

### HasRole
```csharp
public static bool HasRole(this ClaimsPrincipal principal, string role)
```
- **Purpose:** Checks whether the principal possesses a specific role claim.
- **Parameters:** 
  - `principal` – the `ClaimsPrincipal` to inspect.
  - `role` – the role value to match.
- **Return value:** `true` if any `role` claim equals `role`; otherwise `false`.
- **Exceptions:** 
  - Throws `ArgumentNullException` if `principal` is `null`.
  - Throws `ArgumentException` if `role` is `null` or empty.

### GetTokenSubject
```csharp
public static string? GetTokenSubject(this ClaimsPrincipal principal)
```
- **Purpose:** Retrieves the token subject claim (typically `sub` from a JWT).
- **Parameters:** `principal` – the `ClaimsPrincipal` to inspect.
- **Return value:** The claim value as a string, or `null` if the claim is missing.
- **Exceptions:** Throws `ArgumentNullException` if `principal` is `null`.

### GetAudience
```csharp
public static string? GetAudience(this ClaimsPrincipal principal)
```
- **Purpose:** Retrieves the first `aud` claim value.
- **Parameters:** `principal` – the `ClaimsPrincipal` to inspect.
- **Return value:** The claim value as a string, or `null` if the claim is missing.
- **Exceptions:** Throws `ArgumentNullException` if `principal` is `null`.

### GetScopes
```csharp
public static IEnumerable<string> GetScopes(this ClaimsPrincipal principal)
```
- **Purpose:** Enumerates all scope claim values (commonly `scp` or `scope`).
- **Parameters:** `principal` – the `ClaimsPrincipal` to inspect.
- **Return value:** An `IEnumerable<string>` containing the scope claim values; empty if no scope claims exist.
- **Exceptions:** Throws `ArgumentNullException` if `principal` is `null`.

### HasScope
```csharp
public static bool HasScope(this ClaimsPrincipal principal, string scope)
```
- **Purpose:** Checks whether the principal possesses a specific scope claim.
- **Parameters:** 
  - `principal` – the `ClaimsPrincipal` to inspect.
  - `scope` – the scope value to match.
- **Return value:** `true` if any scope claim equals `scope`; otherwise `false`.
- **Exceptions:** 
  - Throws `ArgumentNullException` if `principal` is `null`.
  - Throws `ArgumentException` if `scope` is `null` or empty.

### GetIssuedAt
```csharp
public static long? GetIssuedAt(this ClaimsPrincipal principal)
```
- **Purpose:** Retrieves the `iat` claim as a Unix timestamp (seconds since epoch).
- **Parameters:** `principal` – the `ClaimsPrincipal` to inspect.
- **Return value:** The claim value as a `long?`, or `null` if the claim is missing or not a valid integer.
- **Exceptions:** Throws `ArgumentNullException` if `principal` is `null`.

### GetExpiration
```csharp
public static long? GetExpiration(this ClaimsPrincipal principal)
```
- **Purpose:** Retrieves the `exp` claim as a Unix timestamp (seconds since epoch).
- **Parameters:** `principal` – the `ClaimsPrincipal` to inspect.
- **Return value:** The claim value as a `long?`, or `null` if the claim is missing or not a valid integer.
- **Exceptions:** Throws `ArgumentNullException` if `principal` is `null`.

## Usage

### Role-based authorization
```csharp
if (HttpContext.User.HasRole("admin"))
{
    // Allow access to administrative functionality
}
else
{
    // Challenge or forbid
}
```

### Token expiration check
```csharp
var principal = GetClaimsPrincipalFromToken(jwtToken);
var exp = principal.GetExpiration();
if (exp.HasValue && DateTimeOffset.FromUnixTimeSeconds(exp.Value) < DateTimeOffset.UtcNow)
{
    // Token has expired; request a new one or reject the request
}
```

## Notes

- All extension methods are stateless and thread‑safe; they only read from the supplied `ClaimsPrincipal` and do not modify it.
- Passing a `null` principal results in an `ArgumentNullException` for every method.
- When a claim is absent, methods that return a string or nullable long yield `null`, while collection‑returning methods (`GetRoles`, `GetScopes`) return an empty enumeration.
- String comparisons for `HasRole` and `HasScope` are case‑sensitive and require an exact match.
- The `IsEmailVerified` method treats any non‑boolean or missing claim as `false`; it does not throw on malformed values.
- Consumers should still validate the overall token signature and issuer separately; these helpers only simplify claim extraction.
